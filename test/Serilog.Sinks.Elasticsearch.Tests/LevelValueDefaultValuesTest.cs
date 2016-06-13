using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Serilog.Events;
using Serilog.Parsing;

namespace Serilog.Sinks.Elasticsearch.Tests
{
    [TestFixture]
    public class LevelValueDefaultValuesTest : ElasticsearchSinkTestsBase
    {
        [Test]
        public void UseEnumValuesIfNotSet()
        {
            try
            {
                throw new Exception("Test");
            }
            catch (Exception e)
            {
                var messageTemplate = "{Song}++";
                var template = new MessageTemplateParser().Parse(messageTemplate);
                using (var sink = new ElasticsearchSink(_options))
                {
                    var properties = new List<LogEventProperty>
                    {
                        new LogEventProperty("Prop", new ScalarValue("Value")),
                    };
                    var logEvent = new LogEvent(DateTime.UtcNow, LogEventLevel.Information, e, template, properties);
                    sink.Emit(logEvent);
                    var logEvent2 = new LogEvent(DateTime.UtcNow, LogEventLevel.Error, e, template, properties);
                    sink.Emit(logEvent2);
                }
                _seenHttpPosts.Should().NotBeEmpty().And.HaveCount(1);
                var json = _seenHttpPosts.First();
                var bulkJsonPieces = json.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                bulkJsonPieces.Should().HaveCount(4);
                bulkJsonPieces[1].Should().Contain(@"""level"":""Information");
                bulkJsonPieces[3].Should().Contain(@"""level"":""Error");
            }
        }
    }
}