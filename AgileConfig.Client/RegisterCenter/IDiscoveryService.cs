using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AgileConfig.Client.RegisterCenter
{
    public interface IDiscoveryService
    {
        string DataVersion { get; }
        List<ServiceInfo> OfflineServices { get; }
        List<ServiceInfo> OnlineServices { get; }
        List<ServiceInfo> Services { get; }
        Task RefreshAsync();
    }
}
