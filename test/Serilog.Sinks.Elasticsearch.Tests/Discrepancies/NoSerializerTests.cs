using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace Serilog.Sinks.Elasticsearch.Tests.Discrepancies
{
    [TestFixture]
    public class NoSerializerTests : ElasticsearchSinkUniformityTestsBase
    {
        public NoSerializerTests() : base(null) {}

        [Test]
        public void Should_SerializeToExpandedExceptionObjectWhenExceptionIsSet()
        {
            this.ThrowAndLogAndCatchBulkOutput("test_with_no_serializer");
        }
    }

}
