using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Xunit;
using Serilog.Events;
using Serilog.Parsing;
using Serilog.Sinks.Elasticsearch.Tests.Domain;

namespace Serilog.Sinks.Elasticsearch.Tests
{
    public class ExceptionAsJsonObjectFormatterTests : ElasticsearchSinkTestsBase
    {
        private static readonly MessageTemplateParser _messageTemplateParser = new MessageTemplateParser();

        public ExceptionAsJsonObjectFormatterTests() : base()
        { 

            _options.CustomFormatter = new ExceptionAsObjectJsonFormatter(renderMessage:true);
        }

        [Fact]
        public void WhenLoggingAnEvent_OutputsValidJson()
        {
            const string expectedMessage = "test";

            using (var sink = new ElasticsearchSink(_options))
            {
                sink.Emit(LogEventWithMessage(expectedMessage));
            }

            var eventWritten = AssertAndGetJsonEvents().First();
            eventWritten.Level.Should().Be(LogEventLevel.Warning);
            eventWritten.Message.Should().Be(expectedMessage);
        }

        [Fact]
        public void WhenLogging_WithException_ExceptionShouldBeRenderedInExceptionField()
        {
            const string expectedExceptionMessage = "test exception";

            using (var sink = new ElasticsearchSink(_options))
            {
                sink.Emit(LogEventWithMessage("test", new Exception(expectedExceptionMessage)));
            }

            var eventWritten = AssertAndGetJsonEvents().First();
            var exceptionInfo = eventWritten.Exception;
            exceptionInfo.Should().NotBeNull();
            exceptionInfo.Message.Should().Be(expectedExceptionMessage);
#if !DOTNETCORE
            exceptionInfo.ClassName.Should().Be("System.Exception");
#endif
        }

        [Fact]
        public void WhenLogging_ExceptionWithInner_ExceptionShouldIncludeInnerExceptions()
        {
            var inner = new InvalidOperationException();
            var exception = new Exception("outer", inner);

            using (var sink = new ElasticsearchSink(_options))
            {
                sink.Emit(LogEventWithMessage("test", exception));
            }

            var eventWritten = AssertAndGetJsonEvents().First();
            var exceptionInfo = eventWritten.Exception;
            exceptionInfo.InnerException.Should().NotBeNull();

        }

        private static LogEvent LogEventWithMessage(string expectedMessage, Exception exception = null)
        {
            var template = _messageTemplateParser.Parse(expectedMessage);
            return new LogEvent(DateTimeOffset.Now, LogEventLevel.Warning, exception, template, Enumerable.Empty<LogEventProperty>());
        }

        private IEnumerable<KibanaFriendlyJsonEvent> AssertAndGetJsonEvents()
        {
            _seenHttpPosts.Should().NotBeEmpty();
            return _seenHttpPosts.SelectMany(postedData => postedData.Split(new char[] { '\n'}, StringSplitOptions.RemoveEmptyEntries))
                .Where((_,i) => i % 2 == 1)
                .Select(JsonConvert.DeserializeObject<KibanaFriendlyJsonEvent>);
        }


        class KibanaFriendlyJsonEvent : IBulkData
        {
            [JsonProperty("@timestamp")]
            public DateTime Timestamp { get; set; }

            [JsonProperty("level")]
            [JsonConverter(typeof(StringEnumConverter))]
            public LogEventLevel Level { get; set; }

            [JsonProperty("messageTemplate")]
            public string MessageTemplate { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }

            [JsonProperty("exception")]
            public SerilogElasticsearchExceptionInfoWithInner Exception { get; set; }
        }

        class SerilogElasticsearchExceptionInfoWithInner : SerilogElasticsearchExceptionInfo
        {
            [JsonProperty("innerException")]
            public SerilogElasticsearchExceptionInfo InnerException { get; set; }
        }
    }


}