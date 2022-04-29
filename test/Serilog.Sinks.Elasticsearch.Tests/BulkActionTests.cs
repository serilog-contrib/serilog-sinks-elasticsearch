using System;
using System.Linq;
using FluentAssertions;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace Serilog.Sinks.Elasticsearch.Tests
{
    public class BulkActionTests : ElasticsearchSinkTestsBase
    {
        [Fact]
        public void DefaultBulkActionV7()
        {
            _options.IndexFormat = "logs";
            _options.TypeName = "_doc";
            _options.PipelineName = null;
            using (var sink = new ElasticsearchSink(_options))
            {
                sink.Emit(ADummyLogEvent());
                sink.Emit(ADummyLogEvent());
            }

            var bulkJsonPieces = this.AssertSeenHttpPosts(_seenHttpPosts, 2, 1);
            const string expectedAction = @"{""index"":{""_type"":""_doc"",""_index"":""logs""}}";
            bulkJsonPieces[0].Should().Be(expectedAction);
        }

        [Fact(Skip = "Flaky test on GitHub actions")]
        public void BulkActionV7OverrideTypeName()
        {
            _options.IndexFormat = "logs";
            _options.TypeName = "logevent"; // This is the default value when creating the sink via configuration
            _options.AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7;
            _options.PipelineName = null;
            using (var sink = new ElasticsearchSink(_options))
            {
                sink.Emit(ADummyLogEvent());
                sink.Emit(ADummyLogEvent());
            }

            var bulkJsonPieces = this.AssertSeenHttpPosts(_seenHttpPosts, 2, 1);
            const string expectedAction = @"{""index"":{""_type"":""_doc"",""_index"":""logs""}}";
            bulkJsonPieces[0].Should().Be(expectedAction);
        }        
        
        [Fact]
        public void DefaultBulkActionV8()
        {
            _options.IndexFormat = "logs";
            _options.TypeName = null;
            _options.PipelineName = null;
            using (var sink = new ElasticsearchSink(_options))
            {
                sink.Emit(ADummyLogEvent());
                sink.Emit(ADummyLogEvent());
            }

            var bulkJsonPieces = this.AssertSeenHttpPosts(_seenHttpPosts, 2, 1);
            const string expectedAction = @"{""index"":{""_index"":""logs""}}";
            bulkJsonPieces[0].Should().Be(expectedAction);
        }


        [Fact(Skip = "Flaky test on GitHub actions")]
        public void BulkActionDataStreams()
        {
            _options.IndexFormat = "logs-my-stream";
            _options.TypeName = null;
            _options.PipelineName = null;
            _options.BatchAction = ElasticOpType.Create;
            
            using (var sink = new ElasticsearchSink(_options))
            {
                sink.Emit(ADummyLogEvent());
                sink.Emit(ADummyLogEvent());
            }

            var bulkJsonPieces = this.AssertSeenHttpPosts(_seenHttpPosts, 2, 1);
            const string expectedAction = @"{""create"":{""_index"":""logs-my-stream""}}";
            bulkJsonPieces[0].Should().Be(expectedAction);
        }
        
        [Fact]
        public void PipelineAction()
        {
            _options.IndexFormat = "logs-my-stream";
            _options.TypeName = "_doc";
            _options.PipelineName = "my-pipeline";
            _options.BatchAction = ElasticOpType.Index;
            
            using (var sink = new ElasticsearchSink(_options))
            {
                sink.Emit(ADummyLogEvent());
                sink.Emit(ADummyLogEvent());
            }

            var bulkJsonPieces = this.AssertSeenHttpPosts(_seenHttpPosts, 2, 1);
            const string expectedAction = @"{""index"":{""_type"":""_doc"",""_index"":""logs-my-stream"",""pipeline"":""my-pipeline""}}";
            bulkJsonPieces[0].Should().Be(expectedAction);
        }

        private static LogEvent ADummyLogEvent() {
            return new LogEvent(DateTimeOffset.Now, LogEventLevel.Information, null,
                new MessageTemplate("A template", Enumerable.Empty<MessageTemplateToken>()),
                Enumerable.Empty<LogEventProperty>());
        }
    }
}