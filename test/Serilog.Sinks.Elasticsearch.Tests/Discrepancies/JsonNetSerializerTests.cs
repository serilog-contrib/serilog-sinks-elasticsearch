using Elasticsearch.Net;
using Nest;
using NUnit.Framework;

namespace Serilog.Sinks.Elasticsearch.Tests.Discrepancies
{
    [TestFixture]
    public class JsonNetSerializerTests : ElasticsearchSinkUniformityTestsBase
    {
        public JsonNetSerializerTests() : base(new JsonNetSerializer(new ConnectionSettings())) { }

        [Test]
        public void Should_SerializeToExpandedExceptionObjectWhenExceptionIsSet()
        {
            this.ThrowAndLogAndCatchBulkOutput("test_with_jsonnet_serializer");
        }
    }

}
