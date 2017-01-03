using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elasticsearch.Net;
using Serilog.Events;
using Serilog.Parsing;
using Serilog.Sinks.Elasticsearch.Sinks.ElasticSearch;
using Xunit;
using FluentAssertions;

namespace Serilog.Sinks.Elasticsearch.Tests
{
    public class TreatPropertyAsSourceElasticSearchJsonFormatterTests : ElasticsearchSinkTestsBase
    {
        [Fact]
        public async Task TreatPropertyAsSource()
        {
            using (var sink = new ElasticsearchSink(new ElasticsearchSinkOptions(
                new SingleNodeConnectionPool(new Uri("http://localhost:9200")))
            {
                BatchPostingLimit = 2,
                Connection = _connection,
                InlineFields = true,
                CustomFormatter = new TreatPropertyAsSourceElasticsearchJsonFormatter("MyProperty")
            }))
            {
                var properties = new List<LogEventProperty>
                    {
                        new LogEventProperty("MyProperty", new StructureValue(new List<LogEventProperty>()
                        {
                            new LogEventProperty("Timestamp", new ScalarValue("2017-01-03T03:28:54.5776763Z")),
                            new LogEventProperty("Level", new ScalarValue("Error")),
                            new LogEventProperty("Prop1", new ScalarValue("Prop1Value")),
                            new LogEventProperty("Prop2", new ScalarValue("Prop2Value")),
                        } )),
                    };

                sink.Emit(new LogEvent(DateTime.Now, LogEventLevel.Information, null, new MessageTemplateParser().Parse("@MyProperty"), properties));
                sink.Emit(new LogEvent(DateTime.Now, LogEventLevel.Information, null, new MessageTemplateParser().Parse("@MyProperty"), properties));
                
            }
            var bulkJsonPieces = this.AssertSeenHttpPosts(_seenHttpPosts, 4);
            bulkJsonPieces[1].ShouldBeEquivalentTo(
                "{\"Timestamp\":\"2017-01-03T03:28:54.5776763Z\",\"Level\":\"Error\",\"Prop1\":\"Prop1Value\",\"Prop2\":\"Prop2Value\"}\r");
            
        }
    }
}
