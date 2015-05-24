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
            //TODO make sure we do not kill appdata injection
            //TODO handle bulk errors and write to self log, what does logstash do in this case?
            //TODO NEST trace logging ID's to corrolate requests to eachother

            options = options ?? new ElasticsearchSinkOptions(new[] { new Uri("http://localhost:9200") });

            var sink = string.IsNullOrWhiteSpace(options.BufferBaseFilename)
                ? (ILogEventSink)new ElasticsearchSink(options)
                : new DurableElasticsearchSink(options);

            return loggerSinkConfiguration.Sink(sink, options.MinimumLogEventLevel ?? LevelAlias.Minimum);
        }
    }
}
