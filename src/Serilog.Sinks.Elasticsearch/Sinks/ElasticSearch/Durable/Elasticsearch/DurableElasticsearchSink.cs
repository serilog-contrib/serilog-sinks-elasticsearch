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
using Serilog.Core;
using Serilog.Events;


namespace Serilog.Sinks.Elasticsearch.Durable
{
    class DurableElasticsearchSink : ILogEventSink, IDisposable
    {
        // we rely on the date in the filename later!
        const string FileNameSuffix = "-.json";

        readonly Logger _sink;
        readonly LogShipper<List<string>> _shipper;
        readonly ElasticsearchSinkState _state;

        public DurableElasticsearchSink(ElasticsearchSinkOptions options)
        {
            _state = ElasticsearchSinkState.Create(options);

            if (string.IsNullOrWhiteSpace(options.BufferBaseFilename))
            {
                throw new ArgumentException("Cannot create the durable ElasticSearch sink without a buffer base file name!");
            }

            _sink = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.File(_state.DurableFormatter,
                    options.BufferBaseFilename + FileNameSuffix,
                    rollingInterval: options.BufferFileRollingInterval,
                    fileSizeLimitBytes: options.BufferFileSizeLimitBytes,
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: options.BufferFileCountLimit,
                    levelSwitch: _state.Options.LevelSwitch,
                    encoding: Encoding.UTF8)
                .CreateLogger();
            
            var elasticSearchLogClient = new ElasticsearchLogClient(
                elasticLowLevelClient: _state.Client, 
                cleanPayload: _state.Options.BufferCleanPayload,
                elasticOpType: _state.Options.BatchAction);

            var payloadReader = new ElasticsearchPayloadReader(
                 pipelineName: _state.Options.PipelineName,  
                 typeName:_state.Options.TypeName, 
                 serialize:_state.Serialize,  
                 getIndexForEvent: _state.GetBufferedIndexForEvent,
                 elasticOpType: _state.Options.BatchAction,
                 rollingInterval: options.BufferFileRollingInterval);

            _shipper = new ElasticsearchLogShipper(
                bufferBaseFilename: _state.Options.BufferBaseFilename,
                batchPostingLimit: _state.Options.BatchPostingLimit,
                period: _state.Options.BufferLogShippingInterval ?? TimeSpan.FromSeconds(5),
                eventBodyLimitBytes: _state.Options.SingleEventSizePostingLimit,
                levelControlSwitch: _state.Options.LevelSwitch,
                logClient: elasticSearchLogClient,
                payloadReader: payloadReader,
                retainedInvalidPayloadsLimitBytes: _state.Options.BufferRetainedInvalidPayloadsLimitBytes,
                bufferSizeLimitBytes: _state.Options.BufferFileSizeLimitBytes,
                registerTemplateIfNeeded: _state.RegisterTemplateIfNeeded,
                rollingInterval: options.BufferFileRollingInterval);
                
        }

        public void Emit(LogEvent logEvent)
        {
            // This is a lagging indicator, but the network bandwidth usage benefits
            // are worth the ambiguity.
            if (_shipper.IsIncluded(logEvent))
            {
                _sink.Write(logEvent);
            }
        }

        public void Dispose()
        {
            _sink.Dispose();
            _shipper.Dispose();
        }
    }
}