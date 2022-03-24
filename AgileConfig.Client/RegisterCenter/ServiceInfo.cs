using System.Collections.Generic;

namespace AgileConfig.Client.RegisterCenter
{
    public enum ServiceStatus
    {
        Offline = 0,
        Online = 1
    }

    public class ServiceInfo
    {
        public string ServiceId { get; set; } = "";

        public string ServiceName { get; set; } = "";

        public string Ip { get; set; } = "";

        public int? Port { get; set; }

        public List<string> MetaData { get; set; } = new List<string>();
        
        public ServiceStatus Status { get; set; }
    }

    public class ServiceRegisterInfo: ServiceInfo
    {
        public string CheckUrl { get; set; } = "";
        public string HeartBeatMode { get; set; } = "server";

        public int Interval { get; set; } = 30;
    }
}
