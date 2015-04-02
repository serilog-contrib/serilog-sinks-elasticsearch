# Serilog.Sinks.Elasticsearch

[![Build status](https://ci.appveyor.com/api/projects/status/bk367tcnx9qt2sjy/branch/master?svg=true)](https://ci.appveyor.com/project/serilog/serilog-sinks-elasticsearch/branch/master)

Send your log events directly into ElasticSearch. By default it will connect to an ElasticSearch node running on your localhost and port 9200. Indexes are created automatically and are based on the indexFormat which is standard logstash-{yyyy.MM.dd} matching the logstash format also used by Kibana. ElasticSearch and Kibana can be downloaded from [elasticsearch.net](http://www.elasticsearch.org/overview/elkdownloads/). You can override those defaults.

It is recommended to use a template so ElasticSearch knows how to efficiently handle the events. The sink will not force any and will create standard indexes. A default logstash format will already help the indexing process. You can find one [here](https://gist.github.com/mivano/9688328). Add it to ElasticSearch by placing it under the config/templates folder or by sending it to the node using [PUT](http://www.elasticsearch.org/guide/en/elasticsearch/reference/current/indices-templates.html#indices-templates).

**Package** - [Serilog.Sinks.ElasticSearch](http://nuget.org/packages/serilog.sinks.elasticsearch)
| **Platforms** - .NET 4.5

```csharp
var log = new LoggerConfiguration()
    .WriteTo.ElasticSearch()
    .CreateLogger();
```

See the [documentation](https://github.com/serilog/serilog-sinks-elasticsearch/wiki) pages for more information.
