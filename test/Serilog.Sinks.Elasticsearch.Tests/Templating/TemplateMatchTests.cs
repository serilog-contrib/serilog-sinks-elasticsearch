using System;
using System.IO;
using System.Reflection;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Serilog.Sinks.Elasticsearch.Tests.Templating
{
    [TestFixture]
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

            this._seenHttpPosts.Should().NotBeNullOrEmpty().And.HaveCount(1);
            this._seenHttpPuts.Should().NotBeNullOrEmpty().And.HaveCount(1);
            _templatePut = this._seenHttpPuts[0];
            
        }

        [Test]
        public void TemplatePutToCorrectUrl()
        {
            var uri = this._templatePut.Item1;
            uri.AbsolutePath.Should().Be("/_template/dailyindex-logs-template");
        }

        [Test]
        public void TemplateMatchShouldReflectConfiguredIndexFormat()
        {
            var json = this._templatePut.Item2;
            json.Should().Contain(@"""template"":""dailyindex-*-mycompany""");
        }

    }
}