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
using System.Text;
using System.Text.RegularExpressions;
using Elasticsearch.Net;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Formatting;

namespace Serilog.Sinks.Elasticsearch
{
    internal class ElasticsearchSinkState
    {
        public static ElasticsearchSinkState Create(ElasticsearchSinkOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            return new ElasticsearchSinkState(options);
        }

        private readonly ElasticsearchSinkOptions _options;
        readonly Func<LogEvent, DateTimeOffset, string> _indexDecider;

        private readonly ITextFormatter _formatter;
        private readonly ITextFormatter _durableFormatter;

        private readonly ElasticLowLevelClient _client;

        readonly string _typeName;
        private readonly bool _registerTemplateOnStartup;
        private readonly string _templateName;
        private readonly string _templateMatchString;
        private static readonly Regex IndexFormatRegex = new Regex(@"^(.*)(?:\{0\:.+\})(.*)$");

        public ElasticsearchSinkOptions Options => this._options;
        public IElasticLowLevelClient Client => this._client;
        public ITextFormatter Formatter => this._formatter;
        public ITextFormatter DurableFormatter => this._durableFormatter;


        private ElasticsearchSinkState(ElasticsearchSinkOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.IndexFormat)) throw new ArgumentException("options.IndexFormat");
            if (string.IsNullOrWhiteSpace(options.TypeName)) throw new ArgumentException("options.TypeName");
            if (string.IsNullOrWhiteSpace(options.TemplateName)) throw new ArgumentException("options.TemplateName");

            this._templateName = options.TemplateName;
            this._templateMatchString = IndexFormatRegex.Replace(options.IndexFormat, @"$1*$2");

            _indexDecider = options.IndexDecider ?? ((@event, offset) => string.Format(options.IndexFormat, offset));

            _typeName = options.TypeName;
            _options = options;

            Func<ConnectionConfiguration, IElasticsearchSerializer> serializerFactory = null;
            if (options.Serializer != null)
            {
                serializerFactory = s => options.Serializer;
            }
            ConnectionConfiguration configuration = new ConnectionConfiguration(options.ConnectionPool, options.Connection, serializerFactory)
                .RequestTimeout(options.ConnectionTimeout);

            if (options.ModifyConnectionSettings != null)
                configuration = options.ModifyConnectionSettings(configuration);

            configuration.ThrowExceptions();
            _client = new ElasticLowLevelClient(configuration);

            _formatter = options.CustomFormatter ?? new ElasticsearchJsonFormatter(
                formatProvider: options.FormatProvider,
                renderMessage: true,
                closingDelimiter: string.Empty,
                serializer: options.Serializer,
                inlineFields: options.InlineFields
            );
            _durableFormatter = options.CustomDurableFormatter ?? new ElasticsearchJsonFormatter(
               formatProvider: options.FormatProvider,
               renderMessage: true,
               closingDelimiter: Environment.NewLine,
               serializer: options.Serializer,
               inlineFields: options.InlineFields
           );

            _registerTemplateOnStartup = options.AutoRegisterTemplate;
        }


        public string Serialize(object o)
        {
            return _client.Serializer.SerializeToString(o, SerializationFormatting.None);
        }

        public string GetIndexForEvent(LogEvent e, DateTimeOffset offset)
        {
            return this._indexDecider(e, offset);
        }

        /// <summary>
        /// Register the elasticsearch index template if the provided options mandate it.
        /// </summary>
        public void RegisterTemplateIfNeeded()
        {
            if (!this._registerTemplateOnStartup) return;

            try
            {
                if (!this._options.OverwriteTemplate)
                {
                    var templateExistsResponse = this._client.IndicesExistsTemplateForAll<DynamicResponse>(this._templateName);
                    if (templateExistsResponse.HttpStatusCode == 200) return;
                }

                if (_options.GetTemplateContent != null)
                {
                    this._client.IndicesPutTemplateForAll<DynamicResponse>(this._templateName, _options.GetTemplateContent());
                }
                else
                {
                    var settings = new Dictionary<string, string>
                    {
                        {"index.refresh_interval", "5s"}
                    };

                    if (_options.NumberOfShards.HasValue)
                        settings.Add("number_of_shards", _options.NumberOfShards.Value.ToString());
					
					if (_options.NumberOfReplicas.HasValue)
                        settings.Add("number_of_replicas", _options.NumberOfReplicas.Value.ToString());

                    var result = this._client.IndicesPutTemplateForAll<DynamicResponse>(this._templateName, new
                    {
                        template = this._templateMatchString,
                        settings = settings,
                        mappings = new
                        {
                            _default_ = new
                            {
                                _all = new { enabled = true, omit_norms = true },
                                dynamic_templates = new List<Object>
                            {
                                //when you use serilog as an adaptor for third party frameworks
                                //where you have no control over the log message they typically
                                //contain {0} ad infinitum, we force numeric property names to
                                //contain strings by default.
                                { new { numerics_in_fields = new
                                {
                                    path_match = @"fields\.[\d+]$",
                                    match_pattern = "regex",
                                    mapping = new
                                    {
                                        type = "string", index = "analyzed", omit_norms = true
                                    }
                                }}},
                                {
                                    new { string_fields = new
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
                                    }}
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

                    if (!result.Success)
                    {
                        SelfLog.WriteLine("Unable to create the template. {0}", result.ServerError);
                    }
                }
            }
            catch (Exception ex)
            {
                SelfLog.WriteLine("Failed to create the template. {0}", ex);
            }
        }
    }
}
