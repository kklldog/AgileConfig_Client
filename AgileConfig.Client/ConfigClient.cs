﻿using AgileConfig.Client.MessageHandlers;
using AgileConfig.Client.Utils;
using AgileConfig.Protocol;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace AgileConfig.Client
{
    public enum ConnectStatus
    {
        Disconnected,
        Connecting,
        Connected,
    }

    public class ConfigClient : IConfigClient
    {
        private ConfigClientOptions _options;
        private int _websocketHeartbeatInterval = 30;
        private bool _isAutoReConnecting = false;
        private bool _isWsHeartbeating = false;
        private ClientWebSocket _websocketClient;
        private bool _isLoadFromLocal = false;
        private ConcurrentDictionary<string, string> _data = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private List<ConfigItem> _configs = new List<ConfigItem>();
        private string LocalCacheFileName => Path.Combine(_options.CacheDirectory, $"{_options.AppId}.agileconfig.client.configs.cache");

        /// <summary>
        /// client的实例对象，每次new的时候构造函数会吧this直接赋值给Instance，
        /// 一般来说你可以直接使用这个属性来拿到client对象，但是如果你手动new多个client的话，这个Instance代表最后new的那一个client，强烈不建议这么玩。
        /// </summary>
        public static IConfigClient Instance { get; private set; }

        public ClientWebSocket WebSocket => _websocketClient;

        public ConfigClientOptions Options => _options;

        public int ReconnectInterval => _options.ReconnectInterval;


        public ILogger Logger
        {
            get
            {
                if (_options.Logger == null)
                {
                    // 给一个默认的 console logger
                    return ConfigClientOptions.DefaultConsoleLogger;
                }

                return _options.Logger;
            }
            set => _options.Logger = value;
        }
        public ConnectStatus Status { get; private set; }

        public string ServerNodes => _options.Nodes;

        public string AppId => _options.AppId;

        public string Secret => _options.Secret;

        public string Env => _options.ENV;

        public string Name
        {
            get => _options.Name;
            set => _options.Name = value;
        }
        public string Tag
        {
            get => _options.Tag;
            set => _options.Tag = value;
        }

        /// <summary>
        /// http 超时时间 , 单位秒 , 默认100
        /// </summary>
        public int HttpTimeout
        {
            get => _options.HttpTimeout;
            set => _options.HttpTimeout = value;
        }

        /// <summary>
        /// 是否读取的事本地缓存的配置
        /// </summary>
        public bool IsLoadFromLocal => _isLoadFromLocal;

        /// <summary>
        /// 最新的配置(全量)被加载到本地后触发。
        /// </summary>
        public event Action<ConfigReloadedArgs> ReLoaded
        {
            add => _options.ReLoaded += value;
            remove => _options.ReLoaded -= value;
        }

        /// <summary>
        /// 配置项修改事件
        /// </summary>
        [Obsolete("ConfigChanged event will be obsolete, use ReLoaded event instead of.")]
        public event Action<ConfigChangedArg> ConfigChanged
        {
            add => _options.ConfigChanged += value;
            remove => _options.ConfigChanged -= value;
        }

        /// <summary>
        /// 所有的配置项最后都会转换为字典
        /// </summary>
        public ConcurrentDictionary<string, string> Data => _data;

        public DateTime? LastLoadedTimeFromServer
        {
            get; private set;
        }

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

        private void SetOptions(ConfigClientOptions options)
        {
            var appId = options.AppId;
            if (string.IsNullOrEmpty(appId))
            {
                throw new ArgumentNullException(nameof(appId));
            }
            if (string.IsNullOrEmpty(options.Nodes))
            {
                throw new ArgumentNullException(nameof(options.Nodes));
            }

            options.ENV = string.IsNullOrEmpty(options.ENV) ? "" : options.ENV.ToUpper();
            options.CacheDirectory = options.CacheDirectory ?? "";

            this._options = options;
        }

        /// <summary>
        /// 保证cache文件夹存在
        /// </summary>
        private void EnsureCacheDir()
        {
            if (!string.IsNullOrWhiteSpace(_options.CacheDirectory) && !Directory.Exists(_options.CacheDirectory))
            {
                Directory.CreateDirectory(_options.CacheDirectory);
            }
        }

        public ConfigClient(ConfigClientOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            this.SetOptions(options);
            //兼容老版本的 ConfigChanged 事件
            this._options.ReLoaded += (_) =>
            {
                this._options.ConfigChanged?.Invoke(new ConfigChangedArg(ActionConst.Reload, ""));
            };
            Instance = this;
        }

        public ConfigClient(string json = "appsettings.json")
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentNullException(nameof(json));

            var options = ConfigClientOptions.FromLocalAppsettings(json);
            this.SetOptions(options);
            //兼容老版本的 ConfigChanged 事件
            this._options.ReLoaded += (_) =>
            {
                this._options.ConfigChanged?.Invoke(new ConfigChangedArg(ActionConst.Reload, ""));
            };
            Instance = this;
        }

        public ConfigClient(IConfiguration configuration, ILogger logger = null)
        {
            var children = configuration.GetSection("AgileConfig").GetChildren();

            if (children == null || !children.Any())
            {
                children = configuration.GetChildren();
            }

            if (children == null || !children.Any())
            {
                throw new Exception($"Can not find section:AgileConfig from IConfiguration instance .");
            }

            var options = ConfigClientOptions.FromConfiguration(configuration);
            options.Logger = logger;
            this.SetOptions(options);
            //兼容老版本的 ConfigChanged 事件
            this._options.ReLoaded += (_) =>
            {
                this._options.ConfigChanged?.Invoke(new ConfigChangedArg(ActionConst.Reload, ""));
            };
            Instance = this;
        }

        public ConfigClient(string appId, string secret, string serverNodes, string env, ILogger logger = null)
        {
            if (string.IsNullOrEmpty(appId))
            {
                throw new ArgumentNullException(nameof(appId));
            }
            if (string.IsNullOrEmpty(serverNodes))
            {
                throw new ArgumentNullException(nameof(serverNodes));
            }

            var options = new ConfigClientOptions();
            options.AppId = appId;
            options.Secret = secret;
            options.Nodes = serverNodes;
            options.ENV = env;
            options.Logger = logger;
            this.SetOptions(options);
            //兼容老版本的 ConfigChanged 事件
            this._options.ReLoaded += (_) =>
            {
                this._options.ConfigChanged?.Invoke(new ConfigChangedArg(ActionConst.Reload, ""));
            };
            Instance = this;
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
                || _websocketClient?.State == WebSocketState.Open)
            {
                return true;
            }
            else
            {
                _websocketClient?.Abort();
                _websocketClient?.Dispose();
                _websocketClient = default;
                this.Status = ConnectStatus.Disconnected;
            }

            if (_websocketClient == null)
            {
                this.Status = ConnectStatus.Connecting;
                _websocketClient = new ClientWebSocket();
            }

            var connected = await TryConnectWebsocketAsync(_websocketClient).ConfigureAwait(false);
            await Load();//不管websocket是否成功，都去拉一次配置
            if (connected)
            {
                HandleWebsocketMessageAsync();
                WebsocketHeartbeatAsync();
            }
            //设置自动重连
            AutoReConnect();

            return connected;
        }

        private async Task<bool> TryConnectWebsocketAsync(ClientWebSocket client)
        {
            this.Status = ConnectStatus.Connecting;
            var clientName = string.IsNullOrEmpty(Name) ? "" : HttpUtility.UrlEncode(Name);
            var tag = string.IsNullOrEmpty(Tag) ? "" : HttpUtility.UrlEncode(Tag);

            client.Options.SetRequestHeader("appid", HttpUtility.UrlEncode(AppId));
            client.Options.SetRequestHeader("env", Env);
            client.Options.SetRequestHeader("Authorization", GenerateBasicAuthorization(AppId, Secret));
            client.Options.SetRequestHeader("client-v", AssemblyUtil.GetVer());

            var randomServer = new RandomServers(ServerNodes);
            int failCount = 0;
            while (!randomServer.IsComplete)
            {
                var server = randomServer.Next();
                try
                {
                    var wsUrl = GenerateWSUrl(server, clientName, tag);
                    Logger?.LogTrace("client try connect to server {0}", wsUrl);
                    await client.ConnectAsync(new Uri(wsUrl), CancellationToken.None).ConfigureAwait(false);
                    if (client.State == WebSocketState.Open)
                    {
                        Logger?.LogTrace("client connect server {0} successful .", wsUrl);
                    }
                    break;
                }
                catch (Exception e)
                {
                    failCount++;
                    Logger?.LogError(e, $"client try to connect server [{server}] occur error .");
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

        /// <summary>
        /// 构造websocket连接的url
        /// </summary>
        /// <param name="server"></param>
        /// <param name="clientName"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        private string GenerateWSUrl(string server, string clientName, string tag)
        {
            var url = "";
            if (server.StartsWith("https:", StringComparison.CurrentCultureIgnoreCase))
            {
                url = server.Replace("https:", "wss:").Replace("HTTPS:", "wss:");
            }
            else
            {
                url = server.Replace("http:", "ws:").Replace("HTTP:", "ws:");
            }
            url = url + (url.EndsWith("/") ? "ws" : "/ws");
            url += "?";
            url += "client_name=" + clientName;
            url += "&client_tag=" + tag;

            return url;
        }

        private void LoadConfigsFromLoacl()
        {
            var fileContent = ReadConfigsFromLocal();
            if (!string.IsNullOrEmpty(fileContent))
            {
                ReloadDataDictFromContent(fileContent);
                _isLoadFromLocal = true;
                Logger?.LogTrace("client load all configs from local file .");
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

            Task.Factory.StartNew(async () =>
            {
                while (_isAutoReConnecting)
                {
                    await Task.Delay(1000 * ReconnectInterval).ConfigureAwait(false);

                    if (_websocketClient?.State == WebSocketState.Open)
                    {
                        continue;
                    }
                    try
                    {
                        _websocketClient?.Abort();
                        _websocketClient?.Dispose();
                        this.Status = ConnectStatus.Disconnected;

                        if (!_isAutoReConnecting)
                        {
                            break;
                        }

                        _websocketClient = new ClientWebSocket();
                        var connected = await TryConnectWebsocketAsync(_websocketClient).ConfigureAwait(false);
                        if (connected)
                        {
                            await Load();
                            HandleWebsocketMessageAsync();
                            WebsocketHeartbeatAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger?.LogError(ex, "client try to connect to server but failed.");
                    }
                }
            }, TaskCreationOptions.LongRunning).ConfigureAwait(false);
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

            Task.Factory.StartNew(async () =>
            {
                var data = Encoding.UTF8.GetBytes("ping");
                while (_isWsHeartbeating)
                {
                    await Task.Delay(1000 * _websocketHeartbeatInterval).ConfigureAwait(false); ;
                    if (_websocketClient?.State == WebSocketState.Open)
                    {
                        try
                        {
                            //这里由于多线程的问题，WebsocketClient有可能在上一个if判断成功后被置空或者断开，所以需要try一下避免线程退出
                            await _websocketClient.SendAsync(new ArraySegment<byte>(data, 0, data.Length), WebSocketMessageType.Text, true,
                                    CancellationToken.None).ConfigureAwait(false);
                            Logger?.LogTrace("client send 'ping' to server by websocket .");
                        }
                        catch (Exception ex)
                        {
                            Logger?.LogError(ex, "client try to send Heartbeat to server but failed.");
                        }
                    }
                }
            }, TaskCreationOptions.LongRunning).ConfigureAwait(false);
        }
        /// <summary>
        /// 开启一个线程对服务端推送的websocket message进行处理
        /// </summary>
        /// <returns></returns>
        private void HandleWebsocketMessageAsync()
        {
            Task.Factory.StartNew(async () =>
            {
                while (_websocketClient?.State == WebSocketState.Open)
                {
                    ArraySegment<Byte> buffer = new ArraySegment<byte>(new Byte[1024 * 2]);
                    WebSocketReceiveResult result = null;
                    try
                    {
                        result = await _websocketClient.ReceiveAsync(buffer, CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Logger?.LogError(ex, "client try to receive message occur exception .");
                        throw;
                    }
                    if (result != null && result.CloseStatus.HasValue)
                    {
                        Logger?.LogTrace("client closed {0} .", result.CloseStatusDescription);
                        break;
                    }
                    ProcessMessage(result, buffer);
                }
            }, TaskCreationOptions.LongRunning).ConfigureAwait(false);
        }

        public async Task TryHandleAction(ActionMessage action)
        {
            try
            {
                if (action != null)
                {
                    switch (action.Action)
                    {
                        case ActionConst.Offline:
                            await this.DisconnectAsync();
                            Logger?.LogTrace("client offline because admin console send a command 'offline' .");
                            break;
                        case ActionConst.Reload:
                            await Load();
                            break;
                        case ActionConst.Ping:
                            var localVersion = this.DataMd5Version();
                            if (action.Data != localVersion)
                            {
                                await Load();
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "cannot handle websocket action , {0}", $"Module: {action.Module} Action: {action.Action} Data: {action.Data}");
            }
        }

        /// <summary>
        /// 分发到消息到处理类
        /// </summary>
        private async void ProcessMessage(WebSocketReceiveResult result, ArraySegment<Byte> buffer)
        {
            using (var ms = new MemoryStream())
            {
                ms.Write(buffer.Array, buffer.Offset, result.Count);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    using (var reader = new StreamReader(ms, Encoding.UTF8))
                    {
                        var msg = await reader.ReadToEndAsync().ConfigureAwait(false);
                        Logger?.LogTrace("client receive message ' {0} ' from server .", msg);
                        if (DropMessageHandler.Hit(msg))
                        {
                            return;
                        }
                        #region old message handlers
                        if (OldConfigPingRetrunMessageHandler.Hit(msg))
                        {
                            await OldConfigPingRetrunMessageHandler.Handle(msg, this);
                            return;
                        }
                        if (OldConfigActionMessageHandler.Hit(msg))
                        {
                            await OldConfigActionMessageHandler.Handle(msg, this);
                            return;
                        }
                        #endregion

                        if (ConfigCenterActionMessageHandler.Hit(msg))
                        {
                            await ConfigCenterActionMessageHandler.Handle(msg, this);
                            return;
                        }

                        MessageCenter.Receive(msg);
                        return;

                    }
                }
            }
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
        /// 复制一个当前的配置字典
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, string> CopyConfigDict()
        {
            var dict = new Dictionary<string, string>();
            if (this.Data != null && this.Data.Count > 0)
            {
                foreach (var config in this.Data)
                {
                    dict.Add(config.Key, config.Value);
                }
            }

            return dict;
        }

        /// <summary>
        /// 通过http从server拉取所有配置到本地
        /// </summary>
        public async Task<bool> Load()
        {
            int failCount = 0;
            var randomServer = new RandomServers(ServerNodes);
            while (!randomServer.IsComplete)
            {
                var server = randomServer.Next();
                try
                {
                    var appId = HttpUtility.UrlEncode(AppId);
                    var headers = new Dictionary<string, string>()
                    {
                       {"appid", appId },
                       {"Authorization", GenerateBasicAuthorization(AppId, Secret) }
                    };
                    var apiUrl = server + (server.EndsWith("/") ? "" : "/") + $"api/config/app/{appId}?env={Env}";
                    var timeout = (HttpTimeout <= 0 ? 30 : HttpTimeout) * 1000;
                    using (var result = HttpUtil.Get(apiUrl, headers, timeout))
                    {
                        if (result.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var respContent = await HttpUtil.GetResponseContentAsync(result);
                            LastLoadedTimeFromServer = DateTime.Now;
                            ReloadDataDictFromContent(respContent);
                            WriteConfigsToLocal(respContent);
                            _isLoadFromLocal = false;
                            await SendLoadedNoticeToServer();

                            Logger?.LogTrace("client load all the configs success by API: {0} , try count: {1}.", apiUrl, failCount);
                            return true;
                        }
                        else
                        {
                            //load remote configs err .
                            throw new Exception($"client try to load all the configs but failed , url {apiUrl}.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger?.LogError(ex, "client try to load all the configs but failed .");
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
            var oldData = CopyConfigDict();
            _configs = configs ?? new List<ConfigItem>(0);
            var tempData = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _configs.ForEach(c =>
            {
                var key = GenerateKey(c);
                string value = c.value;
                tempData.TryAdd(key.ToString(), value);
            });
            _data = tempData;
            var newData = CopyConfigDict();
            this.Options.ReLoaded?.Invoke(new ConfigReloadedArgs(oldData, newData));
        }

        private void ReloadDataDictFromContent(string content)
        {
            var configs = JsonSerializer.Deserialize<List<ConfigItem>>(content);
            LoadConfigs(configs);
        }

        /// <summary>
        /// send client loaded message to server
        /// </summary>
        /// <returns></returns>
        private async Task SendLoadedNoticeToServer()
        {
            var data = Encoding.UTF8.GetBytes("loaded");
            if (_websocketClient?.State == WebSocketState.Open)
            {
                try
                {
                    await _websocketClient.SendAsync(new ArraySegment<byte>(data, 0, data.Length), WebSocketMessageType.Text, true,
                            CancellationToken.None).ConfigureAwait(false);
                    Logger?.LogTrace("client send 'loaded' to server by websocket .");
                }
                catch (Exception ex)
                {
                    Logger?.LogError(ex, "client try to send loaded msg to server but failed.");
                }
            }
        }

        private void WriteConfigsToLocal(string configContent)
        {
            if (!_options.CacheEnabled)
            {
                return;
            }

            try
            {
                if (string.IsNullOrEmpty(configContent))
                {
                    return;
                }
                EnsureCacheDir();
                if (Options.ConfigCacheEncrypt)
                {
                    configContent = Encrypt.AesEncryptECB(Options.Secret, configContent);
                }
                File.WriteAllText(LocalCacheFileName, configContent);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "client try to cache all configs to local but failed .");
            }
        }

        private string ReadConfigsFromLocal()
        {
            EnsureCacheDir();
            if (!File.Exists(LocalCacheFileName))
            {
                return "";
            }

            var configContent = File.ReadAllText(LocalCacheFileName);
            if (Options.ConfigCacheEncrypt)
            {
                configContent = Encrypt.AesDecryptECB(Options.Secret, configContent);
            }
            return configContent;
        }

        public string DataMd5Version()
        {
            var keyStr = string.Join("&", Data.Keys.ToArray().OrderBy(k => k, StringComparer.Ordinal));
            var valueStr = string.Join("&", Data.Values.ToArray().OrderBy(v => v, StringComparer.Ordinal));
            var txt = $"{keyStr}&{valueStr}";

            var md5 = Encrypt.Md5(txt);

            return md5;
        }

        public async Task<bool> DisconnectAsync()
        {
            this._isAutoReConnecting = false;
            this._isWsHeartbeating = false;
            if (this._websocketClient?.State == WebSocketState.Open)
            {
                await this._websocketClient?.CloseAsync(WebSocketCloseStatus.Empty, null, CancellationToken.None);
            }
            this.Status = ConnectStatus.Disconnected;
            this._websocketClient?.Dispose();
            this._websocketClient = null;

            return true;
        }
    }
}
