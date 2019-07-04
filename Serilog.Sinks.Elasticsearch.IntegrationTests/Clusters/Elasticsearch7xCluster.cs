using Serilog.Sinks.Elasticsearch.IntegrationTests.Bootstrap;

namespace Serilog.Sinks.Elasticsearch.IntegrationTests.Clusters
{
	/// <summary>
	/// Use this cluster for APIs that do writes. If they are however intrusive or long running consider IntrusiveOperationCluster
	/// instead.
	/// </summary>
	public class Elasticsearch7XCluster : ClientTestClusterBase
	{
		public Elasticsearch7XCluster() : base(CreateConfiguration()) { }

		private static ClientTestClusterConfiguration CreateConfiguration()
		{
			return new ClientTestClusterConfiguration("7.0.0")
			{
				MaxConcurrency = 1
			};
		}

		protected override void SeedCluster() { }
	}
}
