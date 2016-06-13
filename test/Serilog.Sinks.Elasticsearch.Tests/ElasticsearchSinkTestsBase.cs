using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Elasticsearch.Net;
using FakeItEasy;
using FluentAssertions;
using Nest;
using NUnit.Framework;
using Serilog.Sinks.Elasticsearch.Tests.Domain;

namespace Serilog.Sinks.Elasticsearch.Tests
{
    public abstract class ElasticsearchSinkTestsBase
    {
        protected static readonly TimeSpan TinyWait = TimeSpan.FromMilliseconds(50);
        protected readonly IConnection _connection;
        protected readonly ElasticsearchSinkOptions _options;
        protected List<string> _seenHttpPosts = new List<string>();
        protected List<int> _seenHttpHeads = new List<int>();
        protected List<Tuple<Uri, string>> _seenHttpPuts = new List<Tuple<Uri, string>>();
        private JsonNetSerializer _serializer;

        protected int _templateExistsReturnCode = 404;

        [SetUp]
        public void BeforeEach()
        {
            _seenHttpPosts = new List<string>();
            _seenHttpHeads = new List<int>();
            _seenHttpPuts = new List<Tuple<Uri, string>>();

        }
        protected ElasticsearchSinkTestsBase()
        {
            Serilog.Debugging.SelfLog.Out = Console.Out;
            _serializer = new JsonNetSerializer(new ConnectionSettings());
            _connection = A.Fake<IConnection>();
            IConnectionPool connectionPool = new SingleNodeConnectionPool(new Uri("http://localhost:9200"));
            _options = new ElasticsearchSinkOptions(connectionPool)
            {
                BatchPostingLimit = 2,
                Period = TinyWait,
                Connection = _connection
            };

            A.CallTo(() => _connection.Request<DynamicResponse>(A<RequestData>._))
                .ReturnsLazily((RequestData requestData) =>
                {
                    MemoryStream ms = new MemoryStream();
                    if(requestData.PostData != null)
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
                            _seenHttpHeads.Add(_templateExistsReturnCode);
                            break;
                    }
                    return new ElasticsearchResponse<DynamicResponse>(_templateExistsReturnCode, new[] { 200, 404 });
                });
        }

        protected ElasticsearchSinkTestsBase(ElasticsearchSinkOptions option)
        {
            Serilog.Debugging.SelfLog.Out = Console.Out;
            _serializer = new JsonNetSerializer(new ConnectionSettings());
            _connection = option.Connection;
            _options = option;

            A.CallTo(() => _connection.Request<DynamicResponse>(A<RequestData>._))
                .ReturnsLazily((RequestData requestData) =>
                {
                    MemoryStream ms = new MemoryStream();
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
                            _seenHttpHeads.Add(_templateExistsReturnCode);
                            break;
                    }
                    return new ElasticsearchResponse<DynamicResponse>(_templateExistsReturnCode, new[] { 200, 404 });
                });
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
                    throw new Exception(string.Format("Can not deserialize into BulkOperation \r\n:{0}", totalBulks[i]), e);
                }
                action.IndexAction.Should().NotBeNull();
                action.IndexAction.Index.Should().NotBeNullOrEmpty().And.StartWith("logstash-");
                action.IndexAction.Type.Should().NotBeNullOrEmpty().And.Be("logevent");

                SerilogElasticsearchEvent actionMetaData;
                try
                {
                    actionMetaData = this.Deserialize<SerilogElasticsearchEvent>(totalBulks[i + 1]);
                }
                catch (Exception e)
                {
                    throw new Exception(string.Format("Can not deserialize into SerilogElasticsearchMessage \r\n:{0}", totalBulks[i + 1]), e);
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
    }
}