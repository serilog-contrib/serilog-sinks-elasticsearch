using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Serilog.Events;

namespace Serilog.Sinks.Elasticsearch.Tests.Domain
{
    /// <summary>
    /// Elasticsearch _bulk follows a specific pattern:
    /// {operation}\n
    /// {operationmetadata}\n
    /// This provides a marker interface for both
    /// </summary>
    interface IBulkData { }

    public class BulkOperation : IBulkData
    {
        [JsonProperty("index")]
        public IndexAction IndexAction { get; set; }
    }

    public class IndexAction
    {
        [JsonProperty("_index")]
        public string Index { get; set; }
        [JsonProperty("_type")]
        public string Type { get; set; }
    }

    public class SerilogElasticsearchEvent : IBulkData
    {
        [JsonProperty("@timestamp")]
        public DateTime Timestamp { get; set; }
        
        [JsonProperty("level")]
        [JsonConverter(typeof(StringEnumConverter))]
        public LogEventLevel Level { get; set; }
        
        [JsonProperty("messageTemplate")]
        public string MessageTemplate { get; set; }
        
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("exceptions")]
        public List<SerilogElasticsearchExceptionInfo> Exceptions { get; set; }
    }

    public class SerilogElasticsearchExceptionInfo
    {
        public int Depth { get; set; }
        public string ClassName { get; set; }
        public string Message { get; set; }
        public string Source { get; set; }
        public string StackTraceString { get; set; }
        public string RemoteStackTraceString { get; set; }
        public int RemoteStackIndex { get; set; }
        public SerilogExceptionMethodInfo ExceptionMethod { get; set; }
        public int HResult { get; set; }
        public string HelpUrl { get; set; }
        
        //writing byte[] will fall back to serializer and they differ in output 
        //JsonNET assumes string, simplejson writes array of numerics.
        //Skip for now
            
        //public byte[] WatsonBuckets { get; set; }
    }

    public class SerilogExceptionMethodInfo
    {
        public string Name { get; set; }
        public string AssemblyName { get; set; }
        public string AssemblyVersion { get; set; }
        public string AssemblyCulture { get; set; }
        public string ClassName { get; set; }
        public string Signature { get; set; }
        public int MemberType { get; set; }
    }
}
