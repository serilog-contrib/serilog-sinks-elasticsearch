using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elasticsearch.Net;
using Serilog.Debugging;

namespace Serilog.Sinks.Elasticsearch.Durable
{
    public class ElasticLogClient: ILogClient
    {
        private readonly IElasticLowLevelClient _elasticLowLevelClient;

        public ElasticLogClient(IElasticLowLevelClient elasticLowLevelClient)
        {
            _elasticLowLevelClient = elasticLowLevelClient;
        }
        public async Task<SentPayloadResult> SendPayloadAsync(List<string> payload)
        {
            try
            {
                var response = await _elasticLowLevelClient.BulkAsync<DynamicResponse>(PostData.MultiJson(payload));
                
                if (response.Success)
                {
                    var invalidPayload=GetInvalidPayloadAsync(response, payload);
                    return new SentPayloadResult(response,true, invalidPayload);
                }
                else
                {
                    SelfLog.WriteLine("Received failed ElasticSearch shipping result {0}: {1}", response.HttpStatusCode, response.OriginalException);
                    return new SentPayloadResult(response, false, new InvalidResult() { StatusCode = response.HttpStatusCode??500 , ResultContent = response.OriginalException .ToString()});
                }
            }
            catch (Exception ex)
            {
                SelfLog.WriteLine("Exception while emitting periodic batch from {0}: {1}", this, ex);
                return new SentPayloadResult(null, false,null, ex);
            }
            
            
        }

        private InvalidResult GetInvalidPayloadAsync(DynamicResponse baseResult, List<string> payload)
        {
            int i = 0;
            
            var items = baseResult.Body["items"];
            if (items == null) return null;
            StringBuilder badContent=new StringBuilder();
            foreach (dynamic item in items)
            {
                long? status = item.index?.status;
                i++;
                if (!status.HasValue || status < 300)
                {
                    continue;
                }

                var id = item.index?._id;
                var error = item.index?.error;
                if (int.TryParse(id.Split('_')[0], out int index))
                {
                    SelfLog.WriteLine($"Received failed ElasticSearch shipping result {status}: {error?.ToString()}. Failed payload : {payload.ElementAt(index * 2 + 1)}.");
                    badContent.AppendLine(
                        $"Received failed ElasticSearch shipping result {status}: {error?.ToString()}. Failed payload : {payload.ElementAt(index * 2 + 1)}.");
                }
                else
                {
                    SelfLog.WriteLine($"Received failed ElasticSearch shipping result {status}: {error?.ToString()}.");
                    badContent.AppendLine(
                        $"Received failed ElasticSearch shipping result {status}: {error?.ToString()}.");
                }

                
            }

            if (badContent.Length == 0)
              return null;
            return new InvalidResult()
            {
                StatusCode = baseResult.HttpStatusCode ?? 500,
                ResultContent = baseResult.ToString()
            };
        }
    }
}
