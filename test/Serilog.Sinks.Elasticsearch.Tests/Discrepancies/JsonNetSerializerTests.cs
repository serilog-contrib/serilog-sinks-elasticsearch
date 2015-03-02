using System;
using System.Linq;
using Elasticsearch.Net.JsonNet;
using Elasticsearch.Net.Serialization;
using FluentAssertions;
using NUnit.Framework;

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
