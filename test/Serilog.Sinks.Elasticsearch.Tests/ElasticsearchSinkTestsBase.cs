using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elasticsearch.Net;
using FluentAssertions;
using Moq;
using Serilog.Sinks.Elasticsearch.Tests.Domain;
using Serilog.Sinks.Elasticsearch.Tests.Serializer;

namespace Serilog.Sinks.Elasticsearch.Tests
{
    public abstract class ElasticsearchSinkTestsBase
    {
        static readonly TimeSpan TinyWait = TimeSpan.FromMilliseconds(50);
        protected readonly IConnection _connection;
        protected readonly ElasticsearchSinkOptions _options;
        protected readonly List<string> _seenHttpPosts = new List<string>();
        protected readonly List<int> _seenHttpHeads = new List<int>();
        protected readonly List<Tuple<Uri, string>> _seenHttpPuts = new List<Tuple<Uri, string>>();
        private ElasticsearchJsonNetSerializer _serializer;

        protected int _templateExistsReturnCode = 404;

        protected ElasticsearchSinkTestsBase()
        {
            var connectionMock = new Mock<IConnection>();

            connectionMock.Setup(c => c.Request<VoidResponse>(It.IsAny<RequestData>())).Returns(
                (RequestData requestData) =>
                {
                    if (requestData.Method == HttpMethod.HEAD)
                    {
                        _seenHttpHeads.Add(_templateExistsReturnCode);

                        var response = new ResponseBuilder<VoidResponse>(requestData)
                        {
                            StatusCode = _templateExistsReturnCode,
                            Stream = null
                        };

                        return response.ToResponse();
                    }

                    if (requestData.Method == HttpMethod.PUT)
                    {
                        var fixedRespone = new MemoryStream();
                        requestData.PostData.Write(fixedRespone, requestData.ConnectionSettings);

                        _seenHttpPuts.Add(Tuple.Create(requestData.Uri, Encoding.UTF8.GetString(fixedRespone.ToArray())));

                        var response = new ResponseBuilder<VoidResponse>(requestData)
                        {
                            StatusCode = 200,
                            Stream = fixedRespone
                        };

                        return response.ToResponse();
                    }

                    return null;
                }
            );

            connectionMock.Setup(c => c.Request<DynamicResponse>(It.IsAny<RequestData>())).Returns(
                (RequestData requestData) =>
                {
                    if (requestData.Method == HttpMethod.POST)
                    {
                        var fixedRespone = new MemoryStream();
                        requestData.PostData.Write(fixedRespone, requestData.ConnectionSettings);

                        _seenHttpPosts.Add(Encoding.UTF8.GetString(requestData.PostData.WrittenBytes));

                        var response = new ResponseBuilder<DynamicResponse>(requestData)
                        {
                            StatusCode = 200,
                            Stream = fixedRespone
                        };

                        return response.ToResponse();
                    }

                    return null;
                });

            Debugging.SelfLog.Out = Console.Out;
            _serializer = new ElasticsearchJsonNetSerializer();
            _connection = connectionMock.Object;
            _options = new ElasticsearchSinkOptions(new Uri("http://localhost:9200"))
            {
                BatchPostingLimit = 2,
                Period = TinyWait,
                Connection = _connection
            };
        }

        /// <summary>
        /// Returns the posted serilog messages and validates the entire bulk in the process
        /// </summary>
        /// <param name="expectedCount"></param>
        /// <returns></returns>
        protected IList<SerilogElasticsearchEvent> GetPostedLogEvents(int expectedCount)
        {
            _seenHttpPosts.Should().NotBeNullOrEmpty();
            var totalBulks = _seenHttpPosts.SelectMany(p => p.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries)).ToList();
            totalBulks.Should().NotBeNullOrEmpty().And.HaveCount(expectedCount * 2);

            var bulkActions = new List<SerilogElasticsearchEvent>();
            for (var i = 0; i < totalBulks.Count; i += 2)
            {
                BulkOperation action;
                try
                {
                    action = Deserialize<BulkOperation>(totalBulks[i]);
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
                    actionMetaData = Deserialize<SerilogElasticsearchEvent>(totalBulks[i + 1]);
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
            return _serializer.Deserialize<T>(new MemoryStream(Encoding.UTF8.GetBytes(json)));
        }
    }
}