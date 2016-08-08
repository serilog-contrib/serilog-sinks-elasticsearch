using Elasticsearch.Net;
using Xunit;

namespace Serilog.Sinks.Elasticsearch.Tests.Discrepancies
{
    public class ElasticsearchDefaultSerializerTests : ElasticsearchSinkUniformityTestsBase
    {
        public ElasticsearchDefaultSerializerTests() : base(new ElasticsearchDefaultSerializer()) { }

        [Fact]
        public void Should_SerializeToExpandedExceptionObjectWhenExceptionIsSet()
        {
            this.ThrowAndLogAndCatchBulkOutput("test_with_default_serializer");
        }
    }

}
