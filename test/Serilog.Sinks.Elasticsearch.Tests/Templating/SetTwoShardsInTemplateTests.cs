using System;
using System.Reflection;
using FluentAssertions;
using Newtonsoft.Json.Linq;
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

        [Fact]
        public void ShouldRegisterTheCorrectTemplateOnRegistration()
        {
            var method = typeof(SendsTemplateTests).GetMethod(nameof(ShouldRegisterTheCorrectTemplateOnRegistration));
            JsonEquals(_templatePut.Item2, method, "template");
        }

        [Fact]
        public void TemplatePutToCorrectUrl()
        {
            var uri = _templatePut.Item1;
            uri.AbsolutePath.Should().Be("/_template/serilog-events-template");
        }

        protected void JsonEquals(string json, MethodBase method, string fileName = null)
        {
#if DOTNETCORE
            var assembly = typeof(SendsTemplateTests).GetTypeInfo().Assembly;
#else
            var assembly = Assembly.GetExecutingAssembly();
#endif
            var expected = TestDataHelper.ReadEmbeddedResource(assembly, "template_2shards.json");

            var nJson = JObject.Parse(json);
            var nOtherJson = JObject.Parse(expected);
            var equals = JToken.DeepEquals(nJson, nOtherJson);
            if (equals) return;
            expected.Should().BeEquivalentTo(json);
        }
    }
}