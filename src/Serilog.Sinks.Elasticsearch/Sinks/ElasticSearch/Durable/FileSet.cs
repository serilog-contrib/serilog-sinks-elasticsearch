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
using System.Text.RegularExpressions;
using Serilog.Debugging;

namespace Serilog.Sinks.Elasticsearch.Durable
{
    class FileSet
    {
        readonly string _bookmarkFilename;
        readonly string _candidateSearchPath;
        readonly string _logFolder;
        readonly Regex _filenameMatcher;

        const string InvalidPayloadFilePrefix = "invalid-";

        public FileSet(string bufferBaseFilename)
        {
            if (bufferBaseFilename == null) throw new ArgumentNullException(nameof(bufferBaseFilename));

            _bookmarkFilename = Path.GetFullPath(bufferBaseFilename + ".bookmark");
            _logFolder = Path.GetDirectoryName(_bookmarkFilename);
            _candidateSearchPath = Path.GetFileName(bufferBaseFilename) + "-*.json";
            _filenameMatcher = new Regex("^" + Regex.Escape(Path.GetFileName(bufferBaseFilename)) + "-(?<date>\\d{8})(?<sequence>_[0-9]{3,}){0,1}\\.json$");
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
