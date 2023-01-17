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
        protected List<Tuple<Uri, int>> _seenHttpGets = new List<Tuple<Uri, int>>();
        protected List<Tuple<Uri, string>> _seenHttpPuts = new List<Tuple<Uri, string>>();
        private IElasticsearchSerializer _serializer;

        protected int _templateExistsReturnCode = 404;

        protected ElasticsearchSinkTestsBase()
        {
            _seenHttpPosts = new List<string>();
            _seenHttpHeads = new List<int>();
            _seenHttpGets = new List<Tuple<Uri,int>>();
            _seenHttpPuts = new List<Tuple<Uri, string>>();

            var connectionPool = new SingleNodeConnectionPool(new Uri("http://localhost:9200"));
            _connection = new ConnectionStub(_seenHttpPosts, _seenHttpHeads, _seenHttpPuts, _seenHttpGets, () => _templateExistsReturnCode);
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
                action.IndexAction.Type.Should().BeNull();

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

        protected string[] AssertSeenHttpPosts(List<string> _seenHttpPosts, int lastN, int expectedNumberOfRequests = 2)
        {
            _seenHttpPosts.Should().NotBeEmpty().And.HaveCount(expectedNumberOfRequests);
            var json = string.Join("", _seenHttpPosts);
            var bulkJsonPieces = json.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            bulkJsonPieces.Count().Should().BeGreaterOrEqualTo(lastN);
            var skip = Math.Max(0, bulkJsonPieces.Count() - lastN);

            return bulkJsonPieces.Skip(skip).Take(lastN).ToArray();
        }


        public class ConnectionStub : InMemoryConnection
        {
            private Func<int> _templateExistReturnCode;
            private readonly string _productVersion;
            private List<int> _seenHttpHeads;
            private List<Tuple<Uri, int>> _seenHttpGets;
            private List<string> _seenHttpPosts;
            private List<Tuple<Uri, string>> _seenHttpPuts;

            public ConnectionStub(
                List<string> _seenHttpPosts,
                List<int> _seenHttpHeads,
                List<Tuple<Uri, string>> _seenHttpPuts,
                List<Tuple<Uri, int>> _seenHttpGets,
                Func<int> templateExistReturnCode,
                string productVersion = "8.6.0"
                ) : base()
            {
                this._seenHttpPosts = _seenHttpPosts;
                this._seenHttpHeads = _seenHttpHeads;
                this._seenHttpPuts = _seenHttpPuts;
                this._seenHttpGets = _seenHttpGets;
                this._templateExistReturnCode = templateExistReturnCode;
                this._productVersion = productVersion;
            }

            public override TReturn Request<TReturn>(RequestData requestData)
            {
                var ms = new MemoryStream();
                if (requestData.PostData != null)
                    requestData.PostData.Write(ms, new ConnectionConfiguration());

                var responseStream = new MemoryStream();
                int responseStatusCode = 200;
                switch (requestData.Method)
                {
                    case HttpMethod.PUT:
                        _seenHttpPuts.Add(Tuple.Create(requestData.Uri, Encoding.UTF8.GetString(ms.ToArray())));
                        break;
                    case HttpMethod.POST:
                        _seenHttpPosts.Add(Encoding.UTF8.GetString(ms.ToArray()));
                        break;
                    case HttpMethod.GET:
                        switch (requestData.Uri.PathAndQuery.ToLower())
                        {
                            case "/":
                                // handle pre-flight call to Elasticsearch, added in Elasticsearch.NET 7.16 version
                                return ReturnConnectionStatus<TReturn>(requestData); 
                            case "/_cat/nodes":
                                responseStream.Write(Encoding.UTF8.GetBytes(_productVersion));
                                responseStream.Position = 0;
                                responseStatusCode = 200;
                                break;
                        }
                        _seenHttpGets.Add(Tuple.Create(requestData.Uri, responseStatusCode));
                        break;
                    case HttpMethod.HEAD:
                        responseStatusCode = _templateExistReturnCode();
                        _seenHttpHeads.Add(responseStatusCode);
                        break;
                }

                return ResponseBuilder.ToResponse<TReturn>(requestData, null, responseStatusCode, Enumerable.Empty<string>(), responseStream, "text/plain");
            }

            public override Task<TResponse> RequestAsync<TResponse>(RequestData requestData, CancellationToken cancellationToken)
            {
                return Task.FromResult(this.Request<TResponse>(requestData));
            }
        }
    }
}