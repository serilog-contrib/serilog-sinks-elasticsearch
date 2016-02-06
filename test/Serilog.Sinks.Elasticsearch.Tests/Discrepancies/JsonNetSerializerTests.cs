using Serilog.Sinks.Elasticsearch.Tests.Serializer;
using Xunit;

namespace Serilog.Sinks.Elasticsearch.Tests.Discrepancies
{
    public class JsonNetSerializerTests : ElasticsearchSinkUniformityTestsBase
    {
        public JsonNetSerializerTests() : base(new ElasticsearchJsonNetSerializer()) { }

        [Fact]
        public void Should_SerializeToExpandedExceptionObjectWhenExceptionIsSet()
        {
            ThrowAndLogAndCatchBulkOutput("test_with_jsonnet_serializer");
        }
    }

}
