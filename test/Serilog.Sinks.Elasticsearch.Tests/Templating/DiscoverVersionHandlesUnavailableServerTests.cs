using System;
using System.IO;
using System.Text;
using FluentAssertions;
using Xunit;
using Serilog.Debugging;
using Serilog.Sinks.Elasticsearch.Tests.Stubs;

namespace Serilog.Sinks.Elasticsearch.Tests.Templating
{
    [Collection("isolation")]
    public class DiscoverVersionHandlesUnavailableServerTests : ElasticsearchSinkTestsBase
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
            selfLogContents.Should().Contain("Failed to discover the cluster version");

        }

        private static ILogger CreateLoggerThatCrashes()
        {
            var loggerConfig = new LoggerConfiguration()
                .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://localhost:9199"))
                {
                    DetectElasticsearchVersion = true
                });

            return loggerConfig.CreateLogger();
        }
    }
}