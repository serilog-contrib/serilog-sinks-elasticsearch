using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Elasticsearch.Net;
using Elasticsearch.Net.Connection;
using Elasticsearch.Net.Connection.Configuration;
using Elasticsearch.Net.JsonNet;
using FakeItEasy;
using FluentAssertions;
using Serilog.Sinks.Elasticsearch.Tests.Domain;
using Serilog.Sinks.ElasticSearch;

namespace Serilog.Sinks.Elasticsearch.Tests
{
    public abstract class ElasticsearchSinkTestsBase
    {
        static readonly TimeSpan TinyWait = TimeSpan.FromMilliseconds(50);
        protected readonly IConnection _connection;
        protected readonly ElasticsearchSinkOptions _options;
        protected readonly List<string> _seenHttpPosts = new List<string>();
        protected readonly List<Tuple<Uri, string>> _seenHttpPuts = new List<Tuple<Uri, string>>();
        private ElasticsearchJsonNetSerializer _serializer;

        protected ElasticsearchSinkTestsBase()
        {
            Serilog.Debugging.SelfLog.Out = Console.Out;
            _serializer = new ElasticsearchJsonNetSerializer();
            _connection = A.Fake<IConnection>();
            _options = new ElasticsearchSinkOptions(new Uri("http://localhost:9200"))
            {
                BatchPostingLimit  = 2,
                Period = TinyWait,
                Connection = _connection
            };
            A.CallTo(() => _connection.PutSync(A<Uri>._, A<byte[]>._, A<IRequestConfiguration>._))
                .ReturnsLazily((Uri uri, byte[] postData, IRequestConfiguration requestConfiguration) =>
                {
                    var fixedRespone = new MemoryStream(Encoding.UTF8.GetBytes(@"{ ""ok"": true }"));
                    _seenHttpPuts.Add(Tuple.Create(uri, Encoding.UTF8.GetString(postData)));
                    return ElasticsearchResponse<Stream>.Create(new ConnectionConfiguration(), 200, "PUT", "/", postData, fixedRespone);
                });
            A.CallTo(() => _connection.PostSync(A<Uri>._, A<byte[]>._, A<IRequestConfiguration>._))
                .ReturnsLazily((Uri uri, byte[] postData, IRequestConfiguration requestConfiguration) =>
                {
                    var fixedRespone = new MemoryStream(Encoding.UTF8.GetBytes(@"{ ""ok"": true }"));
                    _seenHttpPosts.Add(Encoding.UTF8.GetString(postData));
                    return ElasticsearchResponse<Stream>.Create(new ConnectionConfiguration(), 200, "POST", "/", postData, fixedRespone);
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
            var totalBulks = this._seenHttpPosts.SelectMany(p=>p.Split(new []{"\n"}, StringSplitOptions.RemoveEmptyEntries)).ToList();
            totalBulks.Should().NotBeNullOrEmpty().And.HaveCount(expectedCount*2);

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