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
using System.Runtime.InteropServices;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog.Sinks.Elasticsearch
{
    using global::Elasticsearch.Net;

    /// <summary>
    /// Writes log events as documents to ElasticSearch.
    /// </summary>
    public class ElasticsearchSink : PeriodicBatchingSink
    {

        private readonly ElasticsearchSinkState _state;

        /// <summary>
        /// Creates a new ElasticsearchSink instance with the provided options
        /// </summary>
        /// <param name="options">Options configuring how the sink behaves, may NOT be null</param>
        public ElasticsearchSink(ElasticsearchSinkOptions options)
            : base(options.BatchPostingLimit, options.Period)
        {
            _state = ElasticsearchSinkState.Create(options);
            _state.RegisterTemplateIfNeeded();
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
            var eventsMaterialized = events.ToArray();

            var result = EmitBatchChecked(eventsMaterialized);

            // Handle the results from ES, check if there are any errors.
            if (!result.Success)
            {
                if (_state.Options.EmitEventFailure.HasFlag(EmitEventFailureHandling.WriteToSelfLog))
                {
                    // ES reports an error, output the error to the selflog
                    SelfLog.WriteLine("Failed to store batch of {0} events into Elasticsearch. Elasticsearch reports the following: {1}",
                        eventsMaterialized.Length,
                        result.ServerError?.Error?.Reason);
                }

                if (_state.Options.EmitEventFailure.HasFlag(EmitEventFailureHandling.WriteToFailureSink) && _state.Options.FailureSink != null)
                {
                    foreach (LogEvent e in eventsMaterialized)
                    {
                        // Send to a failure sink
                        try
                        {
                            _state.Options.FailureSink.Emit(e);
                        }
                        catch (Exception ex)
                        {
                            // We do not let this fail too
                            SelfLog.WriteLine("Caught exception {0} while emitting to failure sink {1}.", ex, _state.Options.FailureSink);
                        }
                    }
                }
            }
            else if (result.Body["errors"] == true)
            {
                dynamic body = result.Body;

                var i = 0;
                var items = body.items;
                foreach (var item in items)
                {
                    if (item.index != null && item.index.status != 200)
                    {
                        var e = eventsMaterialized[i];

                        if (_state.Options.EmitEventFailure.HasFlag(EmitEventFailureHandling.WriteToSelfLog))
                        {
                            // ES reports an error, output the error to the selflog
                            SelfLog.WriteLine("Failed to store event with template '{0}' into Elasticsearch. Elasticsearch reports for index {1} the following: {2}",
                                e.MessageTemplate,
                                item.index._index,
                                item.index.error);
                        }

                        if (_state.Options.EmitEventFailure.HasFlag(EmitEventFailureHandling.WriteToFailureSink) && _state.Options.FailureSink != null)
                        {
                            // Send to a failure sink
                            try
                            {
                                _state.Options.FailureSink.Emit(e);
                            }
                            catch (Exception ex)
                            {
                                // We do not let this fail too
                                SelfLog.WriteLine("Caught exception {0} while emitting to failure sink {1}.", ex, _state.Options.FailureSink);
                            }
                        }
                    }

                    i++;
                }


            }
        }

        /// <summary>
        /// Emit a batch of log events, running to completion synchronously.
        /// </summary>
        /// <param name="events">The events to emit.</param>
        /// <returns>Response from Elasticsearch</returns>
        protected virtual ElasticsearchResponse<DynamicResponse> EmitBatchChecked(IEnumerable<LogEvent> events)
        {
            // ReSharper disable PossibleMultipleEnumeration
            if (events == null)
                return null;

            var payload = new List<string>();
            foreach (var e in events)
            {
                var indexName = _state.GetIndexForEvent(e, e.Timestamp.ToUniversalTime());
                payload.Add($"{{\"index\":{{\"_index\":\"{indexName}\",\"_type\":\"{_state.Options.TypeName}\"}}}}");
                var sw = new StringWriter();
                _state.Formatter.Format(e, sw);
                payload.Add(sw.ToString());
            }

            if (payload.Count == 0)
                return null;

            return _state.Client.Bulk<DynamicResponse>(payload);
        }
    }
}
