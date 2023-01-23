using System.Linq;
using Elastic.Elasticsearch.Xunit.XunitPlumbing;
using FluentAssertions;
using Serilog.Sinks.Elasticsearch.IntegrationTests.Bootstrap;
using Serilog.Sinks.Elasticsearch.IntegrationTests.Elasticsearch6.Bootstrap;
using Xunit;

namespace Serilog.Sinks.Elasticsearch.IntegrationTests.Elasticsearch6
{
    public class Elasticsearch6XUsing7X : Elasticsearch6XTestBase, IClassFixture<Elasticsearch6XUsing7X.SetupSerilog>
    {
        private readonly SetupSerilog _setup;

        public Elasticsearch6XUsing7X(Elasticsearch6XCluster cluster, SetupSerilog setup) : base(cluster) => _setup = setup;

        [I] public void AssertTemplate()
        {
            var templateResponse = Client.GetIndexTemplate(t=>t.Name(SetupSerilog.TemplateName));
            templateResponse.TemplateMappings.Should().NotBeEmpty();
            templateResponse.TemplateMappings.Keys.Should().Contain(SetupSerilog.TemplateName);

            var template = templateResponse.TemplateMappings[SetupSerilog.TemplateName];

            template.IndexPatterns.Should().Contain(pattern => pattern.StartsWith(SetupSerilog.IndexPrefix));
        }

        [I] public void AssertLogs()
        {
            var refreshed = Client.Refresh(SetupSerilog.IndexPrefix + "*");

            var search = Client.Search<object>(s => s
                .Index(SetupSerilog.IndexPrefix + "*")
                .Type("_doc")
            );

            // Informational should be filtered
            search.Documents.Count().Should().Be(4);
        }

        // ReSharper disable once ClassNeverInstantiated.Global
        public class SetupSerilog
        {
            public const string IndexPrefix = "logs-6x-using-7x-";
            public const string TemplateName = "serilog-logs-6x-using-7x";

            public SetupSerilog()
            {
                var loggerConfig = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.Console()
                    .WriteTo.Elasticsearch(
                        ElasticsearchSinkOptionsFactory.Create(IndexPrefix, TemplateName, o =>
                        {
                            o.DetectElasticsearchVersion = true;
                            o.AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7;
                            o.AutoRegisterTemplate = true;
                        })
                    );
                var logger = loggerConfig.CreateLogger();

                logger.Information("Hello Information");
                logger.Debug("Hello Debug");
                logger.Warning("Hello Warning");
                logger.Error("Hello Error");
                logger.Fatal("Hello Fatal");

                logger.Dispose();
            }
        }
    }
}