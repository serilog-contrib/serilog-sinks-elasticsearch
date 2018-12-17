using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elasticsearch.Net;
using Serilog.Debugging;

namespace Serilog.Sinks.Elasticsearch.Durable
{
    /// <summary>
    /// 
    /// </summary>
    public class ElasticSearchLogClient : ILogClient<List<string>>
    {
        private readonly IElasticLowLevelClient _elasticLowLevelClient;
        private readonly Action<string, long?, string> _badPayloadAction;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="elasticLowLevelClient"></param>
        /// <param name="badPayloadAction"></param>
        public ElasticSearchLogClient(IElasticLowLevelClient elasticLowLevelClient,
            Action<string, long?, string> badPayloadAction)
        {
            _elasticLowLevelClient = elasticLowLevelClient;
            _badPayloadAction = badPayloadAction;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        public async Task<SentPayloadResult> SendPayloadAsync(List<string> payload)
        {            
            try
            {
                if (payload == null || !payload.Any()) return new SentPayloadResult(null, true);
                var response = await _elasticLowLevelClient.BulkAsync<DynamicResponse>(PostData.MultiJson(payload));

                if (response.Success)
                {
                    var invalidPayload = GetInvalidPayloadAsync(response, payload);
                    return new SentPayloadResult(response, true, invalidPayload);
                }
                else
                {
                    SelfLog.WriteLine("Received failed ElasticSearch shipping result {0}: {1}", response.HttpStatusCode,
                        response.OriginalException);
                    return new SentPayloadResult(response, false,
                        new InvalidResult()
                        {
                            StatusCode = response.HttpStatusCode ?? 500,
                            Content = response.OriginalException.ToString()
                        });
                }
            }
            catch (Exception ex)
            {
                SelfLog.WriteLine("Exception while emitting periodic batch from {0}: {1}", this, ex);
                return new SentPayloadResult(null, false, null, ex);
            }


        }

        private InvalidResult GetInvalidPayloadAsync(DynamicResponse baseResult, List<string> payload)
        {
            int i = 0;

            var items = baseResult.Body["items"];
            if (items == null) return null;
            List<string> badPayload = new List<string>();
            bool hasErrors = false;
            foreach (dynamic item in items)
            {
                long? status = item.index?.status;
                i++;
                if (!status.HasValue || status < 300)
                {
                    continue;
                }

                hasErrors = true;
                var id = item.index?._id;
                var error = item.index?.error;
                if (int.TryParse(id.Split('_')[0], out int index))
                {
                    SelfLog.WriteLine("Received failed ElasticSearch shipping result {0}: {1}. Failed payload : {2}.",  status,  error?.ToString(), payload.ElementAt(index * 2 + 1));
                    badPayload.Add(payload.ElementAt(index * 2 + 1));
                    _badPayloadAction?.Invoke(payload.ElementAt(index * 2 + 1), status, error?.ToString());
                }
                else
                {
                    SelfLog.WriteLine($"Received failed ElasticSearch shipping result {status}: {error?.ToString()}.");
                }
            }

            if (!hasErrors)
                return null;
            return new InvalidResult()
            {
                StatusCode = baseResult.HttpStatusCode ?? 500,
                Content = baseResult.ToString(),
                BadPayLoad = String.Join(Environment.NewLine, badPayload)
            };
        }
    }
}
