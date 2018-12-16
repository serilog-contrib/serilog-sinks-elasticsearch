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
using System.Text;
using Serilog.Core;
using Serilog.Events;


namespace Serilog.Sinks.Elasticsearch.Durable
{
    class DurableElasticsearchSink : ILogEventSink, IDisposable
    {
        // we rely on the date in the filename later!
        const string FileNameSuffix = "-{Date}.json";

        readonly Logger _sink;
        readonly ElasticsearchLogShipper _shipper;
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
                    rollingInterval: RollingInterval.Day,
                    fileSizeLimitBytes: options.BufferFileSizeLimitBytes,
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: options.BufferFileCountLimit,
                    encoding: Encoding.UTF8)
                .CreateLogger();

          

            _shipper = new ElasticsearchLogShipper(_state);
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