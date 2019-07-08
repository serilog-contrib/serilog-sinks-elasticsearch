using System;
using System.Diagnostics;
using System.Linq;

namespace Serilog.Sinks.Elasticsearch.IntegrationTests.Bootstrap
{
    public static class ProxyDetection
    {
        public static readonly bool RunningMitmProxy = Process.GetProcessesByName("mitmproxy").Any();
        private static readonly bool RunningFiddler = Process.GetProcessesByName("fiddler").Any();
        public static string LocalOrProxyHost => RunningFiddler || RunningMitmProxy ? "ipv4.fiddler" : "localhost";
        
        public static readonly Uri MitmProxyAddress = new Uri("http://localhost:8080");
    }
}