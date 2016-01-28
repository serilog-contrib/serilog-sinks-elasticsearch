using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Serilog.Sinks.Elasticsearch.Tests.Serializer;

namespace Serilog.Sinks.Elasticsearch.Tests.Discrepancies
{
    [TestFixture]
    public class JsonNetSerializerTests : ElasticsearchSinkUniformityTestsBase
    {
        public JsonNetSerializerTests() : base(new ElasticsearchJsonNetSerializer()) { }

        [Test]
        public void Should_SerializeToExpandedExceptionObjectWhenExceptionIsSet()
        {
            this.ThrowAndLogAndCatchBulkOutput("test_with_jsonnet_serializer");
        }
    }

}
