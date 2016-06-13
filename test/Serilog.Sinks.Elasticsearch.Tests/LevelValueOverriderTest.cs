using System;
using System.Collections.Generic;
using System.Linq;
using Elasticsearch.Net;
using FakeItEasy;
using FluentAssertions;
using NUnit.Framework;
using Serilog.Events;
using Serilog.Parsing;

namespace Serilog.Sinks.Elasticsearch.Tests
{
    [TestFixture]
    public class LevelValueOverriderTest : ElasticsearchSinkTestsBase
    {
        private static string _expectedInfomationValue = "InformationOverRidden";

        public LevelValueOverriderTest() : base(new ElasticsearchSinkOptions(new Uri("http://localhost:9200"))
        {
            BatchPostingLimit = 2,
            Period = TinyWait,
            Connection = A.Fake<IConnection>(),
            LogEventLevelNameOverrider = new Dictionary<LogEventLevel, string>()
            {
                {
                    LogEventLevel.Information, _expectedInfomationValue
                }
            }

        })
        {

        }

        [Test]
        public void UseDictionaryValuesIfPresent()
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
                bulkJsonPieces[1].Should().Contain($@"""level"":""{_expectedInfomationValue}");
                bulkJsonPieces[3].Should().Contain(@"""level"":""Error");
            }
        }
    }
}