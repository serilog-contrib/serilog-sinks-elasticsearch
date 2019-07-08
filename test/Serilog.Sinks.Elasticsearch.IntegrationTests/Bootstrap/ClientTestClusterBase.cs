using Elastic.Managed.Ephemeral;
using Elastic.Managed.Ephemeral.Plugins;
using Elastic.Xunit;
using Nest;

namespace Serilog.Sinks.Elasticsearch.IntegrationTests.Bootstrap
{
	public abstract class ClientTestClusterBase : XunitClusterBase<ClientTestClusterConfiguration> 
	{
		protected ClientTestClusterBase(ClientTestClusterConfiguration configuration) : base(configuration) { }
	}

	public class ClientTestClusterConfiguration : XunitClusterConfiguration
	{
		public ClientTestClusterConfiguration(
            string elasticsearchVersion,
            ClusterFeatures features = ClusterFeatures.None, 
            int numberOfNodes = 1, 
            params ElasticsearchPlugin[] plugins
		)
			: base(elasticsearchVersion, features, new ElasticsearchPlugins(plugins), numberOfNodes)
		{
			HttpFiddlerAware = true;
			CacheEsHomeInstallation = true;

			Add(AttributeKey("testingcluster"), "true");
            
			Add($"script.max_compilations_per_minute", "10000", "<6.0.0-rc1");
			Add($"script.max_compilations_rate", "10000/1m", ">=6.0.0-rc1");

			Add($"script.inline", "true", "<5.5.0");
			Add($"script.stored", "true", ">5.0.0-alpha1 <5.5.0");
			Add($"script.indexed", "true", "<5.0.0-alpha1");
			Add($"script.allowed_types", "inline,stored", ">=5.5.0");
		}
	}
}
