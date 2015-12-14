using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Serilog.Sinks.Elasticsearch.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            Serilog.Debugging.SelfLog.Out = Console.Out;

            Log.Logger = new LoggerConfiguration()
               .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://localhost:9200"))
               {
                   AutoRegisterTemplate = true
               })
               .MinimumLevel.Debug()
               .CreateLogger();

            Log.Debug("Debug");
            Log.Information("Info");
            Log.Warning("Warning");
            Log.Error("Error");

            Console.ReadLine();
        }
    }
}
