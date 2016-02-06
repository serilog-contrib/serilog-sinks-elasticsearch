using System;
using FluentAssertions;
using Xunit;

namespace Serilog.Sinks.Elasticsearch.Tests.Templating
{
    public class TemplateMatchTests : ElasticsearchSinkTestsBase
    {
        private readonly Tuple<Uri, string> _templatePut;

        public TemplateMatchTests()
        {
            _options.AutoRegisterTemplate = true;
            _options.IndexFormat = "dailyindex-{0:yyyy.MM.dd}-mycompany";
            _options.TemplateName = "dailyindex-logs-template";
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

            _seenHttpPosts.Should().NotBeNullOrEmpty().And.HaveCount(1);
            _seenHttpPuts.Should().NotBeNullOrEmpty().And.HaveCount(1);
            _seenHttpHeads.Should().NotBeNullOrEmpty().And.HaveCount(1);
            _templatePut = _seenHttpPuts[0];
            
        }

        [Fact]
        public void TemplatePutToCorrectUrl()
        {
            var uri = _templatePut.Item1;
            uri.AbsolutePath.Should().Be("/_template/dailyindex-logs-template");
        }

        [Fact]
        public void TemplateMatchShouldReflectConfiguredIndexFormat()
        {
            var json = _templatePut.Item2;
            json.Should().Contain(@"""template"":""dailyindex-*-mycompany""");
        }
    }
}
