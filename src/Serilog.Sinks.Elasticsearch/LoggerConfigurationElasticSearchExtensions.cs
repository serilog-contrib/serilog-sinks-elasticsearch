// Copyright 2014 Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;
using System.Collections.Specialized;
using System.ComponentModel;
using Elasticsearch.Net;
using Serilog.Formatting;
using Serilog.Sinks.Elasticsearch.Durable;

namespace Serilog
{
    /// <summary>
    /// Adds the WriteTo.Elasticsearch() extension method to <see cref="LoggerConfiguration"/>.
    /// </summary>
    public static class LoggerConfigurationElasticsearchExtensions
    {
        const string DefaultNodeUri = "http://localhost:9200";

        /// <summary>
        /// Adds a sink that writes log events as documents to an Elasticsearch index.
        /// This works great with the Kibana web interface when using the default settings.
        /// 
        /// By passing in the BufferBaseFilename, you make this into a durable sink. 
        /// Meaning it will log to disk first and tries to deliver to the Elasticsearch server in the background.
        /// </summary>
        /// <remarks>
        /// Make sure to have a sensible mapping in your Elasticsearch indexes. 
        /// You can automatically create one by specifying this in the options.
        /// </remarks>
        /// <param name="loggerSinkConfiguration">Options for the sink.</param>
        /// <param name="options">Provides options specific to the Elasticsearch sink</param>
        /// <returns>LoggerConfiguration object</returns>
        public static LoggerConfiguration Elasticsearch(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            ElasticsearchSinkOptions options = null)
        {
            options = options ?? new ElasticsearchSinkOptions(new[] { new Uri(DefaultNodeUri) });

            var sink = string.IsNullOrWhiteSpace(options.BufferBaseFilename)
                ? (ILogEventSink)new ElasticsearchSink(options)
                : new DurableElasticsearchSink(options);

            return loggerSinkConfiguration.Sink(
                sink,
                restrictedToMinimumLevel: options.MinimumLogEventLevel ?? LevelAlias.Minimum,
                levelSwitch: options.LevelSwitch
            );
        }

        /// <summary>
        /// Overload to allow basic configuration through AppSettings.
        /// </summary>
        /// <param name="loggerSinkConfiguration">Options for the sink.</param>
        /// <param name="nodeUris">A comma or semi-colon separated list of URIs for Elasticsearch nodes.</param>
        /// <param name="indexFormat"><see cref="ElasticsearchSinkOptions.IndexFormat"/></param>
        /// <param name="templateName"><see cref="ElasticsearchSinkOptions.TemplateName"/></param>
        /// <param name="typeName"><see cref="ElasticsearchSinkOptions.TypeName"/></param>
        /// <param name="batchPostingLimit"><see cref="ElasticsearchSinkOptions.BatchPostingLimit"/></param>
        /// <param name="period"><see cref="ElasticsearchSinkOptions.Period"/></param>
        /// <param name="inlineFields"><see cref="ElasticsearchSinkOptions.InlineFields"/></param>
        /// <param name="restrictedToMinimumLevel">The minimum log event level required in order to write an event to the sink. Ignored when <paramref name="levelSwitch"/> is specified.</param>
        /// <param name="levelSwitch">A switch allowing the pass-through minimum level to be changed at runtime.</param>
        /// <param name="bufferBaseFilename"><see cref="ElasticsearchSinkOptions.BufferBaseFilename"/></param>
        /// <param name="bufferFileSizeLimitBytes"><see cref="ElasticsearchSinkOptions.BufferFileSizeLimitBytes"/></param>
        /// <param name="bufferLogShippingInterval"><see cref="ElasticsearchSinkOptions.BufferLogShippingInterval"/></param>
        /// <param name="connectionGlobalHeaders">A comma or semi-colon separated list of key value pairs of headers to be added to each elastic http request</param>
        [Obsolete("New code should not be compiled against this obsolete overload"), EditorBrowsable(EditorBrowsableState.Never)]
        public static LoggerConfiguration Elasticsearch(
           this LoggerSinkConfiguration loggerSinkConfiguration,
           string nodeUris,
           string indexFormat,
           string templateName,
           string typeName,
           int batchPostingLimit,
           int period,
           bool inlineFields,
           LogEventLevel restrictedToMinimumLevel,
           string bufferBaseFilename,
           long? bufferFileSizeLimitBytes,
           long bufferLogShippingInterval,
           string connectionGlobalHeaders,
           LoggingLevelSwitch levelSwitch)
        {
            return Elasticsearch(loggerSinkConfiguration, nodeUris, indexFormat, templateName, typeName, batchPostingLimit, period, inlineFields, restrictedToMinimumLevel, bufferBaseFilename,
                bufferFileSizeLimitBytes, bufferLogShippingInterval, connectionGlobalHeaders, levelSwitch, 5, EmitEventFailureHandling.WriteToSelfLog, 100000, null, false,
                AutoRegisterTemplateVersion.ESv2, false, RegisterTemplateRecovery.IndexAnyway, null, null, null);
        }

        /// <summary>
        /// Overload to allow basic configuration through AppSettings.
        /// </summary>
        /// <param name="loggerSinkConfiguration">Options for the sink.</param>
        /// <param name="nodeUris">A comma or semi-colon separated list of URIs for Elasticsearch nodes.</param>
        /// <param name="indexFormat"><see cref="ElasticsearchSinkOptions.IndexFormat"/></param>
        /// <param name="templateName"><see cref="ElasticsearchSinkOptions.TemplateName"/></param>
        /// <param name="typeName"><see cref="ElasticsearchSinkOptions.TypeName"/></param>
        /// <param name="batchPostingLimit"><see cref="ElasticsearchSinkOptions.BatchPostingLimit"/></param>
        /// <param name="period"><see cref="ElasticsearchSinkOptions.Period"/></param>
        /// <param name="inlineFields"><see cref="ElasticsearchSinkOptions.InlineFields"/></param>
        /// <param name="restrictedToMinimumLevel">The minimum log event level required in order to write an event to the sink. Ignored when <paramref name="levelSwitch"/> is specified.</param>
        /// <param name="levelSwitch">A switch allowing the pass-through minimum level to be changed at runtime.</param>
        /// <param name="bufferBaseFilename"><see cref="ElasticsearchSinkOptions.BufferBaseFilename"/></param>
        /// <param name="bufferFileSizeLimitBytes"><see cref="ElasticsearchSinkOptions.BufferFileSizeLimitBytes"/></param>
        /// <param name="bufferFileCountLimit"><see cref="ElasticsearchSinkOptions.BufferFileCountLimit"/></param>        
        /// <param name="bufferLogShippingInterval"><see cref="ElasticsearchSinkOptions.BufferLogShippingInterval"/></param>
        /// <param name="connectionGlobalHeaders">A comma or semi-colon separated list of key value pairs of headers to be added to each elastic http request</param>
        /// <param name="connectionTimeout"><see cref="ElasticsearchSinkOptions.ConnectionTimeout"/>The connection timeout (in seconds) when sending bulk operations to elasticsearch (defaults to 5).</param>   
        /// <param name="emitEventFailure"><see cref="ElasticsearchSinkOptions.EmitEventFailure"/>Specifies how failing emits should be handled.</param>  
        /// <param name="queueSizeLimit"><see cref="ElasticsearchSinkOptions.QueueSizeLimit"/>The maximum number of events that will be held in-memory while waiting to ship them to Elasticsearch. Beyond this limit, events will be dropped. The default is 100,000. Has no effect on durable log shipping.</param>   
        /// <param name="pipelineName"><see cref="ElasticsearchSinkOptions.PipelineName"/>Name the Pipeline where log events are sent to sink. Please note that the Pipeline should be existing before the usage starts.</param>   
        /// <param name="autoRegisterTemplate"><see cref="ElasticsearchSinkOptions.AutoRegisterTemplate"/>When set to true the sink will register an index template for the logs in elasticsearch.</param>   
        /// <param name="autoRegisterTemplateVersion"><see cref="ElasticsearchSinkOptions.AutoRegisterTemplateVersion"/>When using the AutoRegisterTemplate feature, this allows to set the Elasticsearch version. Depending on the version, a template will be selected. Defaults to pre 5.0.</param>  
        /// <param name="overwriteTemplate"><see cref="ElasticsearchSinkOptions.OverwriteTemplate"/>When using the AutoRegisterTemplate feature, this allows you to overwrite the template in Elasticsearch if it already exists. Defaults to false</param>   
        /// <param name="registerTemplateFailure"><see cref="ElasticsearchSinkOptions.RegisterTemplateFailure"/>Specifies the option on how to handle failures when writing the template to Elasticsearch. This is only applicable when using the AutoRegisterTemplate option.</param>  
        /// <param name="deadLetterIndexName"><see cref="ElasticsearchSinkOptions.DeadLetterIndexName"/>Optionally set this value to the name of the index that should be used when the template cannot be written to ES.</param>  
        /// <param name="numberOfShards"><see cref="ElasticsearchSinkOptions.NumberOfShards"/>The default number of shards.</param>   
        /// <param name="numberOfReplicas"><see cref="ElasticsearchSinkOptions.NumberOfReplicas"/>The default number of replicas.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="connection">Allows you to override the connection used to communicate with elasticsearch.</param>
        /// <param name="serializer">When passing a serializer unknown object will be serialized to object instead of relying on their ToString representation</param>
        /// <param name="connectionPool">The connectionpool describing the cluster to write event to</param>
        /// <param name="customFormatter">Customizes the formatter used when converting log events into ElasticSearch documents. Please note that the formatter output must be valid JSON :)</param>
        /// <param name="customDurableFormatter">Customizes the formatter used when converting log events into the durable sink. Please note that the formatter output must be valid JSON :)</param>
        /// <param name="failureSink">Sink to use when Elasticsearch is unable to accept the events. This is optionally and depends on the EmitEventFailure setting.</param>   
        /// <param name="singleEventSizePostingLimit"><see cref="ElasticsearchSinkOptions.SingleEventSizePostingLimit"/>The maximum length of an event allowed to be posted to Elasticsearch.default null</param>
        /// <param name="templateCustomSettings">Add custom elasticsearch settings to the template</param>
        /// <param name="batchAction">Configures the OpType being used when inserting document in batch. Must be set to create for data streams.</param>
        /// <returns>LoggerConfiguration object</returns>
        /// <exception cref="ArgumentNullException"><paramref name="nodeUris"/> is <see langword="null" />.</exception>
        public static LoggerConfiguration Elasticsearch(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            string nodeUris,
            string indexFormat = null,
            string templateName = null,
            string typeName = "logevent",
            int batchPostingLimit = 50,
            int period = 2,
            bool inlineFields = false,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            string bufferBaseFilename = null,
            long? bufferFileSizeLimitBytes = null,
            long bufferLogShippingInterval = 5000,
            string connectionGlobalHeaders = null,
            LoggingLevelSwitch levelSwitch = null,
            int connectionTimeout = 5,
            EmitEventFailureHandling emitEventFailure = EmitEventFailureHandling.WriteToSelfLog,
            int queueSizeLimit = 100000,
            string pipelineName = null,
            bool autoRegisterTemplate = false,
            AutoRegisterTemplateVersion autoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv2,
            bool overwriteTemplate = false,
            RegisterTemplateRecovery registerTemplateFailure = RegisterTemplateRecovery.IndexAnyway,
            string deadLetterIndexName = null,
            int? numberOfShards = null,
            int? numberOfReplicas = null,
            IFormatProvider formatProvider = null,
            IConnection connection = null,
            IElasticsearchSerializer serializer = null,
            IConnectionPool connectionPool = null,
            ITextFormatter customFormatter = null,
            ITextFormatter customDurableFormatter = null,
            ILogEventSink failureSink = null,
            long? singleEventSizePostingLimit = null,
            int? bufferFileCountLimit = null,
            Dictionary<string,string> templateCustomSettings = null,
            ElasticOpType batchAction = ElasticOpType.Index)
        {
            if (string.IsNullOrEmpty(nodeUris))
                throw new ArgumentNullException(nameof(nodeUris), "No Elasticsearch node(s) specified.");

            IEnumerable<Uri> nodes = nodeUris
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(uriString => new Uri(uriString));

            var options = connectionPool == null ? new ElasticsearchSinkOptions(nodes) : new ElasticsearchSinkOptions(connectionPool);

            if (!string.IsNullOrWhiteSpace(indexFormat))
            {
                options.IndexFormat = indexFormat;
            }

            if (!string.IsNullOrWhiteSpace(templateName))
            {
                options.AutoRegisterTemplate = true;
                options.TemplateName = templateName;
            }

            options.TypeName = !string.IsNullOrWhiteSpace(typeName) ? typeName : null;

            options.BatchPostingLimit = batchPostingLimit;
            options.BatchAction = batchAction;
            options.SingleEventSizePostingLimit = singleEventSizePostingLimit;
            options.Period = TimeSpan.FromSeconds(period);
            options.InlineFields = inlineFields;
            options.MinimumLogEventLevel = restrictedToMinimumLevel;
            options.LevelSwitch = levelSwitch;

            if (!string.IsNullOrWhiteSpace(bufferBaseFilename))
            {
                Path.GetFullPath(bufferBaseFilename);       // validate path
                options.BufferBaseFilename = bufferBaseFilename;
            }

            if (bufferFileSizeLimitBytes.HasValue)
            {
                options.BufferFileSizeLimitBytes = bufferFileSizeLimitBytes.Value;
            }

            if (bufferFileCountLimit.HasValue)
            {
                options.BufferFileCountLimit = bufferFileCountLimit.Value;
            }
            options.BufferLogShippingInterval = TimeSpan.FromMilliseconds(bufferLogShippingInterval);

            if (!string.IsNullOrWhiteSpace(connectionGlobalHeaders))
            {
                NameValueCollection headers = new NameValueCollection();
                connectionGlobalHeaders
                    .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList()
                    .ForEach(headerValueStr =>
                    {
                        var headerValue = headerValueStr.Split(new[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);
                        headers.Add(headerValue[0], headerValue[1]);
                    });

                options.ModifyConnectionSettings = (c) => c.GlobalHeaders(headers);
            }

            options.ConnectionTimeout = TimeSpan.FromSeconds(connectionTimeout);
            options.EmitEventFailure = emitEventFailure;
            options.QueueSizeLimit = queueSizeLimit;
            options.PipelineName = pipelineName;

            options.AutoRegisterTemplate = autoRegisterTemplate;
            options.AutoRegisterTemplateVersion = autoRegisterTemplateVersion;
            options.RegisterTemplateFailure = registerTemplateFailure;
            options.OverwriteTemplate = overwriteTemplate;
            options.NumberOfShards = numberOfShards;
            options.NumberOfReplicas = numberOfReplicas;

            if (!string.IsNullOrWhiteSpace(deadLetterIndexName))
            {
                options.DeadLetterIndexName = deadLetterIndexName;
            }

            options.FormatProvider = formatProvider;
            options.FailureSink = failureSink;
            options.Connection = connection;
            options.CustomFormatter = customFormatter;
            options.CustomDurableFormatter = customDurableFormatter;
            options.Serializer = serializer;

            options.TemplateCustomSettings = templateCustomSettings;

            return Elasticsearch(loggerSinkConfiguration, options);
        }
    }
}
