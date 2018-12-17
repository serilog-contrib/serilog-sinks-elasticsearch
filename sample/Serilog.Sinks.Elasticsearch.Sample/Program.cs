using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Debugging;
using Serilog.Formatting.Json;
using Serilog.Sinks.File;
using Serilog.Sinks.SystemConsole.Themes;

namespace Serilog.Sinks.Elasticsearch.Sample
{
    class Program
    {
        private static IConfiguration Configuration { get; } = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", true, true)
            .AddEnvironmentVariables()
            .Build();
        static void Main(string[] args)
        {
            // Enable the selflog output
            SelfLog.Enable(Console.Error);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(theme: SystemConsoleTheme.Literate)
                //not persistant
                //.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(Configuration.GetConnectionString("elasticsearch"))) // for the docker-compose implementation
                //{
                //    AutoRegisterTemplate = true,
                //    //BufferBaseFilename = "./buffer",
                //    RegisterTemplateFailure = RegisterTemplateRecovery.IndexAnyway,
                //    FailureCallback = e => Console.WriteLine("Unable to submit event " + e.MessageTemplate),
                //    EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog |
                //                       EmitEventFailureHandling.WriteToFailureSink |
                //                       EmitEventFailureHandling.RaiseCallback,
                //    FailureSink = new FileSink("./fail-{Date}.txt", new JsonFormatter(), null, null)
                //})
                ////presistant
                .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(Configuration.GetConnectionString("elasticsearch"))) 
                {
                    AutoRegisterTemplate = true,
                    BufferBaseFilename = "./buffer/logserilog",
                    IndexFormat = "log-serilog-{0:yyyy.MM}",
                    RegisterTemplateFailure = RegisterTemplateRecovery.FailSink,
                    FailureCallback = e => Console.WriteLine("Unable to submit event " + e.MessageTemplate),
                    EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog |
                                       EmitEventFailureHandling.WriteToFailureSink |
                                       EmitEventFailureHandling.RaiseCallback
                })
                .CreateLogger();

           

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

            // Introduce a failure by storing a field as a different type
            Log.Debug("Reusing {A} by {B}", "string", true);

            Log.CloseAndFlush();
            Console.Read();
        }
    }
}
