using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elasticsearch.Net;
using FluentAssertions;
using Nest;
using Xunit;
using Serilog.Debugging;
using Serilog.Sinks.Elasticsearch.Tests.Domain;
using Nest.JsonNetSerializer;
using System.Collections;
using System.Threading;

namespace Serilog.Sinks.Elasticsearch.Tests
{
    public abstract class ElasticsearchSinkTestsBase
    {
        static readonly TimeSpan TinyWait = TimeSpan.FromMilliseconds(50);
        protected readonly IConnection _connection;
        protected readonly ElasticsearchSinkOptions _options;
        protected List<string> _seenHttpPosts = new List<string>();
        protected List<int> _seenHttpHeads = new List<int>();
        protected List<Tuple<Uri, string>> _seenHttpPuts = new List<Tuple<Uri, string>>();
        private IElasticsearchSerializer _serializer;

        protected int _templateExistsReturnCode = 404;

        protected ElasticsearchSinkTestsBase()
        {
            _seenHttpPosts = new List<string>();
            _seenHttpHeads = new List<int>();
            _seenHttpPuts = new List<Tuple<Uri, string>>();

            var connectionPool = new SingleNodeConnectionPool(new Uri("http://localhost:9200"));
            _connection = new ConnectionStub(_seenHttpPosts, _seenHttpHeads, _seenHttpPuts, () => _templateExistsReturnCode);
            _serializer = JsonNetSerializer.Default(LowLevelRequestResponseSerializer.Instance, new ConnectionSettings(connectionPool, _connection));

            _options = new ElasticsearchSinkOptions(connectionPool)
            {
                BatchPostingLimit = 2,
                //Period = TinyWait,
                Connection = _connection,
                Serializer = _serializer,
                PipelineName = "testPipe",
            };
        }

        /// <summary>
        /// Returns the posted serilog messages and validates the entire bulk in the process
        /// </summary>
        /// <param name="expectedCount"></param>
        /// <returns></returns>
        protected IList<SerilogElasticsearchEvent> GetPostedLogEvents(int expectedCount)
        {
            this._seenHttpPosts.Should().NotBeNullOrEmpty();
            var totalBulks = this._seenHttpPosts.SelectMany(p => p.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries)).ToList();
            totalBulks.Should().NotBeNullOrEmpty().And.HaveCount(expectedCount * 2);

            var bulkActions = new List<SerilogElasticsearchEvent>();
            for (var i = 0; i < totalBulks.Count; i += 2)
            {
                BulkOperation action;
                try
                {
                    action = this.Deserialize<BulkOperation>(totalBulks[i]);
                }
                catch (Exception e)
                {
                    throw new Exception($"Can not deserialize into BulkOperation \r\n:{totalBulks[i]}", e);
                }
                action.IndexAction.Should().NotBeNull();
                action.IndexAction.Index.Should().NotBeNullOrEmpty().And.StartWith("logstash-");
                action.IndexAction.Type.Should().NotBeNullOrEmpty().And.Be("_doc");

                SerilogElasticsearchEvent actionMetaData;
                try
                {
                    actionMetaData = this.Deserialize<SerilogElasticsearchEvent>(totalBulks[i + 1]);
                }
                catch (Exception e)
                {
                    throw new Exception(
                        $"Can not deserialize into SerilogElasticsearchMessage \r\n:{totalBulks[i + 1]}", e);
                }
                actionMetaData.Should().NotBeNull();
                bulkActions.Add(actionMetaData);
            }
            return bulkActions;
        }

        protected T Deserialize<T>(string json)
        {
            return this._serializer.Deserialize<T>(new MemoryStream(Encoding.UTF8.GetBytes(json)));
        }

        protected async Task ThrowAsync()
        {
            await Task.Delay(1);
            throw new Exception("boom!");
        }

        protected string[] AssertSeenHttpPosts(List<string> _seenHttpPosts, int lastN)
        {
            _seenHttpPosts.Should().NotBeEmpty().And.HaveCount(2);
            var json = string.Join("", _seenHttpPosts);
            var bulkJsonPieces = json.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            bulkJsonPieces.Count().Should().BeGreaterOrEqualTo(lastN);
            var skip = Math.Max(0, bulkJsonPieces.Count() - lastN);

            return bulkJsonPieces.Skip(skip).Take(lastN).ToArray();
        }


        public class ConnectionStub : InMemoryConnection
        {
            private Func<int> _templateExistReturnCode;
            private List<int> _seenHttpHeads;
            private List<string> _seenHttpPosts;
            private List<Tuple<Uri, string>> _seenHttpPuts;

            public ConnectionStub(
                List<string> _seenHttpPosts,
                List<int> _seenHttpHeads,
                List<Tuple<Uri, string>> _seenHttpPuts,
                Func<int> templateExistReturnCode
                )
            {
                this._seenHttpPosts = _seenHttpPosts;
                this._seenHttpHeads = _seenHttpHeads;
                this._seenHttpPuts = _seenHttpPuts;
                this._templateExistReturnCode = templateExistReturnCode;
            }

            public override TReturn Request<TReturn>(RequestData requestData)
            {
                var ms = new MemoryStream();
                if (requestData.PostData != null)
                    requestData.PostData.Write(ms, new ConnectionConfiguration());

                switch (requestData.Method)
                {
                    case HttpMethod.PUT:
                        _seenHttpPuts.Add(Tuple.Create(requestData.Uri, Encoding.UTF8.GetString(ms.ToArray())));
                        break;
                    case HttpMethod.POST:
                        _seenHttpPosts.Add(Encoding.UTF8.GetString(ms.ToArray()));
                        break;
                    case HttpMethod.HEAD:
                        _seenHttpHeads.Add(this._templateExistReturnCode());
                        break;
                }

                var responseStream = new MemoryStream();
                return ResponseBuilder.ToResponse<TReturn>(requestData, null, this._templateExistReturnCode(), Enumerable.Empty<string>(), responseStream);
            }

            public override async Task<TResponse> RequestAsync<TResponse>(RequestData requestData, CancellationToken cancellationToken)
            {
                var ms = new MemoryStream();
                if (requestData.PostData != null)
                    await requestData.PostData.WriteAsync(ms, new ConnectionConfiguration(), cancellationToken);

                switch (requestData.Method)
                {
                    case HttpMethod.PUT:
                        _seenHttpPuts.Add(Tuple.Create(requestData.Uri, Encoding.UTF8.GetString(ms.ToArray())));
                        break;
                    case HttpMethod.POST:
                        _seenHttpPosts.Add(Encoding.UTF8.GetString(ms.ToArray()));
                        break;
                    case HttpMethod.HEAD:
                        _seenHttpHeads.Add(this._templateExistReturnCode());
                        break;
                }

                var responseStream = new MemoryStream();
                return await ResponseBuilder.ToResponseAsync<TResponse>(requestData, null, this._templateExistReturnCode(), Enumerable.Empty<string>(), responseStream, null, cancellationToken);
            }
        }
    }
}