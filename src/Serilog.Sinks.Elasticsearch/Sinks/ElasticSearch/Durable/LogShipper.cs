// Serilog.Sinks.Seq Copyright 2017 Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#if DURABLE

using System;
using System.IO;
using System.Linq;
using System.Text;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using IOFile = System.IO.File;
using System.Threading.Tasks;
#if HRESULTS
using System.Runtime.InteropServices;
#endif

namespace Serilog.Sinks.Elasticsearch.Durable
{
    /// <summary>
    /// Reads and sends logdata to log server
    /// Generic version of  https://github.com/serilog/serilog-sinks-seq/blob/v4.0.0/src/Serilog.Sinks.Seq/Sinks/Seq/Durable/HttpLogShipper.cs
    /// </summary>
    /// <typeparam name="TPayload"></typeparam>
    public class LogShipper<TPayload> : IDisposable
    {
        private static readonly TimeSpan RequiredLevelCheckInterval = TimeSpan.FromMinutes(2);
        
        readonly int _batchPostingLimit;
        readonly long? _eventBodyLimitBytes;
        private readonly ILogClient<TPayload> _logClient;
        private readonly IPayloadReader<TPayload> _payloadReader;
        readonly FileSet _fileSet;
        readonly long? _retainedInvalidPayloadsLimitBytes;
        readonly long? _bufferSizeLimitBytes;

        // Timer thread only

        /// <summary>
        /// 
        /// </summary>
        protected readonly ExponentialBackoffConnectionSchedule _connectionSchedule;
        DateTime _nextRequiredLevelCheckUtc = DateTime.UtcNow.Add(RequiredLevelCheckInterval);

        // Synchronized
        readonly object _stateLock = new object();

        readonly PortableTimer _timer;

        // Concurrent
        readonly ControlledLevelSwitch _controlledSwitch;

        volatile bool _unloading;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bufferBaseFilename"></param>
        /// <param name="batchPostingLimit"></param>
        /// <param name="period"></param>
        /// <param name="eventBodyLimitBytes"></param>
        /// <param name="levelControlSwitch"></param>
        /// <param name="logClient"></param>
        /// <param name="payloadReader"></param>
        /// <param name="retainedInvalidPayloadsLimitBytes"></param>
        /// <param name="bufferSizeLimitBytes"></param>
        /// <param name="rollingInterval"></param>
        public LogShipper(            
            string bufferBaseFilename,            
            int batchPostingLimit,
            TimeSpan period,
            long? eventBodyLimitBytes,
            LoggingLevelSwitch levelControlSwitch,
            ILogClient<TPayload> logClient,
            IPayloadReader<TPayload> payloadReader,
            long? retainedInvalidPayloadsLimitBytes,
            long? bufferSizeLimitBytes,
            RollingInterval rollingInterval = RollingInterval.Day)
        {
            _batchPostingLimit = batchPostingLimit;
            _eventBodyLimitBytes = eventBodyLimitBytes;
            _logClient = logClient;
            _payloadReader = payloadReader;
            _controlledSwitch = new ControlledLevelSwitch(levelControlSwitch);
            _connectionSchedule = new ExponentialBackoffConnectionSchedule(period);
            _retainedInvalidPayloadsLimitBytes = retainedInvalidPayloadsLimitBytes;
            _bufferSizeLimitBytes = bufferSizeLimitBytes;
            _fileSet = new FileSet(bufferBaseFilename, rollingInterval);
            _timer = new PortableTimer(c => OnTick());
            SetTimer();

        }

        void CloseAndFlush()
        {
            lock (_stateLock)
            {
                if (_unloading)
                    return;

                _unloading = true;
            }

            _timer.Dispose();

            OnTick().GetAwaiter().GetResult();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logEvent"></param>
        /// <returns></returns>
        public bool IsIncluded(LogEvent logEvent)
        {
            return _controlledSwitch.IsIncluded(logEvent);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            CloseAndFlush();
        }

        protected void SetTimer()
        {
            // Note, called under _stateLock
            _timer.Start(_connectionSchedule.NextInterval);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected virtual async Task OnTick()
        {
            try
            {
                int count;
                do
                {
                    count = 0;

                    using (var bookmarkFile = _fileSet.OpenBookmarkFile())
                    {
                        var position = bookmarkFile.TryReadBookmark();
                        var files = _fileSet.GetBufferFiles();

                        if (position.File == null || !IOFile.Exists(position.File))
                        {
                            position = new FileSetPosition(0, files.FirstOrDefault());
                        }

                        TPayload payload;
                        if (position.File == null)
                        {
                            payload = _payloadReader.GetNoPayload();
                            count = 0;
                        }
                        else
                        {
                            payload = _payloadReader.ReadPayload(_batchPostingLimit, _eventBodyLimitBytes, ref position, ref count,position.File);
                        }

                        if (count > 0 || _controlledSwitch.IsActive && _nextRequiredLevelCheckUtc < DateTime.UtcNow)
                        {
                            _nextRequiredLevelCheckUtc = DateTime.UtcNow.Add(RequiredLevelCheckInterval);

                            var result = await _logClient.SendPayloadAsync(payload).ConfigureAwait(false);
                            if (result.Success)
                            {
                                _connectionSchedule.MarkSuccess();
                                if(result.InvalidResult!=null)
                                     DumpInvalidPayload(result.InvalidResult.StatusCode, result.InvalidResult.Content, result.InvalidResult.BadPayLoad);
                                bookmarkFile.WriteBookmark(position);
                            }                           
                            else
                            {
                                _connectionSchedule.MarkFailure();                               
                                if (_bufferSizeLimitBytes.HasValue)
                                    _fileSet.CleanUpBufferFiles(_bufferSizeLimitBytes.Value, 2);

                                break;
                            }
                        }
                        else if (position.File == null)
                        {
                            break;
                        }
                        else
                        {
                            // For whatever reason, there's nothing waiting to send. This means we should try connecting again at the
                            // regular interval, so mark the attempt as successful.
                            _connectionSchedule.MarkSuccess();

                            // Only advance the bookmark if no other process has the
                            // current file locked, and its length is as we found it.
                            if (files.Length == 2 && files.First() == position.File &&
                                FileIsUnlockedAndUnextended(position))
                            {
                                bookmarkFile.WriteBookmark(new FileSetPosition(0, files[1]));
                            }

                            if (files.Length > 2)
                            {
                                // By this point, we expect writers to have relinquished locks
                                // on the oldest file.
                                IOFile.Delete(files[0]);
                            }
                        }
                    }
                } while (count == _batchPostingLimit);
            }
            catch (Exception ex)
            {
                _connectionSchedule.MarkFailure();
                SelfLog.WriteLine("Exception while emitting periodic batch from {0}: {1}", this, ex);

                if (_bufferSizeLimitBytes.HasValue)
                    _fileSet.CleanUpBufferFiles(_bufferSizeLimitBytes.Value, 2);
            }
            finally
            {
                lock (_stateLock)
                {
                    if (!_unloading)
                        SetTimer();
                }
            }
        }

       

        void  DumpInvalidPayload(int statusCode,string resultContent, string payload)
        {
            var invalidPayloadFile = _fileSet.MakeInvalidPayloadFilename(statusCode);            
            SelfLog.WriteLine("HTTP shipping failed with {0}: {1}; dumping payload to {2}", statusCode,
                resultContent, invalidPayloadFile);
            var bytesToWrite = Encoding.UTF8.GetBytes(payload);
            if (_retainedInvalidPayloadsLimitBytes.HasValue)
            {
                _fileSet.CleanUpInvalidPayloadFiles(_retainedInvalidPayloadsLimitBytes.Value - bytesToWrite.Length);
            }
            IOFile.WriteAllBytes(invalidPayloadFile, bytesToWrite);
        }

        static bool FileIsUnlockedAndUnextended(FileSetPosition position)
        {
            try
            {
                using (var fileStream = IOFile.Open(position.File, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
                {
                    return fileStream.Length <= position.NextLineStart;
                }
            }
#if HRESULTS
            catch (IOException ex)
            {
                var errorCode = Marshal.GetHRForException(ex) & ((1 << 16) - 1);
                if (errorCode != 32 && errorCode != 33)
                {
                    SelfLog.WriteLine("Unexpected I/O exception while testing locked status of {0}: {1}", position.File, ex);
                }
            }
#else
            catch (IOException)
            {
                // Where no HRESULT is available, assume IOExceptions indicate a locked file
            }
#endif
            catch (Exception ex)
            {
                SelfLog.WriteLine("Unexpected exception while testing locked status of {0}: {1}", position.File, ex);
            }

            return false;
        }
    }
}

#endif
