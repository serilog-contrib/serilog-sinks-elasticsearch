using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Serilog.Sinks.Elasticsearch.Durable
{


    public class SentPayloadResult
    {
        
        public dynamic BaseResult { get; }
        public bool Success { get; }
        public InvalidResult InvalidResult { get; }


        public Exception Exception { get; }

        public SentPayloadResult(dynamic baseResult, bool success, InvalidResult invalidResult =null,  Exception exception=null)
        {            
            BaseResult = baseResult;
            Success = success;
            InvalidResult = invalidResult;
            Exception = exception;
        }

        
    }

    public class InvalidResult
    {
        public int StatusCode { get; set; }
        public string ResultContent { get; set; }
    }

    public interface  ILogClient
    {
        Task<SentPayloadResult> SendPayloadAsync(List<string> payload);        
    }
}
