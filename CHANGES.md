# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [9.0.1]

- fixes: v9.0.0 fails template creation with ES v7 with ElasticsearchCl… by @nenadvicentic in #533

## [Unreleased]

## [9.0.0] - 2023-01-23

### Added
 - PR #462
 - PR #488

### Major Changes
- `DetectElasticsearchVersion` is set to `true` by default.
- When `DetectElasticsearchVersion` is set to `false` Elasticsearch version 7 is assumed (as it has broadest wire-compatibility at the moment - v7 and v8)
- When `DetectElasticsearchVersion` is set to `true`, `TypeName` is handled automatically across different versions of Elasticserach (6.x to 8.x). For example, user-defined name will NOT be used on v7 and v8. Also, correct templates endpoint will be picked up.
- Elasticsearch 8.x endpoint for templates is supported (`_index_template`)
- Internal class `ElasticsearchVersionManager` has been added, mainly to handle situations where detection of version fails or when it is disabled. In the case of fallback, sink will assume "default" version 7.
- Elasticsearch.NET client version 7.15.2 (latest version 7, until new `Elastic.Clients.Elasticsearch` 8.x catches up functional parity with 7.x).
- Elasticsearch server versions 2 and 5 are no longer supported.

### Other Changes
- Nuget pacakges have been updated (except for the Elasticsearch integration-tests related packages)
- Most of the `ElasticserachSink` functionality has been moved into internal `BatchedElasticsearchSink` class that inherits from `IBatchedLogEventSink`, so it complies with new recommended way of integration with `PeriodicBatchingSink` and we don't use obsolete constructors.
- `ConnectionStub` was moved out of `ElasticsearchSinkTestsBase` and extended. Both are now in `/Stubs` subfolder. Newer versions of Elasticsearch.NET client are now using "pre-flight" request to determine if endpoint is Elasticsearch and if it is indeed between 6.x and 8.x. `ConnectionStub` had to accommodate for that.
- Unit tests have been fixed/added accordingly, running on multiple target frameworks (`net6`, `net7` and `net48`).
- Built-in .NET SDK conditional compilation symbols are now used (e.g NETFRAMEWORK).

## [9.0.0] - 2022-04-29

### Fixed
 - Dropped support for old .NET framework, and now uses .NET Core. Previous versions were out of support by MS anyway.
 - Fixed the build so it uses GitHub Actions as AppVeyor was not working
 - Created packages in GitHub Packages 

### Added
 - PR #420
 - PR #416 
 - PR #406
## [8.4.1] - 2020-09-28
### Fixed
- Make sure TypeName is set to `_doc` when setting the template version to `ESv7`.
   The regression was introduced in `8.4.0`. #364

## [8.4.0] - 2020-09-19
### Added
- Do not crash when ES is unreachable and the option `DetectElasticsearchVersion` is set to true. #359
- Create snupkg instead of the old style symbol files. #360
- Support for explicitly setting `Options.TypeName` to `null` this will remove the 
   deprecated `_type` from the bulk payload being sent to Elastic. Earlier an exception was
   thrown if the `Options.TypeName` was `null`. _If you're using `AutoRegisterTemplateVersion.ESv7`
   we'll not force the type to `_doc` if it's set to `null`. This is a small step towards support 
   for writing logs to Elastic v8. #345
- Support for setting the Elastic `op_type` e.g. `index` or `create` for bulk actions.
   This is a requirement for writing to [data streams](https://www.elastic.co/guide/en/elasticsearch/reference/7.9/data-streams.html)
   that's only supporting `create`. Data streams is a more slipped stream way to handle rolling
   indices, that previous required an ILM, template and a magic write alias. Now it's more integrated
   in Elasticsearch and Kibana. If you're running Elastic `7.9` you'll get rolling indices out of the box
   with this configuration:
   ```
   TypeName = null,
   IndexFormat = "logs-my-stream",
   BatchAction = ElasticOpType.Create,
   ```
   _Note: that current templates doesn't support data streams._ #355

### Removed
- Disable dot-escaping for field names, because ELK already supports dots in field names. #351

8.2
 * Allow the use of templateCustomSettings when reading from settings json (#315)
 * Updated Elasticsearch.Net dependency #340

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
