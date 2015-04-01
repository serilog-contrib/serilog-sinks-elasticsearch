using System;
using System.IO;
using System.Reflection;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Serilog.Sinks.Elasticsearch.Tests.Templating
{
    [TestFixture]
    public class DoNotRegisterIfTemplateExistsTests : ElasticsearchSinkTestsBase
    {
       
        public DoNotRegisterIfTemplateExistsTests()
        {
            _templateExistsReturnCode = 200;

            _options.AutoRegisterTemplate = true;
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
        }

        [Test]
        public void ShoudNotSendAPutTemplate()
        {
            this._seenHttpPosts.Should().NotBeNullOrEmpty().And.HaveCount(1);
            this._seenHttpHeads.Should().NotBeNullOrEmpty().And.HaveCount(1);
            this._seenHttpPuts.Should().BeNullOrEmpty();
        }


    }
}