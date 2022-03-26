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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Serilog.Debugging;
[assembly:
    InternalsVisibleTo(
        "Serilog.Sinks.Elasticsearch.Tests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100fb8d13fd344a1c6fe0fe83ef33c1080bf30690765bc6eb0df26ebfdf8f21670c64265b30db09f73a0dea5b3db4c9d18dbf6d5a25af5ce9016f281014d79dc3b4201ac646c451830fc7e61a2dfd633d34c39f87b81894191652df5ac63cc40c77f3542f702bda692e6e8a9158353df189007a49da0f3cfd55eb250066b19485ec")]
namespace Serilog.Sinks.Elasticsearch.Durable
{
    /// <summary>
    /// https://github.com/serilog/serilog-sinks-seq/blob/v4.0.0/src/Serilog.Sinks.Seq/Sinks/Seq/Durable/FileSet.cs
    /// </summary>
    class FileSet
    {
        readonly string _bookmarkFilename;
        readonly string _candidateSearchPath;
        readonly string _logFolder;
        readonly Regex _filenameMatcher;

        const string InvalidPayloadFilePrefix = "invalid-";

        public FileSet(string bufferBaseFilename, RollingInterval rollingInterval)
        {
            if (bufferBaseFilename == null) throw new ArgumentNullException(nameof(bufferBaseFilename));

            _bookmarkFilename = Path.GetFullPath(bufferBaseFilename + ".bookmark");
            _logFolder = Path.GetDirectoryName(_bookmarkFilename);
            _candidateSearchPath = Path.GetFileName(bufferBaseFilename) + "-*.json";
            var dateRegularExpressionPart = rollingInterval.GetMatchingDateRegularExpressionPart();
            _filenameMatcher = new Regex("^" + Regex.Escape(Path.GetFileName(bufferBaseFilename)) + "-(?<date>" 
                                         + dateRegularExpressionPart + ")(?<sequence>_[0-9]{3,}){0,1}\\.json$");
        }

        public BookmarkFile OpenBookmarkFile()
        {
            return new BookmarkFile(_bookmarkFilename);
        }

        public string[] GetBufferFiles()
        {
            return Directory.GetFiles(_logFolder, _candidateSearchPath)
                .Select(n => new KeyValuePair<string, Match>(n, _filenameMatcher.Match(Path.GetFileName(n))))
                .Where(nm => nm.Value.Success)
                .OrderBy(nm => nm.Value.Groups["date"].Value, StringComparer.OrdinalIgnoreCase)
                .ThenBy(nm => int.Parse("0" + nm.Value.Groups["sequence"].Value.Replace("_", "")))
                .Select(nm => nm.Key)
                .ToArray();
        }

        public void CleanUpBufferFiles(long bufferSizeLimitBytes, int alwaysRetainCount)
        {
            try
            {
                var bufferFiles = GetBufferFiles();
                Array.Reverse(bufferFiles);
                DeleteExceedingCumulativeSize(bufferFiles.Select(f => new FileInfo(f)), bufferSizeLimitBytes, 2);
            }
            catch (Exception ex)
            {
                SelfLog.WriteLine("Exception thrown while cleaning up buffer files: {0}", ex);
            }
        }

        public string MakeInvalidPayloadFilename(int statusCode)
        {
            var invalidPayloadFilename = $"{InvalidPayloadFilePrefix}{statusCode}-{Guid.NewGuid():n}.json";
            return Path.Combine(_logFolder, invalidPayloadFilename);
        }

        public void CleanUpInvalidPayloadFiles(long maxNumberOfBytesToRetain)
        {
            try
            {
                var candidateFiles = from file in Directory.EnumerateFiles(_logFolder, $"{InvalidPayloadFilePrefix}*.json")
                                     let candiateFileInfo = new FileInfo(file)
                                     orderby candiateFileInfo.LastWriteTimeUtc descending
                                     select candiateFileInfo;

                DeleteExceedingCumulativeSize(candidateFiles, maxNumberOfBytesToRetain, 0);
            }
            catch (Exception ex)
            {
                SelfLog.WriteLine("Exception thrown while cleaning up invalid payload files: {0}", ex);
            }
        }

        static void DeleteExceedingCumulativeSize(IEnumerable<FileInfo> files, long maxNumberOfBytesToRetain, int alwaysRetainCount)
        {
            long cumulative = 0;
            var i = 0;
            foreach (var file in files)
            {
                cumulative += file.Length;

                if (i++ < alwaysRetainCount)
                    continue;

                if (cumulative <= maxNumberOfBytesToRetain)
                    continue;

                try
                {
                    file.Delete();
                }
                catch (Exception ex)
                {
                    SelfLog.WriteLine("Exception thrown while trying to delete file {0}: {1}", file.FullName, ex);
                }
            }
        }
    }
}

#endif
