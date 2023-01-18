using System;
using System.IO;
using System.Reflection;
using FluentAssertions;
using Newtonsoft.Json.Linq;
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
                .WriteTo.Console()
                .WriteTo.Elasticsearch(_options);

            var logger = loggerConfig.CreateLogger();
            using (logger as IDisposable)
            {
                logger.Error("Test exception. Should not contain an embedded exception object.");
            }

            this._seenHttpPosts.Should().NotBeNullOrEmpty().And.HaveCount(1);
            this._seenHttpPuts.Should().NotBeNullOrEmpty().And.HaveCount(1);
            this._seenHttpHeads.Should().NotBeNullOrEmpty().And.HaveCount(1);
            _templatePut = this._seenHttpPuts[0];
            
        }

        [Fact]
        public void TemplatePutToCorrectUrl()
        {
            var uri = this._templatePut.Item1;
            uri.AbsolutePath.Should().Be("/_template/dailyindex-logs-template");
        }

        [Fact]
        public void TemplateMatchShouldReflectConfiguredIndexFormat()
        {
            var json = this._templatePut.Item2;
            json.Should().Contain(@"""template"":""dailyindex-*-mycompany""");
        }

    }
}