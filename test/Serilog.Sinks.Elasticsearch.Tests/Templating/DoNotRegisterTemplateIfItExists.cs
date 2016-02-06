using System;
using FluentAssertions;
using Xunit;

namespace Serilog.Sinks.Elasticsearch.Tests.Templating
{
    public class DoNotRegisterIfTemplateExistsTests : ElasticsearchSinkTestsBase
    {
        public DoNotRegisterIfTemplateExistsTests()
        {
            _templateExistsReturnCode = 200;

            _options.AutoRegisterTemplate = true;
            _options.Connection = this._connection;
            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.WithMachineName()
                .WriteTo.ColoredConsole()
                .WriteTo.Elasticsearch(_options);

            var logger = loggerConfig.CreateLogger();
            using (logger as IDisposable)
            {
                logger.Error("Test exception. Should not contain an embedded exception object.");
            }
        }

        [Fact]
        public void ShoudNotSendAPutTemplate()
        {
            _seenHttpPosts.Should().NotBeNullOrEmpty().And.HaveCount(1);
            _seenHttpHeads.Should().NotBeNullOrEmpty().And.HaveCount(1);
            _seenHttpPuts.Should().BeNullOrEmpty();
        }
    }
}