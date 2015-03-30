using System.Linq;
using Elasticsearch.Net.Serialization;
using NUnit.Framework;

namespace Serilog.Sinks.Elasticsearch.Tests.Discrepancies
{
    [TestFixture]
    public class ElasticsearchDefaultSerializerTests : ElasticsearchSinkUniformityTestsBase
    {
        public ElasticsearchDefaultSerializerTests() : base(new ElasticsearchDefaultSerializer()) { }

        [Test]
        public void Should_SerializeToExpandedExceptionObjectWhenExceptionIsSet()
        {
            this.ThrowAndLogAndCatchBulkOutput("test_with_default_serializer");
        }
    }

}
