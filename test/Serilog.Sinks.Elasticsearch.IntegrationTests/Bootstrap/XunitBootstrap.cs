using Elastic.Xunit;
using Serilog.Sinks.Elasticsearch.IntegrationTests.Bootstrap;
using Xunit;

[assembly: TestFramework("Elastic.Xunit.Sdk.ElasticTestFramework", "Elastic.Xunit")]
[assembly: ElasticXunitConfiguration(typeof(SerilogSinkElasticsearchXunitRunOptions))]
