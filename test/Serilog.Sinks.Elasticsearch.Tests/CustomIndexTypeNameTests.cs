using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Serilog.Events;
using Serilog.Parsing;
using Serilog.Sinks.Elasticsearch;
using Xunit;

namespace Serilog.Sinks.Elasticsearch.Tests
{
    public class CustomIndexTypeNameTests : ElasticsearchSinkTestsBase
    {
        [Fact]
        public void CustomIndex_And_TypeName_EndsUpInTheOutput()
        {
            //DO NOTE that you cant send objects as scalar values through Logger.*("{Scalar}", {});
            var timestamp = new DateTimeOffset(2013, 05, 28, 22, 10, 20, 666, TimeSpan.FromHours(10));
            const string messageTemplate = "{Song}++ @{Complex}";
            var template = new MessageTemplateParser().Parse(messageTemplate);
            _options.TypeName = "custom-event-type";
            _options.IndexFormat = "event-index-{0:yyyy.MM.dd}";
            using (var sink = new ElasticsearchSink(_options))
            {
                var properties = new List<LogEventProperty> { new LogEventProperty("Song", new ScalarValue("New Macabre")) };
                var e = new LogEvent(timestamp, LogEventLevel.Information, null, template, properties);
                //one off 
                sink.Emit(e);

                sink.Emit(e);
                var exception = new ArgumentException("parameter");
                properties = new List<LogEventProperty>
                {
                    new LogEventProperty("Song", new ScalarValue("Old Macabre")),
                    new LogEventProperty("Complex", new ScalarValue(new { A  = 1, B = 2}))
                };
                e = new LogEvent(timestamp.AddYears(-2), LogEventLevel.Fatal, exception, template, properties);
                sink.Emit(e);
            }

            var bulkJsonPieces = this.AssertSeenHttpPosts(_seenHttpPosts, 4);

            bulkJsonPieces.Should().HaveCount(4);
            bulkJsonPieces[0].Should().Contain(@"""_index"":""event-index-2013.05.28");
            bulkJsonPieces[0].Should().Contain(@"""_type"":""custom-event-type");
            bulkJsonPieces[1].Should().Contain("New Macabre");
            bulkJsonPieces[2].Should().Contain(@"""_index"":""event-index-2011.05.28");
            bulkJsonPieces[2].Should().Contain(@"""_type"":""custom-event-type");
            bulkJsonPieces[3].Should().Contain("Old Macabre");

            bulkJsonPieces[3].Should().Contain("Complex\":{");

        }

        [Fact]
        public void UpperCasedIndex_And_TypeName_EndsUpInTheOutput()
        {
            //DO NOTE that you cant send objects as scalar values through Logger.*("{Scalar}", {});
            var timestamp = new DateTimeOffset(2013, 05, 28, 22, 10, 20, 666, TimeSpan.FromHours(10));
            const string messageTemplate = "{Song}++ @{Complex}";
            var template = new MessageTemplateParser().Parse(messageTemplate);
            _options.TypeName = "custom-event-type";
            _options.IndexFormat = "Event-Index-{0:yyyy.MM.dd}";
            using (var sink = new ElasticsearchSink(_options))
            {
                var properties = new List<LogEventProperty> { new LogEventProperty("Song", new ScalarValue("New Macabre")) };
                var e = new LogEvent(timestamp, LogEventLevel.Information, null, template, properties);
                //one off 
                sink.Emit(e);

                sink.Emit(e);
                var exception = new ArgumentException("parameter");
                properties = new List<LogEventProperty>
                {
                    new LogEventProperty("Song", new ScalarValue("Old Macabre")),
                    new LogEventProperty("Complex", new ScalarValue(new { A  = 1, B = 2}))
                };
                e = new LogEvent(timestamp.AddYears(-2), LogEventLevel.Fatal, exception, template, properties);
                sink.Emit(e);
            }

            var bulkJsonPieces = this.AssertSeenHttpPosts(_seenHttpPosts, 4);

            bulkJsonPieces.Should().HaveCount(4);
            bulkJsonPieces[0].Should().Contain(@"""_index"":""event-index-2013.05.28");
            bulkJsonPieces[0].Should().Contain(@"""_type"":""custom-event-type");
            bulkJsonPieces[1].Should().Contain("New Macabre");
            bulkJsonPieces[2].Should().Contain(@"""_index"":""event-index-2011.05.28");
            bulkJsonPieces[2].Should().Contain(@"""_type"":""custom-event-type");
            bulkJsonPieces[3].Should().Contain("Old Macabre");

            bulkJsonPieces[3].Should().Contain("Complex\":{");

        }

    }

}
