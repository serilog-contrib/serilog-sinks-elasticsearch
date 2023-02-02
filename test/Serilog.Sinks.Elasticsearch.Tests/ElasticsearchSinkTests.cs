using Elasticsearch.Net;
using Serilog.Sinks.Elasticsearch.Tests.Stubs;
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
        public void Ctor_DetectElasticsearchVersionSetToTrue_SetsTypeName(string elasticVersion, string configuredTypeName, string expectedTypeName)
        {
            /* ARRANGE */
            var options = new ElasticsearchSinkOptions
            {
                Connection = FakeProductCheckResponse(elasticVersion),
                TypeName = configuredTypeName
            };

            /* ACT */
            _ = ElasticsearchSinkState.Create(options);

            /* Assert */
            Assert.Equal(expectedTypeName, options.TypeName);
        }

        [Theory]
        [InlineData("8.0.0", "my-logevent", null)]
        [InlineData("7.17.5", "my-logevent", null)]
        [InlineData("6.8.1", "my-logevent", null)]
        [InlineData("8.0.0", null, null)]
        [InlineData("7.17.5", null, null)]
        [InlineData("6.8.1", null, null)]
        public void Ctor_DetectElasticsearchVersionSetToFalseAssumesVersion7_SetsTypeNameToNull(string elasticVersion, string configuredTypeName, string expectedTypeName)
        {
            /* ARRANGE */
            var options = new ElasticsearchSinkOptions
            {
                Connection = FakeProductCheckResponse(elasticVersion),
                DetectElasticsearchVersion = false,
                TypeName = configuredTypeName
            };

            /* ACT */
            _ = ElasticsearchSinkState.Create(options);

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
                Connection = FakeProductCheckResponse(elasticVersion),
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

        private static IConnection FakeProductCheckResponse(string responseText)
        {
            var productCheckResponse = ConnectionStub.ModifiedProductCheckResponse(responseText);
            return new InMemoryConnection(productCheckResponse);
        }
    }
}
