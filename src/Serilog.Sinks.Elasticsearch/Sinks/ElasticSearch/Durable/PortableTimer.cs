// Copyright 2013-2016 Serilog Contributors
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

using Serilog.Debugging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Serilog.Sinks.Elasticsearch.Durable
{
    /// <summary>
    /// https://github.com/serilog/serilog-sinks-seq/blob/v4.0.0/src/Serilog.Sinks.Seq/Sinks/Seq/PortableTimer.cs
    /// </summary>
    class PortableTimer : IDisposable
    {
        readonly object _stateLock = new object();

        readonly Func<CancellationToken, Task> _onTick;
        readonly CancellationTokenSource _cancel = new CancellationTokenSource();

#if THREADING_TIMER
        readonly Timer _timer;
#endif

        bool _running;
        bool _disposed;

        public PortableTimer(Func<CancellationToken, Task> onTick)
        {
            if (onTick == null) throw new ArgumentNullException(nameof(onTick));

            _onTick = onTick;

#if THREADING_TIMER
            _timer = new Timer(_ => OnTick(), null, Timeout.Infinite, Timeout.Infinite);
#endif
        }

        public void Start(TimeSpan interval)
        {
            if (interval < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(interval));

            lock (_stateLock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(PortableTimer));

#if THREADING_TIMER
                _timer.Change(interval, Timeout.InfiniteTimeSpan);
#else
                Task.Delay(interval, _cancel.Token)
                    .ContinueWith(
                        _ => OnTick(),
                        CancellationToken.None,
                        TaskContinuationOptions.DenyChildAttach,
                        TaskScheduler.Default);
#endif
            }
        }

        async void OnTick()
        {
            try
            {
                lock (_stateLock)
                {
                    if (_disposed)
                    {
                        return;
                    }

                    // There's a little bit of raciness here, but it's needed to support the
                    // current API, which allows the tick handler to reenter and set the next interval.

                    if (_running)
                    {
                        Monitor.Wait(_stateLock);

                        if (_disposed)
                        {
                            return;
                        }
                    }

                    _running = true;
                }

                if (!_cancel.Token.IsCancellationRequested)
                {
                    await _onTick(_cancel.Token);
                }
            }
            catch (OperationCanceledException tcx)
            {
                SelfLog.WriteLine("The timer was canceled during invocation: {0}", tcx);
            }
            finally
            {
                lock (_stateLock)
                {
                    _running = false;
                    Monitor.PulseAll(_stateLock);
                }
            }
        }

        public void Dispose()
        {
            _cancel.Cancel();

            lock (_stateLock)
            {
                if (_disposed)
                {
                    return;
                }

                while (_running)
                {
                    Monitor.Wait(_stateLock);
                }

#if THREADING_TIMER
                _timer.Dispose();
#endif

                _disposed = true;
            }
        }
    }
}
