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

namespace Serilog.Sinks.Elasticsearch.Durable
{
    /// <summary>
    /// https://github.com/serilog/serilog-sinks-seq/blob/v4.0.0/src/Serilog.Sinks.Seq/Sinks/Seq/Durable/BookmarkFile.cs
    /// </summary>
    sealed class BookmarkFile : IDisposable
    {
        readonly FileStream _bookmark;

        public BookmarkFile(string bookmarkFilename)
        {
            _bookmark = System.IO.File.Open(bookmarkFilename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
        }

        public FileSetPosition TryReadBookmark()
        {
            if (_bookmark.Length != 0)
            {
                _bookmark.Position = 0;

                // Important not to dispose this StreamReader as the stream must remain open.
                var reader = new StreamReader(_bookmark, Encoding.UTF8, false, 128);
                var current = reader.ReadLine();

                if (current != null)
                {
                    var parts = current.Split(new[] { ":::" }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        return new FileSetPosition(long.Parse(parts[0]), parts[1]);
                    }
                }
            }

            return FileSetPosition.None;
        }

        public void WriteBookmark(FileSetPosition bookmark)
        {
            if (bookmark.File == null)
                return;

            // Don't need to truncate, since we only ever read a single line and
            // writes are always newline-terminated
            _bookmark.Position = 0;

            // Cannot dispose, as `leaveOpen` is not available on all target platforms
            var writer = new StreamWriter(_bookmark);
            writer.WriteLine("{0}:::{1}", bookmark.NextLineStart, bookmark.File);
            writer.Flush();
        }

        public void Dispose()
        {
            _bookmark.Dispose();
        }
    }
}

#endif
