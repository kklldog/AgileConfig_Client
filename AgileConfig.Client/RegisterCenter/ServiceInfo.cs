using System.Collections.Generic;

namespace AgileConfig.Client.RegisterCenter
{
    public class ServiceInfo
    {
        public string ServiceId { get; set; } = "";

        public string ServiceName { get; set; } = "";

        public string Ip { get; set; } = "";

        public int? Port { get; set; }

        public List<string> MetaData { get; set; } = new List<string>();

    }

    public class ServiceRegisterInfo: ServiceInfo
    {
        public string CheckUrl { get; set; } = "";

        public int CheckInterval { get; set; } = 30;
    }
}
