using System;
using FluentAssertions;
using Serilog.Sinks.Elasticsearch.Tests.Stubs;
using Xunit;

namespace Serilog.Sinks.Elasticsearch.Tests.Templating
{
    public class SendsTemplateTests : ElasticsearchSinkTestsBase
    {
        private readonly Tuple<Uri, string> _templatePut;

        public SendsTemplateTests()
        {
            _options.DetectElasticsearchVersion = false;
            _options.AutoRegisterTemplate = true;

            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.WithMachineName()
                .WriteTo.Console()
                .WriteTo.Elasticsearch(_options);

            var logger = loggerConfig.CreateLogger();
            using (logger as IDisposable)
            {
                logger.Error("Test exception. Should not contain an embedded exception object.");
            }

            this._seenHttpPosts.Should().NotBeNullOrEmpty().And.HaveCount(1);
            this._seenHttpPuts.Should().NotBeNullOrEmpty().And.HaveCount(1);
            _templatePut = this._seenHttpPuts[0];
        }

        [Fact]
        public void ShouldRegisterTheVersion7TemplateOnRegistrationWhenDetectElasticsearchVersionFalse()
        {
            JsonEquals(_templatePut.Item2, "template_v7_no-aliases.json");
        }

        [Fact]
        public void TemplatePutToCorrectUrl()
        {
            var uri = _templatePut.Item1;
            uri.AbsolutePath.Should().Be("/_template/serilog-events-template");
            uri.Query.Should().Be(string.Empty);
        }
    }
}