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

namespace Serilog.Sinks.Elasticsearch.Durable
{
    /// <summary>
    /// https://github.com/serilog/serilog-sinks-seq/blob/v4.0.0/src/Serilog.Sinks.Seq/Sinks/Seq/Durable/FileSetPosition.cs
    /// </summary>
    public struct FileSetPosition
    {
        public string File { get; }

        public long NextLineStart { get; }

        public FileSetPosition(long nextLineStart, string file)
        {
            NextLineStart = nextLineStart;
            File = file;
        }

        public static readonly FileSetPosition None = default(FileSetPosition);
    }
}

#endif
