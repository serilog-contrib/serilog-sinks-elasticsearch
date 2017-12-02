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
using Elasticsearch.Net;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;

namespace Serilog.Sinks.Elasticsearch
{
    /// <summary>
    /// Provides ElasticsearchSink with configurable options
    /// </summary>
    public class ElasticsearchSinkOptions
    {
        private int _queueSizeLimit;

        /// <summary>
        /// When set to true the sink will register an index template for the logs in elasticsearch.
        /// This template is optimized to deal with serilog events
        /// </summary>
        public bool AutoRegisterTemplate { get; set; }

        /// <summary>
        /// Specifies the option on how to handle failures when writing the template to Elasticsearch. This is only applicable when using the AutoRegisterTemplate option.
        /// </summary>
        public RegisterTemplateRecovery RegisterTemplateFailure { get; set; }

        ///<summary>
        /// When using the <see cref="AutoRegisterTemplate"/> feature this allows you to override the default template name.
        /// Defaults to: serilog-events-template
        /// </summary>
        public string TemplateName { get; set; }

        /// <summary>
        /// When using the <see cref="AutoRegisterTemplate"/> feature, this allows you to override the default template content.
        /// If not provided, a default template that is optimized to deal with Serilog events is used.
        /// </summary>
        public Func<object> GetTemplateContent { get; set; }

        /// <summary>
        /// When using the <see cref="AutoRegisterTemplate"/> feature, this allows you to overwrite the template in Elasticsearch if it already exists.
        /// Defaults to: false
        /// </summary>
        public bool OverwriteTemplate { get; set; }

        /// <summary>
        /// When using the <see cref="AutoRegisterTemplate"/> feature, this allows you to override the default number of shards.
        /// If not provided, this will default to the default number_of_shards configured in Elasticsearch.
        /// </summary>
        public int? NumberOfShards { get; set; }

        /// <summary>
        /// When using the <see cref="AutoRegisterTemplate"/> feature, this allows you to override the default number of replicas.
        /// If not provided, this will default to the default number_of_replicas configured in Elasticsearch.
        /// </summary>
        public int? NumberOfReplicas { get; set; }

        ///<summary>
        /// Connection configuration to use for connecting to the cluster.
        /// </summary>
        public Func<ConnectionConfiguration, ConnectionConfiguration> ModifyConnectionSettings { get; set; }

        ///<summary>
        /// The index name formatter. A string.Format using the DateTimeOffset of the event is run over this string.
        /// defaults to "logstash-{0:yyyy.MM.dd}"
        /// </summary>
        public string IndexFormat { get; set; }

        /// <summary>
        /// Optionally set this value to the name of the index that should be used when the template cannot be written to ES.
        /// defaults to "deadletter-{0:yyyy.MM.dd}"
        /// </summary>
        public string DeadLetterIndexName { get; set; }

        ///<summary>
        /// The default elasticsearch type name to use for the log events. Defaults to: logevent.
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        /// Name the Pipeline where log events are sent to sink. Please note that the Pipeline should be existing before the usage starts.
        /// </summary>
        public string PipelineName { get; set; }

        ///<summary>
        /// The maximum number of events to post in a single batch. Defaults to: 50.
        /// </summary>
        public int BatchPostingLimit { get; set; }

        ///<summary>
        /// The time to wait between checking for event batches. Defaults to 2 seconds.
        /// </summary>
        public TimeSpan Period { get; set; }

        ///<summary>
        /// Supplies culture-specific formatting information, or null.
        /// </summary>
        public IFormatProvider FormatProvider { get; set; }

        ///<summary>
        /// Allows you to override the connection used to communicate with elasticsearch.
        /// </summary>
        public IConnection Connection { get; set; }

        /// <summary>
        /// The connection timeout (in milliseconds) when sending bulk operations to elasticsearch (defaults to 5000).
        /// </summary>
        public TimeSpan ConnectionTimeout { get; set; }

        /// <summary>
        /// When true fields will be written at the root of the json document.
        /// </summary>
        public bool InlineFields { get; set; }

        /// <summary>
        /// The minimum log event level required in order to write an event to the sink.
        /// </summary>
        public LogEventLevel? MinimumLogEventLevel { get; set; }

        ///<summary>
        /// When passing a serializer unknown object will be serialized to object instead of relying on their ToString representation
        /// </summary>
        public IElasticsearchSerializer Serializer { get; set; }

        /// <summary>
        /// The connectionpool describing the cluster to write event to
        /// </summary>
        public IConnectionPool ConnectionPool { get; private set; }

        /// <summary>
        /// Function to decide which index to write the LogEvent to
        /// </summary>
        public Func<LogEvent, DateTimeOffset, string> IndexDecider { get; set; }

        /// <summary>
        /// Optional path to directory that can be used as a log shipping buffer for increasing the reliability of the log forwarding.
        /// </summary>
        public string BufferBaseFilename { get; set; }

        /// <summary>
        /// The maximum size, in bytes, to which the buffer log file for a specific date will be allowed to grow. By default no limit will be applied.
        /// </summary>
        public long? BufferFileSizeLimitBytes { get; set; }

        /// <summary>
        /// The interval between checking the buffer files.
        /// </summary>
        public TimeSpan? BufferLogShippingInterval { get; set; }

        /// <summary>
        /// Customizes the formatter used when converting log events into ElasticSearch documents. Please note that the formatter output must be valid JSON :)
        /// </summary>
        public ITextFormatter CustomFormatter { get; set; }

        /// <summary>
        /// Customizes the formatter used when converting log events into the durable sink. Please note that the formatter output must be valid JSON :)
        /// </summary>
        public ITextFormatter CustomDurableFormatter { get; set; }

        /// <summary>
        /// Specifies how failing emits should be handled. 
        /// </summary>
        public EmitEventFailureHandling EmitEventFailure { get; set; }

        /// <summary>
        /// Sink to use when Elasticsearch is unable to accept the events. This is optionally and depends on the EmitEventFailure setting.
        /// </summary>
        public ILogEventSink FailureSink { get; set; }

        /// <summary>
        /// A callback which can be used to handle logevents which are not submitted to Elasticsearch
        /// like when it is unable to accept the events. This is optionally and depends on the EmitEventFailure setting.
        /// </summary>
        public Action<LogEvent> FailureCallback { get; set; }

        /// <summary>
        /// The maximum number of events that will be held in-memory while waiting to ship them to
        /// Elasticsearch. Beyond this limit, events will be dropped. The default is 100,000. Has no effect on
        /// durable log shipping.
        /// </summary>
        public int QueueSizeLimit
        {
            get { return _queueSizeLimit; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(QueueSizeLimit), "Queue size limit must be non-zero.");
                _queueSizeLimit = value;
            }
        }

        /// <summary>
        /// Configures the elasticsearch sink defaults
        /// </summary>
        protected ElasticsearchSinkOptions()
        {
            this.IndexFormat = "logstash-{0:yyyy.MM.dd}";
            this.DeadLetterIndexName = "deadletter-{0:yyyy.MM.dd}";
            this.TypeName = "logevent";
            this.Period = TimeSpan.FromSeconds(2);
            this.BatchPostingLimit = 50;
            this.TemplateName = "serilog-events-template";
            this.ConnectionTimeout = TimeSpan.FromSeconds(5);
            this.EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog;
            this.RegisterTemplateFailure = RegisterTemplateRecovery.IndexAnyway;
            this.QueueSizeLimit = 100000;
        }

        /// <summary>
        /// Configures the elasticsearch sink
        /// </summary>
        /// <param name="connectionPool">The connectionpool to use to write events to</param>
        public ElasticsearchSinkOptions(IConnectionPool connectionPool)
            : this()
        {
            ConnectionPool = connectionPool;
        }

        /// <summary>
        /// Configures the elasticsearch sink
        /// </summary>
        /// <param name="nodes">The nodes to write to</param>
        public ElasticsearchSinkOptions(IEnumerable<Uri> nodes)
            : this()
        {
            nodes = nodes != null && nodes.Any(n => n != null)
                ? nodes.Where(n => n != null)
                : new[] { new Uri("http://localhost:9200") };
            if (nodes.Count() == 1)
                ConnectionPool = new SingleNodeConnectionPool(nodes.First());
            else
                ConnectionPool = new StaticConnectionPool(nodes);
        }

        /// <summary>
        /// Configures the elasticsearch sink
        /// </summary>
        /// <param name="node">The node to write to</param>
        public ElasticsearchSinkOptions(Uri node) : this(new[] { node }) { }
    }

    /// <summary>
    /// Sepecifies options for handling failures when emitting the events to Elasticsearch. Can be a combination of options.
    /// </summary>
    [Flags]
    public enum EmitEventFailureHandling
    {
        /// <summary>
        /// Send the error to the SelfLog
        /// </summary>
        WriteToSelfLog = 1,

        /// <summary>
        /// Write the events to another sink. Make sure to configure this one.
        /// </summary>
        WriteToFailureSink = 2,

        /// <summary>
        /// Throw the exception to the caller.
        /// </summary>
        ThrowException = 4,

        /// <summary>
        /// The failure callback function will be called when the event cannot be submitted to Elasticsearch.
        /// </summary>
        RaiseCallback = 8
    }

    /// <summary>
    /// Specifies what to do when the template could not be created. This can mean that your data is not correctly indexed, so you might want to handle this failure.
    /// </summary>
    public enum RegisterTemplateRecovery
    {
        /// <summary>
        /// Ignore the issue and keep indexing. This is the default option.
        /// </summary>
        IndexAnyway = 1,

        ///// <summary>
        ///// Keep buffering the data until it is written. be aware you might hit a limit here. 
        ///// </summary>
        //BufferUntilSuccess = 2,

        /// <summary>
        /// When the template cannot be registered, move the events to the deadletter index instead.
        /// </summary>
        IndexToDeadletterIndex = 4,

        /// <summary>
        /// When the template cannot be registered, throw an exception and fail the sink.
        /// </summary>
        FailSink = 8
    }
}