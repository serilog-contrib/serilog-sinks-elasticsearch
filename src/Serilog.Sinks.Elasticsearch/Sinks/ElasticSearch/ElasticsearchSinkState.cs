using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Elasticsearch.Net;
using Elasticsearch.Net.Connection;
using Elasticsearch.Net.Serialization;
using Serilog.Events;
using Serilog.Formatting;

namespace Serilog.Sinks.Elasticsearch
{
	internal class ElasticsearchSinkState
	{
		public static ElasticsearchSinkState Create(ElasticsearchSinkOptions options)
		{
            if (options == null) throw new ArgumentNullException("options");
			var state = new ElasticsearchSinkState(options);
			if (state.Options.AutoRegisterTemplate)
				state.RegisterTemplateIfNeeded();
			return state;
		}

		private readonly ElasticsearchSinkOptions _options;
        readonly Func<LogEvent, DateTimeOffset, string> _indexDecider;

        private readonly ITextFormatter _formatter;
        private readonly ElasticsearchClient _client;

        readonly string _typeName;
        private readonly bool _registerTemplateOnStartup;
        private readonly string _templateName;
        private readonly string _templateMatchString;
        private static readonly Regex IndexFormatRegex = new Regex(@"^(.*)(?:\{0\:.+\})(.*)$");

		public ElasticsearchSinkOptions Options { get { return this._options; }}
		public IElasticsearchClient Client { get { return this._client; }}
		public ITextFormatter Formatter { get { return this._formatter; }}


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

            var configuration = new ConnectionConfiguration(options.ConnectionPool)
                .SetTimeout(options.ConnectionTimeout)
                .SetMaximumAsyncConnections(20);

            if (options.ModifyConnectionSetttings != null)
                configuration = options.ModifyConnectionSetttings(configuration);

            _client = new ElasticsearchClient(configuration, connection: options.Connection, serializer: options.Serializer);
            _formatter = options.CustomFormatter ?? new ElasticsearchJsonFormatter(
                formatProvider: options.FormatProvider,
                renderMessage: true,
                closingDelimiter: string.Empty,
                serializer: options.Serializer,
                inlineFields: options.InlineFields
            );

			this._registerTemplateOnStartup = options.AutoRegisterTemplate;
		}


		public string Serialize(object o)
		{
			var bytes = _client.Serializer.Serialize(o, SerializationFormatting.None);
			return Encoding.UTF8.GetString(bytes);
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

	}
}
