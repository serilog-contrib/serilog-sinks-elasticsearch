using System;
using System.Runtime.Serialization;
using Elasticsearch.Net.Serialization;
using FluentAssertions;

namespace Serilog.Sinks.Elasticsearch.Tests.Discrepancies
{
    public class ElasticsearchSinkUniformityTestsBase : ElasticsearchSinkTestsBase
    {
        public ElasticsearchSinkUniformityTestsBase(IElasticsearchSerializer serializer)
        {
            _options.Serializer = serializer;
        }

        public void ThrowAndLogAndCatchBulkOutput(string exceptionMessage)
        {
            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.WithMachineName()
                .WriteTo.ColoredConsole()
                .WriteTo.Elasticsearch(_options);

            var logger = loggerConfig.CreateLogger();
            using (logger as IDisposable)
            {
                try
                {
                    try
                    {
                        throw new Exception("inner most exception");
                    }
                    catch (Exception e)
                    {
                        var innerException = new NastyException("nasty inner exception", e);
                        throw new Exception(exceptionMessage, innerException);
                    }
                }
                catch (Exception e)
                {
                    logger.Error(e, "Test exception. Should contain an embedded exception object.");
                }
                logger.Error("Test exception. Should not contain an embedded exception object.");
            }

            var postedEvents = this.GetPostedLogEvents(expectedCount: 2);
            Console.WriteLine("BULK OUTPUT BEGIN ==========");
            foreach (var post in _seenHttpPosts)
                Console.WriteLine(post);
            Console.WriteLine("BULK OUTPUT END ============");

            var firstEvent = postedEvents[0];
            firstEvent.Exceptions.Should().NotBeNull().And.HaveCount(3);
            firstEvent.Exceptions[0].Message.Should().NotBeNullOrWhiteSpace()
                .And.Be(exceptionMessage);
            var realException = firstEvent.Exceptions[0];
            realException.ExceptionMethod.Should().NotBeNull();
            realException.ExceptionMethod.Name.Should().NotBeNullOrWhiteSpace();
            realException.ExceptionMethod.AssemblyName.Should().NotBeNullOrWhiteSpace();
            realException.ExceptionMethod.AssemblyVersion.Should().NotBeNullOrWhiteSpace();
            realException.ExceptionMethod.ClassName.Should().NotBeNullOrWhiteSpace();
            realException.ExceptionMethod.Signature.Should().NotBeNullOrWhiteSpace();
            realException.ExceptionMethod.MemberType.Should().BeGreaterThan(0);

            var nastyException = firstEvent.Exceptions[1];
            nastyException.Depth.Should().Be(1);
            nastyException.Message.Should().Be("nasty inner exception");
            nastyException.HelpUrl.Should().Be("help url");
            nastyException.StackTraceString.Should().Be("stack trace string");
            nastyException.RemoteStackTraceString.Should().Be("remote stack trace string");
            nastyException.RemoteStackIndex.Should().Be(1);
            nastyException.HResult.Should().Be(123123);
            nastyException.Source.Should().Be("source");
            nastyException.ClassName.Should().Be("classname nasty exception");
            //nastyException.WatsonBuckets.Should().BeEquivalentTo(new byte[] {1,2,3});


            var secondEvent = postedEvents[1];
            secondEvent.Exceptions.Should().BeNullOrEmpty();
        }
    }

    /// <summary>
    /// Exception that forces often empty serializationinfo values to have a value
    /// </summary>
    public class NastyException : Exception
    {
        public NastyException(string message) : base(message) { }
        public NastyException(string message, Exception innerException) : base(message, innerException) { }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Message", this.Message);
            info.AddValue("HelpURL", "help url");
            info.AddValue("StackTraceString", "stack trace string");
            info.AddValue("RemoteStackTraceString", "remote stack trace string");
            info.AddValue("RemoteStackIndex", 1);
            info.AddValue("ExceptionMethod", "exception method");
            info.AddValue("HResult", 123123);
            info.AddValue("Source", "source");
            info.AddValue("ClassName", "classname nasty exception");
            info.AddValue("WatsonBuckets", new byte[] { 1, 2, 3 }, typeof(byte[]));
        }
    }
}