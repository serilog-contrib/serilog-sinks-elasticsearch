# Serilog.Sinks.Elasticsearch

[![Build status](https://ci.appveyor.com/api/projects/status/bk367tcnx9qt2sjy/branch/master?svg=true)](https://ci.appveyor.com/project/serilog/serilog-sinks-elasticsearch/branch/master)

## What is this sink ?

The Serilog Elasticsearch sink project is a sink (basically a writer) for the Serilog logging framework. Structured log events are written to sinks and each sink is responsible for writing it to its own backend, database, store etc. This sink delivers the data to Elasticsearch, a NoSQL search engine. It does this in a similar structure as Logstash and makes it easy to use Kibana for visualizing your logs.

## Features

- Simple configuration to get log events published to Elasticsearch. Only server address is needed.
- All properties are stored inside fields in ES. This allows you to query on all the relevant data but also run analytics over this data.
- Be able to customize the store; specify the index name being used, the serializer or the connections to the server (load balanced).
- Durable mode; store the logevents first on disk before delivering them to ES making sure you never miss events if you have trouble connecting to your ES cluster.
- Automatically create the right mappings for the best usage of the log events in ES.

## Quick start

```powershell
Install-Package serilog.sinks.elasticsearch
```

Register the sink as shown below or use the appSettings reader (v2.0.42+).

```csharp
var loggerConfig = new LoggerConfiguration()
    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://localhost:9200") ){
             AutoRegisterTemplate = true,
     });
```

And start writing your events using Serilog.

## More information
- [Basic information](https://github.com/serilog/serilog-sinks-elasticsearch/wiki/basic-setup) on how to configure and use this sink.
- [Configuration options](https://github.com/serilog/serilog-sinks-elasticsearch/wiki/config) which you can use.
- How to use the [durability](https://github.com/serilog/serilog-sinks-elasticsearch/wiki/durability) mode.
- [Accessing](https://github.com/serilog/serilog-sinks-elasticsearch/wiki/access-logs) the logs using Kibana.
- Get the [NuGet package](http://www.nuget.org/packages/Serilog.Sinks.Elasticsearch).
- Report issues to the Serilog [issue tracker](https://github.com/serilog/serilog/issues). PR welcome, but do this against the dev branch.

### Breaking changes for version 2

Be aware that version 2 introduces some breaking changes. 

- The overloads have been reduced to a single Elasticsearch function in which you can pass an options object.
- The namespace and function names are now Elasticsearch instead of ElasticSearch everywhere
- The Exceptions recorded by Serilog are customer serialized into the Exceptions property which is an array instead of an object.
- Inner exceptions are recorded in the same array but have an increasing depth parameter. So instead of nesting objects you need to look at this parameter to find the depth of the exception.
- Do no longer use the mapping once provided in the Gist. The Sink can automatically create the right mapping for you, but this feature is disabled by default. We advice you to use it.
- Since version 2.0.42 the ability to register this sink using the AppSettings reader is restored. You can pass in a node (or collection of nodes) and optionally an indexname and template.
