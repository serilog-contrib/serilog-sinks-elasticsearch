using System.Linq;
using Elastic.Managed.Ephemeral;
using Elastic.Xunit;
using Elastic.Xunit.XunitPlumbing;
using FluentAssertions;
using Nest;
using Serilog.Core;
using Serilog.Sinks.Elasticsearch.IntegrationTests.Clusters;
using Xunit;

namespace Serilog.Sinks.Elasticsearch.IntegrationTests
{
    public class Elasticsearch7XSetup
    {
        public const string IndexPrefix = "logs-7x-";
        public const string TemplateName = "serilog-logs-7x";
        public Elasticsearch7XSetup()
        {
            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.ColoredConsole()
                .WriteTo.Elasticsearch(new ElasticsearchSinkOptions
                {
                    
                    IndexFormat = IndexPrefix + "{0:yyyy.MM.dd}",
                    TemplateName = TemplateName,
                    AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
                    AutoRegisterTemplate = true
                });
            var logger = loggerConfig.CreateLogger();
            
            logger.Information("Hello Information");
            logger.Debug("Hello Debug");
            logger.Warning("Hello Warning");
            logger.Error("Hello Error");
            logger.Fatal("Hello Fatal");

            logger.Dispose();
        }

    }


    public class Elasticsearch7X : IClusterFixture<Elasticsearch7XCluster>, IClassFixture<Elasticsearch7XSetup>
    {
        private readonly Elasticsearch7XCluster _cluster;
        private IElasticClient _client;

        public Elasticsearch7X(Elasticsearch7XCluster cluster, Elasticsearch7XSetup setup)
        {
            _cluster = cluster;
            _client = cluster.Client;
        }


        [I] public void AssertTemplate()
        {
            var templateResponse = _client.Indices.GetTemplate(Elasticsearch7XSetup.TemplateName);
            templateResponse.TemplateMappings.Should().NotBeEmpty();
            templateResponse.TemplateMappings.Keys.Should().Contain(Elasticsearch7XSetup.TemplateName);

            var template = templateResponse.TemplateMappings[Elasticsearch7XSetup.TemplateName];

            template.IndexPatterns.Should().Contain(pattern => pattern.StartsWith(Elasticsearch7XSetup.IndexPrefix));

        }
        [I] public void AssertLogs()
        {
            var refreshed = _client.Indices.Refresh(Elasticsearch7XSetup.IndexPrefix + "*");

            var search = _client.Search<object>(s => s.Index(Elasticsearch7XSetup.IndexPrefix + "*"));

            // Informational should be filtered
            search.Documents.Count().Should().Be(4);


        }
    }
}