using System;
using FluentAssertions;
using Xunit;

namespace Serilog.Sinks.Elasticsearch.Tests.Templating
{
    public class SetElasticsearchSinkOptions : ElasticsearchSinkTestsBase
    {

        [Fact]
        public void WhenCreatingOptions_NumberOfShardsInjected_NumberOfShardsAreSet()
        {
            var options = new ElasticsearchSinkOptions(new Uri("http://localhost:9100"))
            {
                AutoRegisterTemplate = true,

                NumberOfShards = 2,
                NumberOfReplicas = 0
            };
                       
            options.NumberOfReplicas.Should().Be(0);
            options.NumberOfShards.Should().Be(2);

        }
    }
}