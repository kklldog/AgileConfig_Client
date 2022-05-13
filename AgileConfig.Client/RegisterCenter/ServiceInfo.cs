using System.Collections.Generic;

namespace AgileConfig.Client.RegisterCenter
{
    public enum ServiceStatus
    {
        Unhealthy = 0,
        Healthy = 1
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
        /// <summary>
        /// 健康检测地址
        /// </summary>
        public string CheckUrl { get; set; } = "";

        /// <summary>
        /// 服务不健康的时候通知地址
        /// </summary>
        public string AlarmUrl { get; set; } = "";

        public string HeartBeatMode { get; set; } = "client";

        public int Interval { get; set; } = 30;
    }

    public static class ServiceInfoExtension
    {
        public static string AsHttpHost(this ServiceInfo serviceInfo)
        {
            if (serviceInfo == null)
            {
                return "";
            }

            const string schema = "http";
            var host = $"{schema}://{serviceInfo.Ip}";
            if (serviceInfo.Port.HasValue)
            {
                host += $":{serviceInfo.Port}";
            }

            return host;
        }

        public static string AsHttpsHost(this ServiceInfo serviceInfo)
        {
            if (serviceInfo == null)
            {
                return "";
            }

            const string schema = "https";
            var host = $"{schema}://{serviceInfo.Ip}";
            if (serviceInfo.Port.HasValue)
            {
                host += $":{serviceInfo.Port}";
            }

            return host;
        }

        public static string AsWsHost(this ServiceInfo serviceInfo)
        {
            if (serviceInfo == null)
            {
                return "";
            }

            const string schema = "ws";
            var host = $"{schema}://{serviceInfo.Ip}";
            if (serviceInfo.Port.HasValue)
            {
                host += $":{serviceInfo.Port}";
            }

            return host;
        }

        public static string AsWssHost(this ServiceInfo serviceInfo)
        {
            if (serviceInfo == null)
            {
                return "";
            }

            const string schema = "wss";
            var host = $"{schema}://{serviceInfo.Ip}";
            if (serviceInfo.Port.HasValue)
            {
                host += $":{serviceInfo.Port}";
            }

            return host;
        }

        public static string AsTcpHost(this ServiceInfo serviceInfo)
        {
            if (serviceInfo == null)
            {
                return "";
            }

            const string schema = "tcp";
            var host = $"{schema}://{serviceInfo.Ip}";
            if (serviceInfo.Port.HasValue)
            {
                host += $":{serviceInfo.Port}";
            }

            return host;
        }
    }
}
