using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
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

            // Create Serilog Elasticsearch logger
            Log.Logger = new LoggerConfiguration()
               //.Enrich.With<MachineNameEnricher>()
               .Enrich.WithProperty("Version", "v1")
               .Enrich.WithProperty("Application", "YourAppName")
               .Enrich.WithProperty("HostName", Environment.MachineName)
               .Enrich
               .FromLogContext()
               .MinimumLevel.Warning()
               .MinimumLevel.Override("Serilog", LogEventLevel.Information)
               .MinimumLevel.Override("JorJika", LogEventLevel.Information)
               .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://127.0.0.1:9200"))
               {
                   //MinimumLogEventLevel = LogEventLevel.Warning,
                   AutoRegisterTemplate = true,
                   AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
                   OverwriteTemplate = true,
                   NumberOfReplicas = 0,
                   NumberOfShards = 1,
                   IndexAliases = new string[] { "yourapp" },
                   IndexFormat = "log-yourapp-{0:yyyy.MM}"
               })
               .CreateLogger();

            // Enable the selflog output
            SelfLog.Enable(Console.Error);



            Log.Logger.Information("Hi");
            Log.Information("Hello, world!");
            Console.ReadLine();

            //Log.Logger = new LoggerConfiguration()
            //    .MinimumLevel.Debug()
            //    .WriteTo.Console(theme: SystemConsoleTheme.Literate)
            //                    //not persistant
            //                    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(Configuration.GetConnectionString("elasticsearch"))) // for the docker-compose implementation
            //                    {
            //                        AutoRegisterTemplate = true,
            //                        //BufferBaseFilename = "./buffer",
            //                        RegisterTemplateFailure = RegisterTemplateRecovery.IndexAnyway,
            //                        FailureCallback = e => Console.WriteLine("Unable to submit event " + e.MessageTemplate),
            //                        EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog |
            //                                           EmitEventFailureHandling.WriteToFailureSink |
            //                                           EmitEventFailureHandling.RaiseCallback,
            //                        FailureSink = new FileSink("./fail-{Date}.txt", new JsonFormatter(), null, null)
            //                    })
            //        .CreateLogger();

            //SetupLoggerWithSimplePersistantStorage();
            //LoggingLevelSwitch levelSwitch = SetupLoggerWithPersistantStorage();

            //Log.Debug("To high loglevel default is Information this will not be logged");
            //levelSwitch.MinimumLevel = LogEventLevel.Debug;
            Log.Information("Hello, world!");
            //Log.Information("To  big log row bigger than SingleEventSizePostingLimit ! {a}", new string('*', 5000));

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
            Console.WriteLine("Press any key to continue...");
            while (!Console.KeyAvailable)
            {
                Thread.Sleep(500);
            }
        }

        private static LoggingLevelSwitch SetupLoggerWithPersistantStorage()
        {
            //persistant storage with all settings available for this mode
            //please note that all limit settings here is set verry low for test, default values should usually work best!
            var levelSwitch = new LoggingLevelSwitch();
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(Configuration.GetConnectionString("elasticsearch")))
                {
                    AutoRegisterTemplate = true,
                    BufferBaseFilename = "./buffer/logserilog",
                    IndexFormat = "log-serilog-{0:yyyy.MM}",
                    RegisterTemplateFailure = RegisterTemplateRecovery.FailSink,
                    BufferCleanPayload = (failingEvent, statuscode, exception) =>
                    {
                        dynamic e = JObject.Parse(failingEvent);
                        return JsonConvert.SerializeObject(new Dictionary<string, object>()
                        {
                            { "@timestamp",e["@timestamp"]},
                            { "level",e.level},
                            { "message","Error: "+e.message},
                            { "messageTemplate",e.messageTemplate},
                            { "failingStatusCode", statuscode},
                            { "failingException", exception}
                        });
                    },
                    OverwriteTemplate = true,
                    NumberOfShards = 1,
                    NumberOfReplicas = 1,
                    GetTemplateContent = null,
                    AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv6,
                    PipelineName = null,
                    TypeName = "logevent",
                    BufferIndexDecider = (logEvent, offset) => "log-serilog-" + (new Random().Next(0, 2)),
                    BatchPostingLimit = 50,
                    BufferLogShippingInterval = TimeSpan.FromSeconds(5),
                    SingleEventSizePostingLimit = 1000,
                    LevelSwitch = levelSwitch,
                    BufferRetainedInvalidPayloadsLimitBytes = 2000,
                    BufferFileSizeLimitBytes = 2000,
                    BufferFileCountLimit = 2
                })
                .CreateLogger();
            return levelSwitch;
        }

        private static void SetupLoggerWithSimplePersistantStorage()
        {
            //presistant
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(Configuration.GetConnectionString("elasticsearch")))
                {
                    BufferBaseFilename = "./buffer/logserilogsimple",
                    IndexFormat = "log-serilog-simple-{0:yyyy.MM}"
                })
                .CreateLogger();
        }
    }
}
