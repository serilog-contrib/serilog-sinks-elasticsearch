using System.Linq;
using Elastic.Elasticsearch.Xunit.XunitPlumbing;
using FluentAssertions;
using Serilog.Sinks.Elasticsearch.IntegrationTests.Bootstrap;
using Serilog.Sinks.Elasticsearch.IntegrationTests.Elasticsearch7.Bootstrap;
using Xunit;

namespace Serilog.Sinks.Elasticsearch.IntegrationTests.Elasticsearch7
{
    public class Elasticsearch7X : Elasticsearch7XTestBase, IClassFixture<Elasticsearch7X.SetupSerilog>
    {
        private readonly SetupSerilog _setup;

        public Elasticsearch7X(Elasticsearch7XCluster cluster, SetupSerilog setup) : base(cluster) => _setup = setup;

        [I] public void AssertTemplate()
        {
            var templateResponse = Client.Indices.GetTemplate(SetupSerilog.TemplateName);
            templateResponse.TemplateMappings.Should().NotBeEmpty();
            templateResponse.TemplateMappings.Keys.Should().Contain(SetupSerilog.TemplateName);

            var template = templateResponse.TemplateMappings[SetupSerilog.TemplateName];

            template.IndexPatterns.Should().Contain(pattern => pattern.StartsWith(SetupSerilog.IndexPrefix));
        }

        [I] public void AssertLogs()
        {
            var refreshed = Client.Indices.Refresh(SetupSerilog.IndexPrefix + "*");

            var search = Client.Search<object>(s => s.Index(SetupSerilog.IndexPrefix + "*"));

            // Informational should be filtered
            search.Documents.Count().Should().Be(4);
        }
        
        // ReSharper disable once ClassNeverInstantiated.Global
        public class SetupSerilog
        {
            public const string IndexPrefix = "logs-7x-default-";
            public const string TemplateName = "serilog-logs-7x";

            public SetupSerilog()
            {
                var loggerConfig = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.Console()
                    .WriteTo.Elasticsearch(
                        ElasticsearchSinkOptionsFactory.Create(IndexPrefix, TemplateName, o =>
                        {
                            o.AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7;
                            o.AutoRegisterTemplate = true;
                        })
                    );
                using (var logger = loggerConfig.CreateLogger())
                {
                    logger.Information("Hello Information");
                    logger.Debug("Hello Debug");
                    logger.Warning("Hello Warning");
                    logger.Error("Hello Error");
                    logger.Fatal("Hello Fatal");
                }
            }
        }

    }
}