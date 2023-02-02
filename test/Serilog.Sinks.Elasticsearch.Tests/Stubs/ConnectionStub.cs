using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Elasticsearch.Net;
using System.Threading;

namespace Serilog.Sinks.Elasticsearch.Tests.Stubs
{
    internal class ConnectionStub : InMemoryConnection
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
            _templateExistReturnCode = templateExistReturnCode;
            _productVersion = productVersion;
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
                return ReturnConnectionStatus<TReturn>(requestData); // hard-coded root page returned
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
            InMemoryHttpResponse productCheckResponse = null;

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
                            productCheckResponse = ModifiedProductCheckResponse(_productVersion);
                            break;
                        case "/_cat/nodes?h=v":
                            responseBytes = Encoding.UTF8.GetBytes(_productVersion);
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

            return ReturnConnectionStatus<TReturn>(requestData, productCheckResponse, responseBytes, responseStatusCode, contentType);
        }

        public override Task<TResponse> RequestAsync<TResponse>(RequestData requestData, CancellationToken cancellationToken)
        {
            return Task.FromResult(Request<TResponse>(requestData));
        }

        public static InMemoryHttpResponse ModifiedProductCheckResponse(string productVersion)
        {
            var productCheckResponse = ValidProductCheckResponse();
            if (productVersion is not null)
            {
                using var originalMemoryStream = new MemoryStream(productCheckResponse.ResponseBytes, false);
                {
                    var json = LowLevelRequestResponseSerializer.Instance.Deserialize<dynamic>(originalMemoryStream);
                    json["version"]["number"] = productVersion;
                    using var modifiedMemoryStream = new MemoryStream();
                    LowLevelRequestResponseSerializer.Instance.Serialize(json, modifiedMemoryStream);
                    productCheckResponse.ResponseBytes = modifiedMemoryStream.ToArray();
                }
            }
            return productCheckResponse;
        }
    }
}