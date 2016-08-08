using Elasticsearch.Net;
using Nest;
using Xunit;

namespace Serilog.Sinks.Elasticsearch.Tests.Discrepancies
{
    public class JsonNetSerializerTests : ElasticsearchSinkUniformityTestsBase
    {
        public JsonNetSerializerTests() : base(new JsonNetSerializer(new ConnectionSettings())) { }

        [Fact]
        public void Should_SerializeToExpandedExceptionObjectWhenExceptionIsSet()
        {
            this.ThrowAndLogAndCatchBulkOutput("test_with_jsonnet_serializer");
        }
    }

}
