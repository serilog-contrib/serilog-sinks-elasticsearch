using Elasticsearch.Net;
using System.Text;
using Xunit;

namespace Serilog.Sinks.Elasticsearch.Tests
{
    public class ElasticsearchSinkTests
    {
        [Theory]
        [InlineData("8.0.0", "my-logevent", null)]
        [InlineData("7.17.5", "my-logevent", null)]
        [InlineData("6.8.1", "my-logevent", "my-logevent")]
        [InlineData("8.0.0", null, null)]
        [InlineData("7.17.5", null, null)]
        [InlineData("6.8.1", null, "logevent")]
        public void DiscoverClusterVersion_DetectElasticsearchVersionSetToTrue_SetsTypeName(string elasticVersion, string configuredTypeName, string expectedTypeName)
        {
            /* ARRANGE */
            var options = new ElasticsearchSinkOptions
            {
                Connection = FakeResponse(elasticVersion),
                DetectElasticsearchVersion = true,
                TypeName = configuredTypeName
            };
            var sink = ElasticsearchSinkState.Create(options);

            /* ACT */
            sink.DiscoverClusterVersion();

            /* Assert */
            Assert.Equal(expectedTypeName, options.TypeName);
        }

        [Theory]
        [InlineData("8.0.0", "my-logevent", null)]
        [InlineData("7.17.5", "my-logevent", null)]
        [InlineData("6.8.1", "my-logevent", "my-logevent")]
        [InlineData("8.0.0", null, null)]
        [InlineData("7.17.5", null, null)]
        [InlineData("6.8.1", null, "logevent")]
        public void CreateLogger_DetectElasticsearchVersionSetToTrue_SetsTypeName(string elasticVersion, string configuredTypeName, string expectedTypeName)
        {
            /* ARRANGE */
            var options = new ElasticsearchSinkOptions
            {
                Connection = FakeResponse(elasticVersion),
                DetectElasticsearchVersion = true,
                TypeName = configuredTypeName
            };

            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.WithMachineName()
                .WriteTo.Console()
                .WriteTo.Elasticsearch(options);

            /* ACT */
            _ = loggerConfig.CreateLogger();

            /* Assert */
            Assert.Equal(expectedTypeName, options.TypeName);
        }

        private static IConnection FakeResponse(string responseText)
        {
            byte[] responseBody = Encoding.UTF8.GetBytes(responseText);
            return new InMemoryConnection(responseBody, contentType: "text/plain; charset=UTF-8");
        }
    }
}
