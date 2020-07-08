using Elastic.Elasticsearch.Xunit;
using Serilog.Sinks.Elasticsearch.IntegrationTests.Bootstrap;
using Xunit;

[assembly: TestFramework("Elastic.Elasticsearch.Xunit.Sdk.ElasticTestFramework", "Elastic.Elasticsearch.Xunit")]
[assembly: ElasticXunitConfiguration(typeof(SerilogSinkElasticsearchXunitRunOptions))]
