using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elasticsearch.Net;
using FluentAssertions;
using Nest;
using Serilog.Sinks.Elasticsearch.Tests.Domain;
using Nest.JsonNetSerializer;
using System.Threading;
using Newtonsoft.Json.Linq;

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

        protected void JsonEquals(string json, string embeddedResourceNameEndsWith)
        {
#if DOTNETCORE
            var assembly = GetType().Assembly;
#else
            var assembly = Assembly.GetExecutingAssembly();
#endif
            var expected = TestDataHelper.ReadEmbeddedResource(assembly, embeddedResourceNameEndsWith);

            var nJson = JObject.Parse(json);
            var nOtherJson = JObject.Parse(expected);
            var equals = JToken.DeepEquals(nJson, nOtherJson);
            if (equals) return;
            expected.Should().BeEquivalentTo(json);
        }


        public class ConnectionStub : InMemoryConnection
        {
            private readonly Func<int> _templateExistReturnCode;
            private readonly List<int> _seenHttpHeads;
            private readonly List<Tuple<Uri, int>> _seenHttpGets;
            private readonly List<string> _seenHttpPosts;
            private readonly List<Tuple<Uri, string>> _seenHttpPuts;

            private readonly string _productVersion;

            /// <summary>
            /// Elasticsearch.NET client version 7.16 or higher
            /// uses pre-flight request, before any other request is served,
            /// to check product (Elasticsearch) and version of the product.
            /// It can be seen on <see cref="IConnectionPool.ProductCheckStatus"/> property.
            /// </summary>
            private bool _productCheckDone;

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
                if (_productCheckDone == false)
                {
                    if (requestData.Method != HttpMethod.GET || requestData.PathAndQuery != string.Empty)
                        throw new InvalidOperationException(
                            $"{nameof(ConnectionStub)} expects first request" +
                            $" to be productCheck pre-flight request");

                    _productCheckDone = true;
                    return ReturnConnectionStatus<TReturn>(requestData); // root page returned
                }

                byte[] responseBytes = Array.Empty<byte>();
                if (requestData.PostData != null)
                {
                    using var ms = new MemoryStream();
                    requestData.PostData.Write(ms, new ConnectionConfiguration());
                    responseBytes = ms.ToArray();
                }

                int responseStatusCode = 200;
                string contentType = null;

                switch (requestData.Method)
                {
                    case HttpMethod.PUT:
                        _seenHttpPuts.Add(Tuple.Create(requestData.Uri, Encoding.UTF8.GetString(responseBytes)));
                        break;
                    case HttpMethod.POST:
                        _seenHttpPosts.Add(Encoding.UTF8.GetString(responseBytes));
                        break;
                    case HttpMethod.GET:
                        switch (requestData.Uri.PathAndQuery.ToLower())
                        {
                            case "/":
                                // ReturnConnectionStatus(...) call at the bottom will return dummy product page
                                // when root "/" is requested.
                                break;
                            case "/_cat/nodes":
                            case "/_cat/nodes?h=v":
                                responseBytes = Encoding.UTF8.GetBytes(_productVersion);
                                responseStatusCode = 200;
                                contentType = "text/plain; charset=UTF-8";
                                break;
                        }
                        _seenHttpGets.Add(Tuple.Create(requestData.Uri, responseStatusCode));
                        break;
                    case HttpMethod.HEAD:
                        if (requestData.Uri.PathAndQuery.ToLower().StartsWith("/_template/"))
                        {
                            responseStatusCode = _templateExistReturnCode();
                        }
                        _seenHttpHeads.Add(responseStatusCode);
                        break;
                }

                return ReturnConnectionStatus<TReturn>(requestData, responseBytes, responseStatusCode, contentType);
            }

            public override Task<TResponse> RequestAsync<TResponse>(RequestData requestData, CancellationToken cancellationToken)
            {
                return Task.FromResult(this.Request<TResponse>(requestData));
            }
        }
    }
}