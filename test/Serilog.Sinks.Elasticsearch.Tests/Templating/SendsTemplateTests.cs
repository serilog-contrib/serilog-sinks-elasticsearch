using System;
using System.IO;
using System.Reflection;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Serilog.Sinks.Elasticsearch.Tests.Templating
{
    public class SendsTemplateTests : ElasticsearchSinkTestsBase
    {
        private readonly Tuple<Uri, string> _templatePut;

        public SendsTemplateTests()
        {
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

            _seenHttpPosts.Should().NotBeNullOrEmpty().And.HaveCount(1);
            _seenHttpPuts.Should().NotBeNullOrEmpty().And.HaveCount(1);
            _templatePut = _seenHttpPuts[0];
        }

        [Fact(Skip = "Not successfully on AppVeyor")]
        public void ShouldRegisterTheCorrectTemplateOnRegistration()
        {
            JsonEquals(_templatePut.Item2, MethodBase.GetCurrentMethod(), "template");
        }

        [Fact]
        public void TemplatePutToCorrectUrl()
        {
            var uri = _templatePut.Item1;
            uri.AbsolutePath.Should().Be("/_template/serilog-events-template");
        }

        protected void JsonEquals(string json, MethodBase method, string fileName = null)
        {
            var file = GetFileFromMethod(method, fileName);
            var exists = System.IO.File.Exists(file);
            exists.Should().BeTrue(file + " exist");

            var expected = System.IO.File.ReadAllText(file);
            var nJson = JObject.Parse(json);
            var nOtherJson = JObject.Parse(expected);
            var equals = JToken.DeepEquals(nJson, nOtherJson);
            if (equals) return;
            expected.Should().BeEquivalentTo(json);

        }

        protected string GetFileFromMethod(MethodBase method, string fileName)
        {
            var type = method.DeclaringType;
            var @namespace = method.DeclaringType.Namespace;
            var folderSep = Path.DirectorySeparatorChar.ToString();
            var folder = @namespace.Replace("Serilog.Sinks.Elasticsearch.Tests.", "").Replace(".", folderSep);
            var file = Path.Combine(folder, (fileName ?? method.Name).Replace(@"\", folderSep) + ".json");
            file = Path.Combine(Environment.CurrentDirectory.Replace("bin" + folderSep + "Debug", "").Replace("bin" + folderSep + "Release", ""), file);
            return file;
        }
    }
}
