using System;
using System.IO;
using System.Reflection;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Serilog.Sinks.Elasticsearch.Tests.Templating
{
    [TestFixture]
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

            this._seenHttpPosts.Should().NotBeNullOrEmpty().And.HaveCount(1);
            this._seenHttpPuts.Should().NotBeNullOrEmpty().And.HaveCount(1);
            _templatePut = this._seenHttpPuts[0];
            
        }


        [Test]
        public void ShouldRegisterTheCorrectTemplateOnRegistration()
        {
            this.JsonEquals(this._templatePut.Item2, MethodBase.GetCurrentMethod(), "template");
        }

        [Test]
        public void TemplatePutToCorrectUrl()
        {
            var uri = this._templatePut.Item1;
            uri.AbsolutePath.Should().Be("/_template/serilog-events-template");
        }

		protected void JsonEquals(string json, MethodBase method, string fileName = null)
		{
			var file = this.GetFileFromMethod(method, fileName);
		    var exists = File.Exists(file);
		    exists.Should().BeTrue(file + "does not exist");

			var expected = File.ReadAllText(file);
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