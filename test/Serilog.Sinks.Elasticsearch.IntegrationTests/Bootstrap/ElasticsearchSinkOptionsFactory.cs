using System;
using Elasticsearch.Net;

namespace Serilog.Sinks.Elasticsearch.IntegrationTests.Bootstrap
{
    public static class ElasticsearchSinkOptionsFactory
    {
        public static ElasticsearchSinkOptions Create(string indexPrefix, string templateName, Action<ElasticsearchSinkOptions> alterOptions = null)
        {
            // make sure we run through `ipv4.fiddler` if `fiddler` or `mitmproxy` is running
            // NOTE with the latter you need to add `ipv4.fiddler` as an alias to 127.0.0.1 in your HOSTS file manually
            var pool = new SingleNodeConnectionPool(new Uri($"http://{ProxyDetection.LocalOrProxyHost}:9200"));
            var options = new ElasticsearchSinkOptions(pool)
            {
                IndexFormat = indexPrefix + "{0:yyyy.MM.dd}",
                TemplateName = templateName,
            };
            
            alterOptions?.Invoke(options);
            // here we make sure we set a proxy on the elasticsearch connection 
            // when we detect `mitmproxy` running. This is a cli tool similar to fiddler
            // on *nix systems which aids debugging what goes over the wire
            var provided = options.ModifyConnectionSettings;
            options.ModifyConnectionSettings = configuration =>
            {
                if (ProxyDetection.RunningMitmProxy)
                    configuration.Proxy(ProxyDetection.MitmProxyAddress, null, (string) null);
                configuration = provided?.Invoke(configuration) ?? configuration;
                return configuration;
            };

            return options;
        }
        
    }
}