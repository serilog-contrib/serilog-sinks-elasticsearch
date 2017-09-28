using System;
using Serilog.Debugging;
using Serilog.Sinks.SystemConsole.Themes;

namespace Serilog.Sinks.Elasticsearch.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(theme: SystemConsoleTheme.Literate)
                .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://elastic:changeme@localhost:9200"))
                {
                 AutoRegisterTemplate  = true
                })
                .CreateLogger();

            SelfLog.Enable(Console.Error);

            Log.Information("Hello, world!");

            int a = 10, b = 0;
            try
            {
                Log.Debug("Dividing {A} by {B}", a, b);
                Console.WriteLine(a / b);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Something went wrong");
            }

            // Introduce a failure by storing something as a different type

            Log.Debug("Reusing {A} by {B}", "string", true);

            Log.CloseAndFlush();
            Console.ReadKey();
        }
    }
}
