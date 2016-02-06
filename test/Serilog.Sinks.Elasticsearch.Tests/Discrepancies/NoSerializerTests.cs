using Xunit;

namespace Serilog.Sinks.Elasticsearch.Tests.Discrepancies
{
    public class NoSerializerTests : ElasticsearchSinkUniformityTestsBase
    {
        public NoSerializerTests() : base(null) {}

        [Fact]
        public void Should_SerializeToExpandedExceptionObjectWhenExceptionIsSet()
        {
            ThrowAndLogAndCatchBulkOutput("test_with_no_serializer");
        }
    }

}
