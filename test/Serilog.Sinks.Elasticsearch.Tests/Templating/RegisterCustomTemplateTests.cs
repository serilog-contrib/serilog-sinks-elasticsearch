using System;
using FluentAssertions;
using Xunit;

namespace Serilog.Sinks.Elasticsearch.Tests.Templating
{
    public class RegisterCustomTemplateTests : ElasticsearchSinkTestsBase
    {
        private const string CustomTemplateContent = @"{ template: ""my-custom-template-*"" }";
        private readonly Tuple<Uri, string> _templatePut;

        public RegisterCustomTemplateTests()
        {
            _options.AutoRegisterTemplate = true;
            _options.GetTemplateContent = () => CustomTemplateContent;
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
        public void ShouldRegisterCustomTemplate()
        {
            this._templatePut.Item2.Should().BeEquivalentTo(CustomTemplateContent);
        }
    }
}