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
                restrictedToMinimumLevel : options.MinimumLogEventLevel ?? LevelAlias.Minimum,
                levelSwitch : options.LoggingLevelSwitch
            );
        }

        /// <summary>
        /// Overload to allow basic configuration through AppSettings.
        /// </summary>
        /// <param name="loggerSinkConfiguration">Options for the sink.</param>
        /// <param name="nodeUris">A comma or semi column separated list of URIs for Elasticsearch nodes.</param>
        /// <param name="indexFormat"><see cref="ElasticsearchSinkOptions.IndexFormat"/></param>
        /// <param name="templateName"><see cref="ElasticsearchSinkOptions.TemplateName"/></param>
        /// <param name="typeName"><see cref="ElasticsearchSinkOptions.TypeName"/></param>
        /// <param name="batchPostingLimit"><see cref="ElasticsearchSinkOptions.BatchPostingLimit"/></param>
        /// <param name="period"><see cref="ElasticsearchSinkOptions.Period"/></param>
        /// <param name="inlineFields"><see cref="ElasticsearchSinkOptions.InlineFields"/></param>
        /// <param name="restrictedToMinimumLevel">The minimum log event level required in order to write an event to the sink. Ignored when <paramref name="loggingLevelSwitch"/> is specified.</param>
        /// <param name="loggingLevelSwitch" > A switch allowing the pass-through minimum level to be changed at runtime.</param>
        /// <param name="bufferBaseFilename"><see cref="ElasticsearchSinkOptions.BufferBaseFilename"/></param>
        /// <param name="bufferFileSizeLimitBytes"><see cref="ElasticsearchSinkOptions.BufferFileSizeLimitBytes"/></param>
        /// <param name="bufferLogShippingInterval"><see cref="ElasticsearchSinkOptions.BufferLogShippingInterval"/></param>
        /// <param name="connectionGlobalHeaders">A comma or semi column separated list of key value pairs of headers to be added to each elastic http request</param>   
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
            LoggingLevelSwitch loggingLevelSwitch = null)
        {
            if (string.IsNullOrEmpty(nodeUris))
                throw new ArgumentNullException("nodeUris", "No Elasticsearch node(s) specified.");

            IEnumerable<Uri> nodes = nodeUris
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(uriString => new Uri(uriString));

            var options = new ElasticsearchSinkOptions(nodes);

            if (!string.IsNullOrWhiteSpace(indexFormat))
            {
                options.IndexFormat = indexFormat;
            }

            if (!string.IsNullOrWhiteSpace(templateName))
            {
                options.AutoRegisterTemplate = true;
                options.TemplateName = templateName;
            }

            if (!string.IsNullOrWhiteSpace(typeName))
            {
                options.TypeName = typeName;
            }

            options.BatchPostingLimit = batchPostingLimit;
            options.Period = TimeSpan.FromSeconds(period);
            options.InlineFields = inlineFields;
            options.MinimumLogEventLevel = restrictedToMinimumLevel;            
            options.LoggingLevelSwitch = loggingLevelSwitch;

            if (!string.IsNullOrWhiteSpace(bufferBaseFilename))
            {
                Path.GetFullPath(bufferBaseFilename);       // validate path
                options.BufferBaseFilename = bufferBaseFilename;
            }

            if (bufferFileSizeLimitBytes.HasValue)
            {
                options.BufferFileSizeLimitBytes = bufferFileSizeLimitBytes.Value;
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

            return Elasticsearch(loggerSinkConfiguration, options);
        }
    }
}
