using Microsoft.VisualStudio.TestTools.UnitTesting;
using AgileConfig.Client.RegisterCenter;
using System;
using System.Collections.Generic;
using System.Text;

namespace AgileConfig.Client.RegisterCenter.Tests
{
    [TestClass()]
    public class ServiceInfoExtensionTests
    {
        [TestMethod()]
        public void AsHttpHostTest()
        {
            ServiceInfo service = null;
            var host = service.AsHttpHost();
            Assert.AreEqual("", host);

            service = new ServiceInfo()
            {
                Ip = "",
                Port = null,
            };
            host = service.AsHttpHost();
            Assert.AreEqual("http://", host);

            service = new ServiceInfo()
            {
                Ip = "192.168.0.1",
                Port = null,
            };
            host = service.AsHttpHost();
            Assert.AreEqual("http://192.168.0.1", host);

            service = new ServiceInfo()
            {
                Ip = "192.168.0.1",
                Port = 80,
            };
            host = service.AsHttpHost();
            Assert.AreEqual("http://192.168.0.1:80", host);
        }

        [TestMethod()]
        public void AsHttpsHostTest()
        {
            ServiceInfo service = null;
            var host = service.AsHttpsHost();
            Assert.AreEqual("", host);

            service = new ServiceInfo()
            {
                Ip = "",
                Port = null,
            };
            host = service.AsHttpsHost();
            Assert.AreEqual("https://", host);

            service = new ServiceInfo()
            {
                Ip = "192.168.0.1",
                Port = null,
            };
            host = service.AsHttpsHost();
            Assert.AreEqual("https://192.168.0.1", host);

            service = new ServiceInfo()
            {
                Ip = "192.168.0.1",
                Port = 80,
            };
            host = service.AsHttpsHost();
            Assert.AreEqual("https://192.168.0.1:80", host);
        }

        [TestMethod()]
        public void AsWsHostTest()
        {
            ServiceInfo service = null;
            var host = service.AsWsHost();
            Assert.AreEqual("", host);

            service = new ServiceInfo()
            {
                Ip = "",
                Port = null,
            };
            host = service.AsWsHost();
            Assert.AreEqual("ws://", host);

            service = new ServiceInfo()
            {
                Ip = "192.168.0.1",
                Port = null,
            };
            host = service.AsWsHost();
            Assert.AreEqual("ws://192.168.0.1", host);

            service = new ServiceInfo()
            {
                Ip = "192.168.0.1",
                Port = 80,
            };
            host = service.AsWsHost();
            Assert.AreEqual("ws://192.168.0.1:80", host);
        }

        [TestMethod()]
        public void AsWssHostTest()
        {
            ServiceInfo service = null;
            var host = service.AsWssHost();
            Assert.AreEqual("", host);

            service = new ServiceInfo()
            {
                Ip = "",
                Port = null,
            };
            host = service.AsWssHost();
            Assert.AreEqual("wss://", host);

            service = new ServiceInfo()
            {
                Ip = "192.168.0.1",
                Port = null,
            };
            host = service.AsWssHost();
            Assert.AreEqual("wss://192.168.0.1", host);

            service = new ServiceInfo()
            {
                Ip = "192.168.0.1",
                Port = 80,
            };
            host = service.AsWssHost();
            Assert.AreEqual("wss://192.168.0.1:80", host);
        }

        [TestMethod()]
        public void AsTcpHostTest()
        {
            ServiceInfo service = null;
            var host = service.AsTcpHost();
            Assert.AreEqual("", host);

            service = new ServiceInfo()
            {
                Ip = "",
                Port = null,
            };
            host = service.AsTcpHost();
            Assert.AreEqual("tcp://", host);

            service = new ServiceInfo()
            {
                Ip = "192.168.0.1",
                Port = null,
            };
            host = service.AsTcpHost();
            Assert.AreEqual("tcp://192.168.0.1", host);

            service = new ServiceInfo()
            {
                Ip = "192.168.0.1",
                Port = 80,
            };
            host = service.AsTcpHost();
            Assert.AreEqual("tcp://192.168.0.1:80", host);
        }
    }
}