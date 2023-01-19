using Serilog.Events;
using Serilog.Parsing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Serilog.Formatting.Elasticsearch;
using Xunit;

namespace Serilog.Sinks.Elasticsearch.Tests
{
    public class ElasticsearchJsonFormatterTests
    {
        #region Helpers
        static LogEvent CreateLogEvent() =>
        new LogEvent
        (
            DateTimeOffset.Now,
            LogEventLevel.Debug,
           //exception: CreateThrownException(),
            exception: null,
            messageTemplate: new MessageTemplate(Enumerable.Empty<MessageTemplateToken>()),
            properties: Enumerable.Empty<LogEventProperty>()
        );

        static Exception CreateThrownException()
        {
            Exception retEx = null;
            try
            {
                ThrowException();
            }
            catch (Exception ex)
            {
                retEx = ex;
            }
            return retEx;
        }

        static void ThrowException()
        {
            try
            {
                ThrowInnerException();
            }
            catch(Exception ex)
            {
                throw new Exception("Test exception message", ex);
            }
        }

        static void ThrowInnerException()
        {
            throw new Exception("Test inner exception message");
        }

        static Exception CreateThrownExceptionWithNotThrownInner()
        {
            Exception retEx = null;
            try
            {
                throw new Exception("Test exception message", new Exception("Test inner exception message"));
            }
            catch (Exception ex)
            {
                retEx = ex;
            }
            return retEx;
        }

        static LogEvent CreateLogEventWithException(Exception ex) =>
        new LogEvent
        (
            DateTimeOffset.Now,
            LogEventLevel.Debug,
            exception: ex,
            messageTemplate: new MessageTemplate(Enumerable.Empty<MessageTemplateToken>()),
            properties: Enumerable.Empty<LogEventProperty>()
        );

        static void CheckProperties(Func<LogEvent> logEventProvider, ElasticsearchJsonFormatter formatter, Action<string> assert)
        {
            string result = null;

            var logEvent = logEventProvider();

            using (var stringWriter = new StringWriter())
            {
                formatter.Format(logEvent, stringWriter);

                result = stringWriter.ToString();
            }

            assert(result);
        }

        static void CheckProperties(ElasticsearchJsonFormatter formatter, Action<string> assert) =>
            CheckProperties(CreateLogEvent, formatter, assert);

        static void ContainsProperty(string propertyToCheck, string result) =>
            Assert.Contains
            (
                propertyToCheck,
                result,
                StringComparison.CurrentCultureIgnoreCase
            );
        static void DoesNotContainsProperty(string propertyToCheck, string result) =>
            Assert.DoesNotContain
            (
                propertyToCheck,
                result,
                StringComparison.CurrentCultureIgnoreCase
            );

        static string FormatProperty(string property) => $"\"{property}\":";
        #endregion

        [Theory]
        [InlineData(ElasticsearchJsonFormatter.RenderedMessagePropertyName)]
        [InlineData(ElasticsearchJsonFormatter.MessageTemplatePropertyName)]
        [InlineData(ElasticsearchJsonFormatter.TimestampPropertyName)]
        [InlineData(ElasticsearchJsonFormatter.LevelPropertyName)]
        public void DefaultJsonFormater_Should_Render_default_properties(string propertyToCheck)
        {
            CheckProperties(
                new ElasticsearchJsonFormatter(),
                result => ContainsProperty(FormatProperty(propertyToCheck), result));
        }

        [Fact]
        public void When_disabling_renderMessage_Should_not_render_message_but_others()
        {
            CheckProperties(
                new ElasticsearchJsonFormatter(renderMessage: false),
                result =>
                {
                    DoesNotContainsProperty(FormatProperty(ElasticsearchJsonFormatter.RenderedMessagePropertyName), result);
                    ContainsProperty(FormatProperty(ElasticsearchJsonFormatter.MessageTemplatePropertyName), result);
                    ContainsProperty(FormatProperty(ElasticsearchJsonFormatter.TimestampPropertyName), result);
                    ContainsProperty(FormatProperty(ElasticsearchJsonFormatter.LevelPropertyName), result);
                });
        }

        [Fact]
        public void When_disabling_renderMessageTemplate_Should_not_render_message_template_but_others()
        {
            CheckProperties(
                new ElasticsearchJsonFormatter(renderMessageTemplate: false),
                result =>
                {
                    DoesNotContainsProperty(FormatProperty(ElasticsearchJsonFormatter.MessageTemplatePropertyName), result);
                    ContainsProperty(FormatProperty(ElasticsearchJsonFormatter.RenderedMessagePropertyName), result);
                    ContainsProperty(FormatProperty(ElasticsearchJsonFormatter.TimestampPropertyName), result);
                    ContainsProperty(FormatProperty(ElasticsearchJsonFormatter.LevelPropertyName), result);
                });
        }

        [Fact]
        public void DefaultJsonFormater_Should_enclose_object()
        {
            CheckProperties(
                new ElasticsearchJsonFormatter(),
                result =>
                {
                    Assert.StartsWith("{", result);
                    Assert.EndsWith($"}}{Environment.NewLine}", result);
                });
        }

        [Fact]
        public void When_omitEnclosingObject_should_not_enclose_object()
        {
            CheckProperties(
                new ElasticsearchJsonFormatter(omitEnclosingObject: true),
                result =>
                {
                    Assert.StartsWith("\"", result);
                    Assert.EndsWith("\"", result);
                });
        }

        [Fact]
        public void When_provide_closing_delimiter_should_use_it()
        {
            CheckProperties(
                new ElasticsearchJsonFormatter(closingDelimiter: "closingDelimiter"),
                result =>
                {
                    Assert.EndsWith("closingDelimiter", result);
                });
        }

        [Fact]
        public void DefaultJsonFormater_Should_Render_Exceptions()
        {
            CheckProperties(
                () => CreateLogEventWithException(CreateThrownException()),
                new ElasticsearchJsonFormatter(),
                result =>
                {
                    string exceptionsProperty = FormatProperty("exceptions");
                    ContainsProperty(exceptionsProperty, result);

            
                    string exceptionsValue = result.Substring(result.IndexOf(exceptionsProperty) + exceptionsProperty.Length);

                    // Check the exceptions property is a JSON array
                    Assert.StartsWith("[", exceptionsValue);

                    string stackTraceProperty = FormatProperty("StackTraceString");
                    ContainsProperty(stackTraceProperty, exceptionsValue);
                    DoesNotContainsProperty(FormatProperty("StackTrace"), exceptionsValue);

                    string stackTraceValue = exceptionsValue.Substring(exceptionsValue.IndexOf(stackTraceProperty) + stackTraceProperty.Length);

                    // Check the StackTraceString property is a JSON string
                    Assert.StartsWith("\"", stackTraceValue);
                });
        }

        [Theory]
        [MemberData(nameof(TestExceptions))]
        public void DefaultJsonFormater_Should_Render_Exceptions_With_StackTrace_As_Array(Exception exception)
        {
            CheckProperties(
                () => CreateLogEventWithException(exception),
                new ElasticsearchJsonFormatter(formatStackTraceAsArray: true),
                result =>
                {
                    string exceptionsProperty = FormatProperty("exceptions");
                    ContainsProperty(exceptionsProperty, result);


                    string exceptionsValue = result.Substring(result.IndexOf(exceptionsProperty) + exceptionsProperty.Length);

                    // Check the exceptions property is a JSON array
                    Assert.StartsWith("[", exceptionsValue);

                    string stackTraceProperty = FormatProperty("StackTrace");
                    ContainsProperty(stackTraceProperty, exceptionsValue);
                    DoesNotContainsProperty(FormatProperty("StackTraceString"), exceptionsValue);

                    string stackTraceValue = exceptionsValue.Substring(exceptionsValue.IndexOf(stackTraceProperty) + stackTraceProperty.Length);

                    // Check the StackTrace property is a JSON array
                    Assert.StartsWith("[", stackTraceValue);
                });
        }

        public static IEnumerable<object[]> TestExceptions =>
            new List<object[]>
            {
                new object[] { CreateThrownException() },
                new object[] { CreateThrownExceptionWithNotThrownInner() },
                new object[] { new Exception("Not thrown exception message") }
            };
    }
}
