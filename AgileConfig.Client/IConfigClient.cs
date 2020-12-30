using Agile.Config.Protocol;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Agile.Config.Client
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

    public interface IConfigClient
    {
        string this[string key] { get; }

        string Get(string key);

        List<ConfigItem> GetGroup(string groupName);

        ConcurrentDictionary<string, string> Data { get; }

        Task<bool> ConnectAsync();

        bool Load();

        void LoadConfigs(List<ConfigItem> configs);

        event Action<ConfigChangedArg> ConfigChanged;
    }
}