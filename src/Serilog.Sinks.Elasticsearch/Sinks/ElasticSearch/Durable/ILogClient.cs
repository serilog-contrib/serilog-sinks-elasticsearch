using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Serilog.Sinks.Elasticsearch.Durable
{
    /// <summary>
    /// A wrapper client which talk to the log server
    /// </summary>
    /// <typeparam name="TPayload"></typeparam>
    public interface ILogClient<TPayload>
    {
        Task<SentPayloadResult> SendPayloadAsync(TPayload payload);
    }
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
        public string Content { get; set; }
        public string BadPayLoad { get; set; }
    }  
}
