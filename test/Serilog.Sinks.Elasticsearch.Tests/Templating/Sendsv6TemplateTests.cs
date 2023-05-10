using System;
using FluentAssertions;
using Serilog.Sinks.Elasticsearch.Tests.Stubs;
using Xunit;

namespace Serilog.Sinks.Elasticsearch.Tests.Templating
{
    public class Sendsv6TemplateTests : ElasticsearchSinkTestsBase
    {
        private readonly Tuple<Uri, string> _templatePut;

        public Sendsv6TemplateTests()
            : base("6.0.0")
        {
            _options.AutoRegisterTemplate = true;
            _options.AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv6;

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
        public void ShouldRegisterTheVersion6TemplateOnRegistrationWhenDetectedElasticsearchVersionIsV6()
        {
            JsonEquals(_templatePut.Item2, "template_v6.json");
        }

        [Fact]
        public void TemplatePutToCorrectUrl()
        {
            var uri = _templatePut.Item1;
            uri.AbsolutePath.Should().Be("/_template/serilog-events-template");
            uri.Query.Should().Be("?include_type_name=true");
        }
    }
}