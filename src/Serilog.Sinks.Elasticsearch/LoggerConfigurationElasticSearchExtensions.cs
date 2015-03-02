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
using System.ComponentModel;
using System.Net.Configuration;
using Elasticsearch.Net.Connection;
using Elasticsearch.Net.Serialization;
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Sinks.ElasticSearch;

namespace Serilog
{
    /// <summary>
    /// Adds the WriteTo.ElasticSearch() extension method to <see cref="LoggerConfiguration"/>.
    /// </summary>
    public static class LoggerConfigurationElasticSearchExtensions
    {

        /// <summary>
        /// Adds a sink that writes log events as documents to an ElasticSearch index.
        /// This works great with the Kibana web interface when using the default settings.
        /// Make sure to add a template to ElasticSearch like the one found here:
        /// https://gist.github.com/mivano/9688328
        /// </summary>
        /// <param name="loggerSinkConfiguration"></param>
        /// <param name="options">Provides options specific to the Elasticsearch sink</param>
        /// <returns></returns>
        public static LoggerConfiguration Elasticsearch(
            this LoggerSinkConfiguration loggerSinkConfiguration, 
            ElasticsearchSinkOptions options = null)
        {
            //TODO make sure we do not kill appdata injection
            //TODO handle bulk errors and write to self log, what does logstash do in this case?
            //TODO NEST trace logging ID's to corrolate requests to eachother
            //Deal with positional formatting in fields property  (default to scalar string in mapping)
            options = options ?? new ElasticsearchSinkOptions(new [] { new Uri("http://localhost:9200") });
            var sink = new ElasticsearchSink(options);
            sink.RegisterTemplateIfNeeded();
            return loggerSinkConfiguration.Sink(sink, options.MinimumLogEventLevel ?? LevelAlias.Minimum);
        }
    }
}
