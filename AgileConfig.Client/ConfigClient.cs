using Agile.Config.Protocol;
using AgileConfig.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
namespace Agile.Config.Client
{
    public enum ConnectStatus
    {
        Disconnected,
        Connecting,
        Connected,
    }

    public class ConfigClient : IConfigClient
    {
        public static IConfigClient Instance = null;
        public ConfigClient(string json = "appsettings.json")
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentNullException(nameof(json));

            //读取本地配置
            var localconfig = new ConfigurationBuilder()
                             .SetBasePath(Directory.GetCurrentDirectory())
                             .AddJsonFile(json).AddEnvironmentVariables().Build();
            //从本地配置里读取AgileConfig的相关信息
            var configSection = localconfig.GetSection("AgileConfig");
            if (!configSection.Exists())
            {
                throw new Exception($"Can not find section:AgileConfig from {json}");
            }
            var appId = localconfig["AgileConfig:appId"];
            var secret = localconfig["AgileConfig:secret"];
            var serverNodes = localconfig["AgileConfig:nodes"];
            var name = localconfig["AgileConfig:name"];
            var tag = localconfig["AgileConfig:tag"];

            if (string.IsNullOrEmpty(appId))
            {
                throw new ArgumentNullException(nameof(appId));
            }
            if (string.IsNullOrEmpty(serverNodes))
            {
                throw new ArgumentNullException(nameof(serverNodes));
            }
            this.Name = name;
            this.Tag = tag;
            this._AppId = appId;
            this._Secret = secret;
            this._ServerNodes = serverNodes;
        }

        public ConfigClient(IConfiguration configuration, ILogger logger = null)
        {
            this.Logger = logger;

            var children = configuration.GetSection("AgileConfig").GetChildren();

            if (children == null || !children.Any())
            {
                children = configuration.GetChildren();
            }

            if (children == null || !children.Any())
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var appId = children.FirstOrDefault(x => string.Equals(x.Key, "appid", StringComparison.OrdinalIgnoreCase))?.Value;
            var secret = children.FirstOrDefault(x => string.Equals(x.Key, "secret", StringComparison.OrdinalIgnoreCase))?.Value;
            var serverNodes = children.FirstOrDefault(x => string.Equals(x.Key, "nodes", StringComparison.OrdinalIgnoreCase))?.Value;
            var name = children.FirstOrDefault(x => string.Equals(x.Key, "name", StringComparison.OrdinalIgnoreCase))?.Value;
            var tag = children.FirstOrDefault(x => string.Equals(x.Key, "tag", StringComparison.OrdinalIgnoreCase))?.Value;

            if (string.IsNullOrEmpty(appId))
            {
                throw new ArgumentNullException(nameof(appId));
            }

            if (string.IsNullOrEmpty(serverNodes))
            {
                throw new ArgumentNullException(nameof(serverNodes));
            }

            this.Name = name;
            this.Tag = tag;
            this._AppId = appId;
            this._Secret = secret;
            this._ServerNodes = serverNodes;
        }

        public ConfigClient(string appId, string secret, string serverNodes, ILogger logger = null)
        {
            this.Logger = logger;
            if (string.IsNullOrEmpty(appId))
            {
                throw new ArgumentNullException(nameof(appId));
            }
            if (string.IsNullOrEmpty(serverNodes))
            {
                throw new ArgumentNullException(nameof(serverNodes));
            }
            this._AppId = appId;
            this._Secret = secret;
            this._ServerNodes = serverNodes;
        }

        private int _WebsocketReconnectInterval = 5;
        private int _WebsocketHeartbeatInterval = 30;

        public ILogger Logger { get; set; }
        private string _ServerNodes;
        private string _AppId;
        private string _Secret;
        private bool _isAutoReConnecting = false;
        private bool _isWsHeartbeating = false;

        private WebSocket4Net.WebSocket _WebsocketClient;
        private bool _adminSayOffline = false;
        private bool _isLoadFromLocal = false;
        private ConcurrentDictionary<string, string> _data = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private List<ConfigItem> _configs = new List<ConfigItem>();

        public ConnectStatus Status { get; private set; }

        public string Name
        {
            get;
            set;
        }
        public string Tag
        {
            get;
            set;
        }

        /// <summary>
        /// 是否读取的事本地缓存的配置
        /// </summary>
        public bool IsLoadFromLocal
        {
            get
            {
                return _isLoadFromLocal;
            }
        }
        /// <summary>
        /// 配置项修改事件
        /// </summary>
        public event Action<ConfigChangedArg> ConfigChanged;
        /// <summary>
        /// 所有的配置项最后都会转换为字典
        /// </summary>
        public ConcurrentDictionary<string, string> Data => _data;

        public string this[string key]
        {
            get
            {
                Data.TryGetValue(key, out string val);
                return val;
            }
        }

        /// <summary>
        /// 根据键值获取配置值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string Get(string key)
        {
            Data.TryGetValue(key, out string val);
            return val;
        }

        /// <summary>
        /// 获取分组配置信息
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        public List<ConfigItem> GetGroup(string groupName)
        {
            if (_configs == null)
            {
                return null;
            }

            return _configs.Where(x => x.group == groupName).ToList();
        }

        /// <summary>
        /// 连接服务端
        /// </summary>
        /// <returns></returns>
        public async Task<bool> ConnectAsync()
        {
            if (this.Status == ConnectStatus.Connected 
                || this.Status == ConnectStatus.Connecting 
                || _WebsocketClient?.State == WebSocket4Net.WebSocketState.Open)
            {
                return true;
            }
            else
            {
                _WebsocketClient?.Dispose();
                _WebsocketClient = default;
                this.Status = ConnectStatus.Disconnected;
            }

            var connected = await TryConnectWebsocketAsync().ConfigureAwait(false);
            Load();//不管websocket是否成功，都去拉一次配置
            if (connected)
            {
                WebsocketHeartbeatAsync();
            }
            //设置自动重连
            AutoReConnect();

            return connected;
        }

        private async Task<bool> TryConnectWebsocketAsync()
        {
            this.Status = ConnectStatus.Connecting;
            var clientName = string.IsNullOrEmpty(Name) ? "" : System.Web.HttpUtility.UrlEncode(Name);
            var tag = string.IsNullOrEmpty(Tag) ? "" : System.Web.HttpUtility.UrlEncode(Tag);


            //client.Options.SetRequestHeader("appid", _AppId);
            //client.Options.SetRequestHeader("Authorization", GenerateBasicAuthorization(_AppId, _Secret));

            var headers = new List<KeyValuePair<string, string>>();
            headers.Add(new KeyValuePair<string, string>("appid", _AppId));
            headers.Add(new KeyValuePair<string, string>("Authorization", GenerateBasicAuthorization(_AppId, _Secret)));

            var randomServer = new RandomServers(_ServerNodes);
            int failCount = 0;
            while (!randomServer.IsComplete)
            {
                var server = randomServer.Next();
                try
                {
                    var websocketServerUrl = "";
                    if (server.StartsWith("https:", StringComparison.CurrentCultureIgnoreCase))
                    {
                        websocketServerUrl = server.Replace("https:", "wss:").Replace("HTTPS:", "wss:");
                    }
                    else
                    {
                        websocketServerUrl = server.Replace("http:", "ws:").Replace("HTTP:", "ws:");
                    }
                    websocketServerUrl = websocketServerUrl + (websocketServerUrl.EndsWith("/") ? "ws" : "/ws");
                    websocketServerUrl += "?";
                    websocketServerUrl += "client_name=" + clientName;
                    websocketServerUrl += "&client_tag=" + tag;
                    Logger?.LogTrace("AgileConfig Client Websocket try connect to server {0}", websocketServerUrl);

                    if (_WebsocketClient == null)
                    {
                        _WebsocketClient = new WebSocket4Net.WebSocket(websocketServerUrl, "", null, headers);
                    }
                    var conn_result = await _WebsocketClient.OpenAsync().ConfigureAwait(false);
                    Logger?.LogTrace("AgileConfig Client Websocket server {0} {1}", websocketServerUrl, conn_result? "success":"failed");
                    if (!conn_result)
                    {
                        _WebsocketClient?.Dispose();
                        _WebsocketClient = null;
                        throw new Exception($"websocket client try to connect to {websocketServerUrl} failed .");
                    }
                    else
                    {
                        _WebsocketClient.MessageReceived += _WebsocketClient_MessageReceived;
                        _WebsocketClient.Closed += (s, e) =>
                        {
                            Logger?.LogTrace("websocket client closed .");
                        };
                        _WebsocketClient.Error += (s, e) =>
                        {
                            Logger?.LogError(e.Exception, "websocket client occur error ." );
                        };
                    }
                    break;
                }
                catch (Exception e)
                {
                    failCount++;
                    Logger?.LogError(e, "AgileConfig Client Websocket try connect to server occur error .");
                }
            }

            if (failCount == randomServer.ServerCount)
            {
                //连接所有的服务器都失败了。
                this.Status = ConnectStatus.Disconnected;
                return false;
            }

            this.Status = ConnectStatus.Connected;
            return true;
        }

        private void _WebsocketClient_MessageReceived(object sender, WebSocket4Net.MessageReceivedEventArgs e)
        {
            Task.Run(async () =>
            {
                await ProcessMessage(e?.Message);
            });
        }

        private void LoadConfigsFromLoacl()
        {
            var fileContent = ReadConfigsFromLocal();
            if (!string.IsNullOrEmpty(fileContent))
            {
                ReloadDataDictFromContent(fileContent);
                _isLoadFromLocal = true;
                Logger?.LogTrace("AgileConfig Client load all configs from local file .");
            }
        }

        /// <summary>
        /// 开启一个线程来初始化Websocket Client，并且5s一次进行检查是否连接打开状态，如果不是则尝试重连。
        /// </summary>
        /// <returns></returns>
        private void AutoReConnect()
        {
            if (_isAutoReConnecting)
            {
                return;
            }
            _isAutoReConnecting = true;

            Thread th = new Thread(async () =>
            {
                while (true)
                {
                    Thread.Sleep(1000 * _WebsocketReconnectInterval);

                    if (_WebsocketClient?.State == WebSocket4Net.WebSocketState.Open || _WebsocketClient?.State == WebSocket4Net.WebSocketState.Connecting)
                    {
                        continue;
                    }

                    try
                    {
                        _WebsocketClient?.Dispose();
                        _WebsocketClient = null;
                        this.Status = ConnectStatus.Disconnected;

                        if (_adminSayOffline)
                        {
                            break;
                        }

                        var connected = await TryConnectWebsocketAsync().ConfigureAwait(false);
                        if (connected)
                        {
                            Load();
                            WebsocketHeartbeatAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger?.LogError(ex, "AgileConfig Client Websocket try to connected to server failed.");
                    }
                }
            });
            th.Start();
        }

        private string GenerateBasicAuthorization(string appId, string secret)
        {
            var txt = $"{appId}:{secret}";
            var data = Encoding.UTF8.GetBytes(txt);
            return "Basic " + Convert.ToBase64String(data);
        }
        /// <summary>
        /// 开启一个线程30s进行一次心跳
        /// </summary>
        /// <returns></returns>
        public void WebsocketHeartbeatAsync()
        {
            if (_isWsHeartbeating)
            {
                return;
            }
            _isWsHeartbeating = true;

            new Thread( () =>
            {
                while (true)
                {
                    Thread.Sleep(1000 * _WebsocketHeartbeatInterval);
                    if (_adminSayOffline)
                    {
                        break;
                    }
                    if (_WebsocketClient?.State == WebSocket4Net.WebSocketState.Open)
                    {
                        try
                        {
                            //这里由于多线程的问题，WebsocketClient有可能在上一个if判断成功后被置空或者断开，所以需要try一下避免线程退出
                            _WebsocketClient.Send("ping");
                            Logger?.LogTrace("AgileConfig Client Say 'ping' by Websocket .");
                        }
                        catch (Exception ex)
                        {
                            Logger?.LogError(ex, "AgileConfig Client Websocket try to send Heartbeat to server failed.");
                        }
                    }
                }
            }).Start();
        }


        /// <summary>
        /// 最终处理服务端推送的消息
        /// </summary>
        private async Task ProcessMessage(string msg)
        {
            Logger?.LogTrace("AgileConfig Client Receive message ' {0} ' by Websocket .", msg);
            if (string.IsNullOrEmpty(msg) || msg == "0")
            {
                return;
            }
            if (msg.StartsWith("V:"))
            {
                var version = msg.Substring(2, msg.Length - 2);
                var localVersion = this.DataMd5Version();
                if (version != localVersion)
                {
                    //如果数据库版本跟本地版本不一致则直接全部更新
                    Load();
                }
                return;
            }
            try
            {
                var action = JsonConvert.DeserializeObject<WebsocketAction>(msg);
                if (action != null)
                {
                    var dict = Data;
                    var itemKey = "";
                    if (action.Item != null)
                    {
                        itemKey = GenerateKey(action.Item);
                    }
                    switch (action.Action)
                    {
                        case ActionConst.Add:
                            dict.AddOrUpdate(itemKey, action.Item.value, (k, v) => { return action.Item.value; });
                            NoticeChangedAsync(ActionConst.Add, itemKey);
                            break;
                        case ActionConst.Update:
                            if (action.OldItem != null)
                            {
                                dict.TryRemove(GenerateKey(action.OldItem), out string oldV);
                            }
                            dict.AddOrUpdate(itemKey, action.Item.value, (k, v) => { return action.Item.value; });
                            NoticeChangedAsync(ActionConst.Update, itemKey);
                            break;
                        case ActionConst.Remove:
                            dict.TryRemove(itemKey, out string oldV1);
                            NoticeChangedAsync(ActionConst.Remove, itemKey);
                            break;
                        case ActionConst.Offline:
                            _adminSayOffline = true;
                            await _WebsocketClient.CloseAsync().ConfigureAwait(false);
                            this.Status = ConnectStatus.Disconnected;
                            Logger?.LogTrace("Websocket client offline because admin console send a command 'offline' ,");
                            NoticeChangedAsync(ActionConst.Offline);
                            break;
                        case ActionConst.Reload:
                            if (Load())
                            {
                                NoticeChangedAsync(ActionConst.Reload);
                            };
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Cannot handle websocket message {0}", msg);
            }
        }

        private void NoticeChangedAsync(string action, string key = "")
        {
            if (ConfigChanged == null)
            {
                return;
            }
            Task.Run(() =>
            {
                ConfigChanged(new ConfigChangedArg(action, key));
            });
        }

        private string GenerateKey(ConfigItem item)
        {
            var key = new StringBuilder();
            if (!string.IsNullOrEmpty(item.group))
            {
                key.Append(item.group + ":");
            }
            key.Append(item.key);

            return key.ToString();
        }

        /// <summary>
        /// 通过http从server拉取所有配置到本地
        /// </summary>
        public bool Load()
        {
            int failCount = 0;
            var randomServer = new RandomServers(_ServerNodes);
            while (!randomServer.IsComplete)
            {
                var url = randomServer.Next();
                try
                {
                    var op = new AgileHttp.RequestOptions()
                    {
                        Headers = new Dictionary<string, string>()
                        {
                            {"appid", _AppId },
                            {"Authorization", GenerateBasicAuthorization(_AppId, _Secret) }
                        }
                    };
                    var apiUrl = url + (url.EndsWith("/") ? "" : "/") + $"api/config/app/{_AppId}";
                    using (var result = AgileHttp.HTTP.Send(apiUrl, "GET", null, op))
                    {
                        if (result.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var respContent = result.GetResponseContent();
                            ReloadDataDictFromContent(respContent);
                            WriteConfigsToLocal(respContent);
                            _isLoadFromLocal = false;

                            Logger?.LogTrace("AgileConfig Client Loaded all the configs success from {0} , Try count: {1}.", apiUrl, failCount);
                            return true;
                        }
                        else
                        {
                            //load remote configs err .
                            var ex = result.Exception ?? new Exception("AgileConfig Client Load all the configs failed .");
                            throw ex;
                        }
                    }
                }
                catch
                {
                    failCount++;
                }
            }
            if (failCount == randomServer.ServerCount)
            {
                LoadConfigsFromLoacl();
            }
            return false;
        }

        public void LoadConfigs(List<ConfigItem> configs)
        {
            Data.Clear();
            _configs.Clear();
            if (configs != null)
            {
                _configs = configs;
                _configs.ForEach(c =>
                {
                    var key = GenerateKey(c);
                    string value = c.value;
                    Data.TryAdd(key.ToString(), value);
                });
            }
        }

        private void ReloadDataDictFromContent(string content)
        {
            var configs = JsonConvert.DeserializeObject<List<ConfigItem>>(content);
            LoadConfigs(configs);
        }

        private string LocalCacheFileName => $"{_AppId}.agileconfig.client.configs.cache";
        private void WriteConfigsToLocal(string configContent)
        {
            try
            {
                if (string.IsNullOrEmpty(configContent))
                {
                    return;
                }

                File.WriteAllText(LocalCacheFileName, configContent);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "AgileConfig Client try to cache all configs to local but fail .");
            }
        }

        private string ReadConfigsFromLocal()
        {
            if (!File.Exists(LocalCacheFileName))
            {
                return "";
            }

            return File.ReadAllText(LocalCacheFileName);
        }

        private string DataMd5Version()
        {
            var keyStr = string.Join("&", Data.Keys.ToArray().OrderBy(k => k));
            var valueStr = string.Join("&", Data.Values.ToArray().OrderBy(v => v));
            var txt = $"{keyStr}&{valueStr}";

            using (var md5 = MD5.Create())
            {
                var inputBytes = Encoding.ASCII.GetBytes(txt);
                var hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                var sb = new StringBuilder();
                for (var i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }

                return sb.ToString();
            }
        }


    }
}
