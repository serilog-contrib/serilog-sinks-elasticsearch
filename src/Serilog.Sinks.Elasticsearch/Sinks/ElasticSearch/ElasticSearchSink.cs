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
using Elasticsearch.Net;
using Elasticsearch.Net.Connection;
using Elasticsearch.Net.Serialization;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;
using System.Text;
using System.Text.RegularExpressions;

namespace Serilog.Sinks.ElasticSearch
{
    /// <summary>
    /// Writes log events as documents to ElasticSearch.
    /// </summary>
    public class ElasticsearchSink : PeriodicBatchingSink
    {
        readonly ElasticsearchJsonFormatter _formatter;
        readonly string _typeName;
        readonly ElasticsearchClient _client;
        readonly Func<LogEvent, DateTimeOffset, string> _indexDecider;

        private readonly bool _registerTemplateOnStartup;
        private readonly string _templateName;
        private readonly string _templateMatchString;
        private static readonly Regex IndexFormatRegex = new Regex(@"^(.*)(?:\{0\:.+\})(.*)$");

        /// <summary>
        /// Creates a new ElasticsearchSink instance with the provided options
        /// </summary>
        /// <param name="options">Options configuring how the sink behaves, may NOT be null</param>
        public ElasticsearchSink(ElasticsearchSinkOptions options)
            : base(options.BatchPostingLimit, options.Period)
        {
            if (string.IsNullOrWhiteSpace(options.IndexFormat)) throw new ArgumentException("options.IndexFormat");
            if (string.IsNullOrWhiteSpace(options.TypeName)) throw new ArgumentException("options.TypeName");
            if (string.IsNullOrWhiteSpace(options.TemplateName)) throw new ArgumentException("options.TemplateName");
            this._templateName = options.TemplateName;
            this._templateMatchString = IndexFormatRegex.Replace(options.IndexFormat, @"$1*$2");
            
            _indexDecider = options.IndexDecider ?? ((@event, offset) => string.Format(options.IndexFormat, offset));
            _typeName = options.TypeName;

            var configuration = new ConnectionConfiguration(options.ConnectionPool)
                .SetTimeout(5000)
                .SetMaximumAsyncConnections(20);

            if (options.ModifyConnectionSetttings != null)
                configuration = options.ModifyConnectionSetttings(configuration);

            _client = new ElasticsearchClient(configuration, connection: options.Connection, serializer: options.Serializer);
            _formatter = new ElasticsearchJsonFormatter(
                formatProvider: options.FormatProvider,
                renderMessage: true,
                closingDelimiter: string.Empty,
                serializer: options.Serializer,
                inlineFields: options.InlineFields
            );

            this._registerTemplateOnStartup = options.AutoRegisterTemplate;
        }

        Func<LogEvent, DateTimeOffset, string> DefaultIndexDecider(string indexFormat)
        {
            return (@event, offset) => string.Format(indexFormat, offset);
        }


        /// <summary>
        /// Register the elasticsearch index template if the provided options mandate it.
        /// </summary>
        public void RegisterTemplateIfNeeded()
        {
            if (!this._registerTemplateOnStartup) return;
            var result = this._client.IndicesPutTemplateForAll<VoidResponse>(this._templateName, new
            {
                template = this._templateMatchString,
                settings = new Dictionary<string, string>
                {
                    {"index.refresh_interval", "5s"}
                },
                mappings = new
                {
                    _default_ = new
                    {
                        _all = new { enabled = true },
                        dynamic_templates = new[] 
                        {
                            new 
                            {
                                string_fields = new 
                                {
                                    match = "*",
                                    match_mapping_type = "string",
                                    mapping = new 
                                    {
                                        type = "string", index = "analyzed", omit_norms = true,
                                        fields = new 
                                        {
                                            raw = new
                                            {
                                                type= "string", index = "not_analyzed", ignore_above = 256
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        properties = new Dictionary<string, object>
                        {
                            { "message", new { type = "string", index =  "analyzed" } },
                            { "exceptions", new
                            {
                                type = "nested", properties =  new Dictionary<string, object>
                                {
                                    { "Depth", new { type = "integer" } },
                                    { "RemoteStackIndex", new { type = "integer" } },
                                    { "HResult", new { type = "integer" } },
                                    { "StackTraceString", new { type = "string", index = "analyzed" } },
                                    { "RemoteStackTraceString", new { type = "string", index = "analyzed" } },
                                    { "ExceptionMessage", new
                                    {
                                        type = "object", properties = new Dictionary<string, object>
                                        {
                                            { "MemberType", new { type = "integer" } },
                                        }
                                    }}
                                }
                            } }
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Emit a batch of log events, running to completion synchronously.
        /// </summary>
        /// <param name="events">The events to emit.</param>
        /// <remarks>
        /// Override either <see cref="M:Serilog.Sinks.PeriodicBatching.PeriodicBatchingSink.EmitBatch(System.Collections.Generic.IEnumerable{Serilog.Events.LogEvent})" />
        ///  or <see cref="M:Serilog.Sinks.PeriodicBatching.PeriodicBatchingSink.EmitBatchAsync(System.Collections.Generic.IEnumerable{Serilog.Events.LogEvent})" />,
        /// not both.
        /// </remarks>
        protected override void EmitBatch(IEnumerable<LogEvent> events)
        {
            // ReSharper disable PossibleMultipleEnumeration
            if (!events.Any())
                return;

            var payload = new List<string>();

            foreach (var e in events)
            {
                var indexName = _indexDecider(e, e.Timestamp.ToUniversalTime());
                var action = new { index = new { _index = indexName, _type = _typeName } };
                var actionJson = _client.Serializer.Serialize(action, SerializationFormatting.None);
                payload.Add(Encoding.UTF8.GetString(actionJson));
                var sw = new StringWriter();
                _formatter.Format(e, sw);
                payload.Add(sw.ToString());
            }

            _client.Bulk(payload);
        }
    }
}
