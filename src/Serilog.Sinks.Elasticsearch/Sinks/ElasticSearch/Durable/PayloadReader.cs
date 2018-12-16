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
using System.Text;
using Serilog.Debugging;

namespace Serilog.Sinks.Elasticsearch.Durable
{
    static class PayloadReader
    {
        public const string NoPayload = "{\"Events\":[]}";

        public static string ReadPayload(int batchPostingLimit, long? eventBodyLimitBytes, ref FileSetPosition position, ref int count)
        {
            var payload = new StringWriter();
            payload.Write("{\"Events\":[");
            var delimStart = "";

            using (var current = System.IO.File.Open(position.File, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var nextLineStart = position.NextLineStart;
                while (count < batchPostingLimit && TryReadLine(current, ref nextLineStart, out var nextLine))
                {
                    position = new FileSetPosition(nextLineStart, position.File);

                    // Count is the indicator that work was done, so advances even in the (rare) case an
                    // oversized event is dropped.
                    ++count;

                    if (eventBodyLimitBytes.HasValue && Encoding.UTF8.GetByteCount(nextLine) > eventBodyLimitBytes.Value)
                    {
                        SelfLog.WriteLine(
                            "Event JSON representation exceeds the byte size limit of {0} and will be dropped; data: {1}",
                            eventBodyLimitBytes, nextLine);
                    }
                    else
                    {
                        payload.Write(delimStart);
                        payload.Write(nextLine);
                        delimStart = ",";
                    }
                }

                payload.Write("]}");
            }
            return payload.ToString();
        }

        // It would be ideal to chomp whitespace here, but not required.
        static bool TryReadLine(Stream current, ref long nextStart, out string nextLine)
        {
            var includesBom = nextStart == 0;

            if (current.Length <= nextStart)
            {
                nextLine = null;
                return false;
            }

            current.Position = nextStart;

            // Important not to dispose this StreamReader as the stream must remain open.
            var reader = new StreamReader(current, Encoding.UTF8, false, 128);
            nextLine = reader.ReadLine();

            if (nextLine == null)
                return false;

            nextStart += Encoding.UTF8.GetByteCount(nextLine) + Encoding.UTF8.GetByteCount(Environment.NewLine);
            if (includesBom)
                nextStart += 3;

            return true;
        }
    }
}

#endif
