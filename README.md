# Serilog.Sinks.Elasticsearch

[![Build status](https://ci.appveyor.com/api/projects/status/bk367tcnx9qt2sjy/branch/master?svg=true)](https://ci.appveyor.com/project/serilog/serilog-sinks-elasticsearch/branch/master)

## What is this sink ?

The Serilog Elasticsearch sink project is a sink (basically a writer) for the Serilog logging framework. Structured log events are written to sinks and each sink is responsible for writing it to its own backend, database, store etc. This sink delivers the data to Elasticsearch, a NoSQL search engine. It does this in a similar structure as Logstash and makes it easy to use Kibana for visualizing your logs.

## Features

- Simple configuration to get log events published to Elasticsearch. Only server address is needed.
- All properties are stored inside fields in ES. This allows you to query on all the relevant data but also run analytics over this data.
- Be able to customize the store; specify the index name being used, the serializer or the connections to the server (load balanced).
- Durable mode; store the logevents first on disk before delivering them to ES making sure you never miss events if you have trouble connecting to your ES cluster.
- Automatically create the right mappings for the best usage of the log events in ES or automatically upload your own custom mapping.
- Starting from version 3, compatible with Elasticsearch 2.
- Version 6.x supports the new Elasticsearch.net version 6.x library.

## Quick start

```powershell
Install-Package serilog.sinks.elasticsearch
```

Register the sink in code or using the appSettings reader (from v2.0.42+) as shown below.

```csharp
var loggerConfig = new LoggerConfiguration()
    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://localhost:9200") ){
             AutoRegisterTemplate = true,
             AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv6
     });
```

This example shows the options that are currently available when using the appSettings reader.

```xml
  <appSettings>
    <add key="serilog:using" value="Serilog.Sinks.Elasticsearch"/>
    <add key="serilog:write-to:Elasticsearch.nodeUris" value="http://localhost:9200;http://remotehost:9200"/>
    <add key="serilog:write-to:Elasticsearch.indexFormat" value="custom-index-{0:yyyy.MM}"/>
    <add key="serilog:write-to:Elasticsearch.templateName" value="myCustomTemplate"/>
    <add key="serilog:write-to:Elasticsearch.typeName" value="myCustomLogEventType"/>
    <add key="serilog:write-to:Elasticsearch.pipelineName" value="myCustomPipelineName"/>
    <add key="serilog:write-to:Elasticsearch.batchPostingLimit" value="50"/>
    <add key="serilog:write-to:Elasticsearch.period" value="2"/>
    <add key="serilog:write-to:Elasticsearch.inlineFields" value="true"/>
    <add key="serilog:write-to:Elasticsearch.minimumLogEventLevel" value="Warning"/>
    <add key="serilog:write-to:Elasticsearch.bufferBaseFilename" value="C:\Temp\SerilogElasticBuffer"/>
    <add key="serilog:write-to:Elasticsearch.bufferFileSizeLimitBytes" value="5242880"/>
    <add key="serilog:write-to:Elasticsearch.bufferLogShippingInterval" value="5000"/>	
    <add key="serilog:write-to:Elasticsearch.connectionGlobalHeaders" value="Authorization=Bearer SOME-TOKEN;OtherHeader=OTHER-HEADER-VALUE" />
    <add key="serilog:write-to:Elasticsearch.connectionTimeout" value="5" />
    <add key="serilog:write-to:Elasticsearch.emitEventFailure" value="WriteToSelfLog" />
    <add key="serilog:write-to:Elasticsearch.queueSizeLimit" value="100000" />
    <add key="serilog:write-to:Elasticsearch.autoRegisterTemplate" value="true" />
    <add key="serilog:write-to:Elasticsearch.autoRegisterTemplateVersion" value="ESv2" />
    <add key="serilog:write-to:Elasticsearch.overwriteTemplate" value="false" />
    <add key="serilog:write-to:Elasticsearch.registerTemplateFailure" value="IndexAnyway" />
    <add key="serilog:write-to:Elasticsearch.deadLetterIndexName" value="deadletter-{0:yyyy.MM}" />
    <add key="serilog:write-to:Elasticsearch.numberOfShards" value="20" />
    <add key="serilog:write-to:Elasticsearch.numberOfReplicas" value="10" />
    <add key="serilog:write-to:Elasticsearch.formatProvider" value="My.Namespace.MyFormatProvider, My.Assembly.Name" />
    <add key="serilog:write-to:Elasticsearch.connection" value="My.Namespace.MyConnection, My.Assembly.Name" />
    <add key="serilog:write-to:Elasticsearch.serializer" value="My.Namespace.MySerializer, My.Assembly.Name" />
    <add key="serilog:write-to:Elasticsearch.connectionPool" value="My.Namespace.MyConnectionPool, My.Assembly.Name" />
    <add key="serilog:write-to:Elasticsearch.customFormatter" value="My.Namespace.MyCustomFormatter, My.Assembly.Name" />
    <add key="serilog:write-to:Elasticsearch.customDurableFormatter" value="My.Namespace.MyCustomDurableFormatter, My.Assembly.Name" />
    <add key="serilog:write-to:Elasticsearch.failureSink" value="My.Namespace.MyFailureSink, My.Assembly.Name" />
  </appSettings>
```

With the appSettings configuration the `nodeUris` property is required. Multiple nodes can be specified using `,` or `;` to seperate them. All other properties are optional. Also required is the '<add key="serilog:using" value="Serilog.Sinks.Elasticsearch"/>' setting to include this sink. All other properties are optional. If you do not explicitly specify an indexFormat-setting, a generic index such as 'logstash-[current_date]' will be used automatically.

And start writing your events using Serilog.

## More information
- [Basic information](https://github.com/serilog/serilog-sinks-elasticsearch/wiki/basic-setup) on how to configure and use this sink.
- [Configuration options](https://github.com/serilog/serilog-sinks-elasticsearch/wiki/Configure-the-sink) which you can use.
- How to use the [durability](https://github.com/serilog/serilog-sinks-elasticsearch/wiki/durability) mode.
- [Accessing](https://github.com/serilog/serilog-sinks-elasticsearch/wiki/access-logs) the logs using Kibana.
- Get the [NuGet package](http://www.nuget.org/packages/Serilog.Sinks.Elasticsearch).
- Report issues to the [issue tracker](https://github.com/serilog/serilog-sinks-elasticsearch/issues). PR welcome, but please do this against the dev branch.
- For an overview of recent changes, have a look at the [change log](https://github.com/serilog/serilog-sinks-elasticsearch/blob/master/CHANGES.md).

### A note about fields inside Elasticsearch

Be aware that there is an explicit and implicit mapping of types inside an Elasticsearch index. A value called `X` as a string will be indexed as being a string. Sending the same `X` as an integer in a next log message will not work. ES will raise a mapping exception, however it is not that evident that your log item was not stored due to the bulk actions performed. 

So be careful about defining and using your fields (and type of fields). It is easy to miss that you first send a {User} as a simple username (string) and next as a User object. The first mapping dynamically created in the index wins. See also issue #184 for details and a possible solution. There are also limits in ES on the number of dynamic fields you can actually throw inside an index.

### A note about Kibana

In order to avoid a potentially deeply nested JSON structure for exceptions with inner exceptions,
by default the logged exception and it's inner exception is logged as an array of exceptions in the field `exceptions`. Use the 'Depth' field to traverse the inner exceptions flow. 

However, not all features in Kibana work just as well with JSON arrays - for instance, including
exception fields on dashboards and visualizations. Therefore, we provide an alternative formatter,  `ExceptionAsObjectJsonFormatter`, which will serialize the exception into the `exception` field as an object with nested `InnerException` properties. This was also the default behaviour of the sink before version 2.

To use it, simply specify it as the `CustomFormatter` when creating the sink:

```csharp
    new ElasticsearchSink(new ElasticsearchSinkOptions(url)
    {
      CustomFormatter = new ExceptionAsObjectJsonFormatter(renderMessage:true)
    });
```

### JSON `appsettings.json` configuration

To use the Elasticsearch sink with _Microsoft.Extensions.Configuration_, for example with ASP.NET Core or .NET Core, use the [Serilog.Settings.Configuration](https://github.com/serilog/serilog-settings-configuration) package. First install that package if you have not already done so:

```powershell
Install-Package Serilog.Settings.Configuration
```

Instead of configuring the sink directly in code, call `ReadFrom.Configuration()`:

```csharp
var configuration = new ConfigurationBuilder()
    .SetBasePath(env.ContentRootPath)
    .AddJsonFile("appsettings.json")
    .Build();

var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();
```

In your `appsettings.json` file, under the `Serilog` node, :

```json
{
  "Serilog": {
    "WriteTo": [{ 
        "Name": "Elasticsearch", 
        "Args": { 
          "nodeUris": "http://localhost:9200;http://remotehost:9200/",
          "indexFormat": "custom-index-{0:yyyy.MM}",
          "templateName": "myCustomTemplate",
          "typeName": "myCustomLogEventType",
          "pipelineName": "myCustomPipelineName",
          "batchPostingLimit": 50,
          "period": 2000,
          "inlineFields": true,
          "minimumLogEventLevel": "Warning",
          "bufferBaseFilename":  "C:/Temp/LogDigipolis/docker-elk-serilog-web-buffer",
          "bufferFileSizeLimitBytes": 5242880,
          "bufferLogShippingInterval": 5000,
          "connectionGlobalHeaders" :"Authorization=Bearer SOME-TOKEN;OtherHeader=OTHER-HEADER-VALUE",
          "connectionTimeout": 5,
          "emitEventFailure": "WriteToSelfLog",
          "queueSizeLimit": "100000",
          "autoRegisterTemplate": true,
          "autoRegisterTemplateVersion": "ESv2",
          "overwriteTemplate": false,
          "registerTemplateFailure": "IndexAnyway",
          "deadLetterIndexName": "deadletter-{0:yyyy.MM}",
          "numberOfShards": 20,
          "numberOfReplicas": 10,
          "formatProvider": "My.Namespace.MyFormatProvider, My.Assembly.Name",
          "connection": "My.Namespace.MyConnection, My.Assembly.Name",
          "serializer": "My.Namespace.MySerializer, My.Assembly.Name",
          "connectionPool": "My.Namespace.MyConnectionPool, My.Assembly.Name",
          "customFormatter": "My.Namespace.MyCustomFormatter, My.Assembly.Name",
          "customDurableFormatter": "My.Namespace.MyCustomDurableFormatter, My.Assembly.Name",
          "failureSink": "My.Namespace.MyFailureSink, My.Assembly.Name"
        }       
    }]
  }
}
```

See the XML `<appSettings>` example above for a discussion of available `Args` options.

### Handling errors

From version 5.5 you have the option to specify how to handle issues with Elasticsearch. Since the sink delivers in a batch, it might be possible that one or more events could actually not be stored in the Elasticseach store.
Can be a mapping issue for example. It is hard to find out what happened here. There is a new option called *EmitEventFailure* which is an enum (flagged) with the following options:

 - WriteToSelfLog, the default option in which the errors are written to the SelfLog.
 - WriteToFailureSink, the failed events are send to another sink. Make sure to configure this one by setting the FailureSink option.
 - ThrowException, in which an exception is raised.
 - RaiseCallback, the failure callback function will be called when the event cannot be submitted to Elasticsearch. Make sure to set the FailureCallback option to handle the event.

An example:

```csharp
.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://localhost:9200")) 
                {
                    FailureCallback = e => Console.WriteLine("Unable to submit event " + e.MessageTemplate),
                    EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog |
                                       EmitEventFailureHandling.WriteToFailureSink |
                                       EmitEventFailureHandling.RaiseCallback,
                    FailureSink = new FileSink("./failures.txt", new JsonFormatter(), null)
                })
```

With the AutoRegisterTemplate option the sink will write a default template to Elasticsearch. When this template is not there, you might not want to index as it can influence the data quality.
Since version 5.5 you can use the RegisterTemplateFailure option. Set it to one of the following options:

 - IndexAnyway; the default option, the events will be send to the server
 - IndexToDeadletterIndex; using the deadletterindex format, it will write the events to the deadletter queue. When you fix your template mapping, you can copy your data into the right index.
 - FailSink; this will simply fail the sink by raising an exception.

 
### Breaking changes for version 6

Starting from version 6, the sink has been upgraded to work with Elasticsearch 6.0 and has support for the new templates used by ES 6. 

If you use the `AutoRegisterTemplate` option, you need to set the `AutoRegisterTemplateVersion` option to `ESv6` in order to generate default templates that are compatible with the breaking changes in ES 6.

### Breaking changes for version 4

Starting from version 4, the sink has been upgraded to work with Serilog 2.0 and has .NET Core support.

### Breaking changes for version 3

Starting from version 3, the sink supports the Elasticsearch.Net 2 package and Elasticsearch version 2. If you need Elasticsearch 1.x support, then stick with version 2 of the sink.
The function
```csharp
protected virtual ElasticsearchResponse<T> EmitBatchChecked<T>(IEnumerable<LogEvent> events)
```
now uses a generic type. This allows you to map to either DynamicResponse when using Elasticsearch.NET or to BulkResponse if you want to use NEST.

We also dropped support for .NET 4 since the Elasticsearch.NET client also does not support this version of the framework anymore. If you need to use .net 4, then you need to stick with the 2.x version of the sink.

### Breaking changes for version 2

Be aware that version 2 introduces some breaking changes.

- The overloads have been reduced to a single Elasticsearch function in which you can pass an options object.
- The namespace and function names are now Elasticsearch instead of ElasticSearch everywhere
- The Exceptions recorded by Serilog are customer serialized into the Exceptions property which is an array instead of an object.
- Inner exceptions are recorded in the same array but have an increasing depth parameter. So instead of nesting objects you need to look at this parameter to find the depth of the exception.
- Do no longer use the mapping once provided in the Gist. The Sink can automatically create the right mapping for you, but this feature is disabled by default. We advice you to use it.
- Since version 2.0.42 the ability to register this sink using the AppSettings reader is restored. You can pass in a node (or collection of nodes) and optionally an indexname and template.
