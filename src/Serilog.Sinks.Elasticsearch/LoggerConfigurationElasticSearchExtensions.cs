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
using System.Linq;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;

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
        /// <param name="options">Provides options specific to the Elasticsearch sink.</param>
        /// <param name="restrictedToMinimumLevel">The minimum level for events passed through the sink.</param>
        /// <returns>LoggerConfiguration object</returns>
        public static LoggerConfiguration Elasticsearch(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            ElasticsearchSinkOptions options = null,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum)
        {
            //TODO make sure we do not kill appdata injection
            //TODO handle bulk errors and write to self log, what does logstash do in this case?
            //TODO NEST trace logging ID's to correlate requests to eachother

            options = options ?? new ElasticsearchSinkOptions(new[] { new Uri(DefaultNodeUri) });

            var sink = string.IsNullOrWhiteSpace(options.BufferBaseFilename)
                ? (ILogEventSink)new ElasticsearchSink(options)
                : new DurableElasticsearchSink(options);

#pragma warning disable CS0618 // Type or member is obsolete
            // This can be removed once MinimumLogEventLevel is completely removed.
            var levelIncludingLegacySetting = options.MinimumLogEventLevel ?? restrictedToMinimumLevel;
#pragma warning restore CS0618 // Type or member is obsolete

            return loggerSinkConfiguration.Sink(sink, levelIncludingLegacySetting);
        }

        /// <summary>
        /// Overload to allow basic configuration through AppSettings.
        /// </summary>
        /// <param name="loggerSinkConfiguration">Options for the sink.</param>
        /// <param name="nodeUris">A comma or semi column separated list of URIs for Elasticsearch nodes.</param>
        /// <param name="indexFormat"><see cref="ElasticsearchSinkOptions.IndexFormat"/></param>
        /// <param name="templateName"><see cref="ElasticsearchSinkOptions.TemplateName"/></param>
        /// <param name="restrictedToMinimumLevel">The minimum level for events passed through the sink.</param>
        /// <returns>LoggerConfiguration object</returns>
        /// <exception cref="ArgumentNullException"><paramref name="nodeUris"/> is <see langword="null" />.</exception>
        public static LoggerConfiguration Elasticsearch(
            this LoggerSinkConfiguration loggerSinkConfiguration,
            string nodeUris,
            string indexFormat = null,
            string templateName = null,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum)
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

            return Elasticsearch(loggerSinkConfiguration, options, restrictedToMinimumLevel);
        }
    }
}
