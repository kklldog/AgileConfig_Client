using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace AgileConfig.Client.RegisterCenter
{
    public static class ServiceListExtension
    {
        public static IEnumerable<ServiceInfo> GetByServiceName(this IEnumerable<ServiceInfo> services, string serviceName)
        {
            return services.Where(x => x.ServiceName == serviceName);
        }

        public static ServiceInfo GetByServiceId(this IEnumerable<ServiceInfo> services, string serviceId)
        {
            return services.FirstOrDefault(x => x.ServiceId == serviceId);
        }

        public static ServiceInfo RandomOne(this IEnumerable<ServiceInfo> services, string serviceName)
        {
            var list = GetByServiceName(services, serviceName);
            var serviceCount = list?.Count();
            if (!serviceCount.HasValue || serviceCount == 0)
            {
                return null;
            }

            var index = new Random().Next(serviceCount.Value);

            return list.Skip(index).Take(1).FirstOrDefault();
        }

        public static ServiceInfo RandomOne(this IEnumerable<ServiceInfo> services)
        {
            var serviceCount = services?.Count();
            if (!serviceCount.HasValue || serviceCount == 0)
            {
                return null;
            }

            var index = new Random().Next(serviceCount.Value);

            return services.Skip(index).Take(1).FirstOrDefault();
        }
    }

    public static class DiscoveryServiceExtension
    {
        public static IEnumerable<ServiceInfo> GetByServiceName(this IDiscoveryService ds, string serviceName)
        {
            return ds.Services.GetByServiceName(serviceName);
        }

        public static ServiceInfo GetByServiceId(this IDiscoveryService ds, string serviceId)
        {
            return ds.Services.GetByServiceId(serviceId);
        }

        public static ServiceInfo RandomOne(this IDiscoveryService ds, string serviceName)
        {
            return ds.Services.RandomOne(serviceName);
        }

        public static ServiceInfo RandomOne(this IDiscoveryService ds)
        {
            return ds.Services.RandomOne();
        }
    }
}
