## Changelog

8.1 
 * Updated sample to use .NET 2.1 and ES 7.5
 * Change default TypeName to '_doc' #298

8.0
 * Adds Elasticsearch 7.0 support #256
 * Adds DetectElasticsearchVersion to the sink that will detect the running cluster version. Something we now use to support sending Esv6 templates to Elasticsearch 7.x and Esv7 templates to Elasticsearch 6.x which should simplify upgrades.
 * Adds an integration test project. Spins up a 6.x and 7.x elasticsearch single node cluster in succession and asserts the default and the mixed mode through DetectElasticsearchVersion works.
 * Dropped support for net45 and netstandard 1.3

7.1
 * DurableElasticsearchSink is rewritten to use the same base code as the sink for Serilog.Sinks.Seq. Nuget Serilog.Sinks.File is now used instead of deprecated Serilog.Sinks.RollingFile. Lots of new fintuning options for file storage is added in ElasticsearchSinkOptions.  Updated  Serilog.Sinks.Elasticsearch.Sample.Main with SetupLoggerWithPersistantStorage with all available options for durable mode.
 * Changed datatype on singleEventSizePostingLimit  from int to long? with default value null. to make it possible ro reuse code from Sinks.Seq .
 * IndexDecider didnt worked well in buffer mode because of LogEvent was null. Added BufferIndexDecider.
 * Added BufferCleanPayload and an example which makes it possible to cleanup your invalid logging document if rejected from elastic because of inconsistent datatype on a field. It'seasy to miss errors in the self log now its possible to se logrows which is bad for elasticsearch in the elastic log.
 * Added BufferRetainedInvalidPayloadsLimitBytes A soft limit for the number of bytes to use for storing failed requests.
 * Added BufferFileCountLimit The maximum number of log files that will be retained.
 * Formatting has been moved to seperate package.

6.4
 * Render message by default (#160). 
 * Expose interface-typed options via appsettings (#162)

6.2
 * Extra overload added to support more settings via AppSettings reader. (#150)

6.1
 * Updated to elasticsearch 6 libraries (#153)
 * Fix field index option for 6.1+ template to use boolean value. (#148)

5.7
 * Supporting ES 6 template defitions while still supporting old versions. (https://github.com/serilog/serilog-sinks-elasticsearch/pull/142) See for details https://www.elastic.co/blog/strings-are-dead-long-live-strings
 * Pipeline decider added.
 * Ability to use loglevelswitch (https://github.com/serilog/serilog-sinks-elasticsearch/pull/139)

5.5
 * Errors from Elasticsearch can now be handled. Either by looking into the selflog, sending the failty events to another sink, handle the logevent yourself by using a callback or let the sink throw an exception.
 * BOM fix for buffered option.
 * The creation of the template might fail. You can now specify what kind of action should be taken if this happens.
 * Added a sample application.
 * Added a docker-compose file that allows you to start a local elasticsearch and kibana instance.
 * ConnectionTimeout is now set to be 5 seconds instead of 1 minute.
 * You can now set the queueSizeLimit, which limits the amount of events stored in the PeriodicBatching buffer. Does not impact the durable buffer.

5.4
 * Added support for pipelines in Elasticsearch. Pipelines allows you to change the ingress data by running it through Processors (https://www.elastic.co/blog/new-way-to-ingest-part-1).

5.3
 * JSON project file converted to CSProj, references updated. PR #109

5.2 
 * Next to the number of shards, you can also set the number of replicas. This will only apply to newly created indices.

5.1
 * You can specify the number of shards when creating the template mapping. This will only apply to newly created indices.

5.0
 * To make the sink work in line with the other sinks, there is a breaking change as described in PR (https://github.com/serilog/serilog-sinks-elasticsearch/pull/94). minimumLogEventLevel is renamed to restrictedToMinimumLevel. The behaviour is now also consistent when you set the minimum level.

4.x
 * *BREAKING CHANGE* This sink now uses Serilog 2.0. This is a breaking change, please use a version >=3.x of the sink if you want to use Serilog 1.x.

3.0.130
 * Added an optional ExceptionAsObjectJsonFormatter to support serializing exceptions as a single object (not as an array).
 
3.0.128
 * SpecificVersion set to False in order not to be dependent on a version of Elasticsearch or Serilog.

3.0.125
 * Dropped support for .NET 4 since the Elasticsearch.NET client also does not support this version of the framework anymore.

3.0.121
 * protected virtual ElasticsearchResponse<T> EmitBatchChecked<T>(IEnumerable<LogEvent> events) function now uses a generic type. This allows you to map to either DynamicResponse or to BulkResponse if you want to use NEST.

3.0.112
 * Added exponential backoff strategy when unable to send data to Elasticsearch when using the durable sink option.

3.0.98
 * Field names cannot contain a dot in ES 2, so they will get replaced by a / instead. See https://github.com/elastic/elasticsearch/issues/14594

3.x
 * *BREAKING CHANGE* This sink now uses the Elasticsearch.Net 2.x library to be compatible with Elasticsearch version 2. This is a breaking change, use a 2.x version of the sink to support Elasticsearch 1.x versions.

2.0.49
 * Fixed typo: ModifyConnectionSetttings to ModifyConnectionSettings.

2.0.42
 * Added an overload so the AppSettings reader can be used to configure the ES sink.

2.0.38
 * Fixes an issue where the index decider was not properly used with pusing events thorugh the ElasticLogShipper.

2.0.37
 * When auto register of the template is enabled, but the ES server is unavailable, the exception is logged to the selflog instead of bubbling up the exception.
 * omit_terms is set to true in the template.

2.0.0
 * Moved the Elasticsearch sink from its [original location](https://github.com/serilog/serilog)
