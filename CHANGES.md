4.x
 * *BREAKING CHANGE* This sink now uses Serilog 2.0. This is a breaking change, please use a version >=3.x of the sink if you want to use Serilog 1.x.

3.0.130
 * Added an optional ExceptionAsJsonObjectFormatter to support serializing exceptions as a single object (not as an array).
 
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
