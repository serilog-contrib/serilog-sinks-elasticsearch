using System;
using FluentAssertions;
using Xunit;

namespace Serilog.Sinks.Elasticsearch.Tests.Templating
{
    public class DiscoverVersionTests : ElasticsearchSinkTestsBase
    {
        private readonly Tuple<Uri,int> _templateGet;

        public DiscoverVersionTests()
        {
            _options.DetectElasticsearchVersion = true;

            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.WithMachineName()
                .WriteTo.Console()
                .WriteTo.Elasticsearch(_options);

            var logger = loggerConfig.CreateLogger();
            using ((IDisposable) logger)
            {
                logger.Error("Test exception. Should not contain an embedded exception object.");
            }

            this._seenHttpGets.Should().NotBeNullOrEmpty().And.HaveCount(1);
            _templateGet = this._seenHttpGets[0];
        }

       
        [Fact]
        public void TemplatePutToCorrectUrl()
        {
            var uri = _templateGet.Item1;
            uri.AbsolutePath.Should().Be("/_cat/nodes");
        }
    }
}