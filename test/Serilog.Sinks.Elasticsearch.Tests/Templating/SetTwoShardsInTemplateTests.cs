using System;
using FluentAssertions;
using Serilog.Sinks.Elasticsearch.Tests.Stubs;
using Xunit;

namespace Serilog.Sinks.Elasticsearch.Tests.Templating
{
    public class SetTwoShardsInTemplateTests : ElasticsearchSinkTestsBase
    {
        private readonly Tuple<Uri, string> _templatePut;

        public SetTwoShardsInTemplateTests()
        {
            _options.AutoRegisterTemplate = true;
            _options.NumberOfShards = 2;
            _options.NumberOfReplicas= 0;

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
        public void ShouldRegisterTheCorrectTemplateOnRegistration()
        {
            JsonEquals(_templatePut.Item2, "template_v8_no-aliases_2shards.json");
        }

        [Fact]
        public void TemplatePutToCorrectUrl()
        {
            var uri = _templatePut.Item1;
            uri.AbsolutePath.Should().Be("/_index_template/serilog-events-template");
        }
    }
}