using System;
using System.IO;
using System.Text;
using FluentAssertions;
using Xunit;
using Serilog.Debugging;

namespace Serilog.Sinks.Elasticsearch.Tests.Templating
{
    [Collection("isolation")]
    public class SendsTemplateHandlesUnavailableServerTests : ElasticsearchSinkTestsBase
    {
        [Fact]
        public void Should_not_crash_when_server_is_unavaiable()
        {
            // If this crashes, the test will fail
            CreateLoggerThatCrashes();
        }

        [Fact]
        public void Should_write_error_to_self_log()
        {
            var selfLogMessages = new StringBuilder();
            SelfLog.Enable(new StringWriter(selfLogMessages));

            // Exception occurs on creation - should be logged
            CreateLoggerThatCrashes();

            var selfLogContents = selfLogMessages.ToString();
            selfLogContents.Should().Contain("Failed to create the template");
#if !DOTNETCORE
            selfLogContents.Should().Contain("WebException");
#else
                selfLogContents.Should().Contain("HttpRequestException");
#endif

        }

        private static ILogger CreateLoggerThatCrashes()
        {
            var loggerConfig = new LoggerConfiguration()
                .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://localhost:31234"))
                {
                    AutoRegisterTemplate = true,
                    TemplateName = "crash"
                });

            return loggerConfig.CreateLogger();
        }
    }
}