using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;
using AgileConfig.Protocol;
using Microsoft.Extensions.Logging;

namespace AgileConfig.Client
{
    public class ConfigChangedArg
    {
        public ConfigChangedArg(string action, string key)
        {
            Action = action;
            Key = key;
        }

        public string Key { get; }

        public string Action { get; }
    }

    public class ConfigReloadedArgs
    {
        public Dictionary<string, string> OldConfigs { get; }
        public Dictionary<string, string> NewConfigs { get; }
        public ConfigReloadedArgs(Dictionary<string,string> oldConfigs, Dictionary<string,string> newConfigs)
        {
            this.OldConfigs = oldConfigs;
            this.NewConfigs = newConfigs;
        }
    }

    public interface IConfigClient
    {
        ConnectStatus Status { get; }

        string this[string key] { get; }

        string Get(string key);

        List<ConfigItem> GetGroup(string groupName);

        ConcurrentDictionary<string, string> Data { get; }

        Task<bool> ConnectAsync();

        Task<bool> DisconnectAsync();

        Task<bool> Load();

        void LoadConfigs(List<ConfigItem> configs);

        [Obsolete("This event will be obsolete, please use ReLaoded event instead of.")]
        event Action<ConfigChangedArg> ConfigChanged;

        event Action<ConfigReloadedArgs> ReLoaded;

        ILogger Logger { get; set; }

        ConfigClientOptions Options { get; }

        ClientWebSocket WebSocket { get;  }

        DateTime? LastLoadedTimeFromServer { get; }
    }
}