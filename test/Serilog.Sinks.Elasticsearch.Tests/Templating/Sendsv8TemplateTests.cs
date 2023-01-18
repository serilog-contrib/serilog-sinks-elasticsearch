﻿using System;
using System.Reflection;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Serilog.Sinks.Elasticsearch.Tests.Templating
{
    public class Sendsv8TemplateTests : ElasticsearchSinkTestsBase
    {
        private readonly Tuple<Uri, string> _templatePut;

        public Sendsv8TemplateTests()
        {
            _options.AutoRegisterTemplate = true;
            _options.AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv8;
            _options.IndexAliases = new string[] { "logstash" };

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
            JsonEquals(_templatePut.Item2, "template_v8.json");
        }

        [Fact]
        public void TemplatePutToCorrectUrl()
        {
            var uri = _templatePut.Item1;
            uri.AbsolutePath.Should().Be("/_template/serilog-events-template");
        }

        protected void JsonEquals(string json, string fileName = null)
        {
#if DOTNETCORE
            var assembly = typeof(Sendsv8TemplateTests).GetTypeInfo().Assembly;
#else
            var assembly = Assembly.GetExecutingAssembly();
#endif
            var expected = TestDataHelper.ReadEmbeddedResource(assembly, fileName ?? "template.json");

            var nJson = JObject.Parse(json);
            var nOtherJson = JObject.Parse(expected);
            var equals = JToken.DeepEquals(nJson, nOtherJson);
            if (equals) return;
            expected.Should().BeEquivalentTo(json);
        }
    }
}