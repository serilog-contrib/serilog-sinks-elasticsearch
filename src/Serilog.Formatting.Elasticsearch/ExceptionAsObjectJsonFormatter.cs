// Copyright 2016 Serilog Contributors
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
using System.IO;

namespace Serilog.Formatting.Elasticsearch
{
    /// <summary>
    /// A JSON formatter which plays nice with Kibana, 
    /// by serializing any exception into an exception object, instead of relying on 
    /// an array of the exceptions and the inner exception.
    /// 
    /// Note that using this formatter comes at the cost that the exception tree 
    /// with inner exceptions can grow deep.
    /// </summary>
    public class ExceptionAsObjectJsonFormatter : ElasticsearchJsonFormatter
    {
        /// <summary>
        /// Constructs a <see cref="ExceptionAsObjectJsonFormatter"/>.
        /// </summary>
        /// <param name="omitEnclosingObject">If true, the properties of the event will be written to
        /// the output without enclosing braces. Otherwise, if false, each event will be written as a well-formed
        /// JSON object.</param>
        /// <param name="closingDelimiter">A string that will be written after each log event is formatted.
        /// If null, <see cref="Environment.NewLine"/> will be used. Ignored if <paramref name="omitEnclosingObject"/>
        /// is true.</param>
        /// <param name="renderMessage">If true, the message will be rendered and written to the output as a
        /// property named RenderedMessage.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="serializer">Inject a serializer to force objects to be serialized over being ToString()</param>
        /// <param name="inlineFields">When set to true values will be written at the root of the json document</param>
        /// <param name="renderMessageTemplate">If true, the message template will be rendered and written to the output as a
        /// property named RenderedMessageTemplate.</param>
        /// <param name="formatStackTraceAsArray">If true, splits the StackTrace by new line and writes it as a an array of strings</param>
        public ExceptionAsObjectJsonFormatter(bool omitEnclosingObject = false, 
            string closingDelimiter = null, 
            bool renderMessage = false, 
            IFormatProvider formatProvider = null, 
            ISerializer serializer = null, 
            bool inlineFields = false, 
            bool renderMessageTemplate = true,
            bool formatStackTraceAsArray = false) 
            : base(omitEnclosingObject, closingDelimiter, renderMessage, formatProvider, serializer, inlineFields, renderMessageTemplate, formatStackTraceAsArray)
        {
        }

        /// <summary>
        /// Writes out the attached exception
        /// </summary>
        protected override void WriteException(Exception exception, ref string delim, TextWriter output)
        {
            output.Write(delim);
            output.Write("\"");
            output.Write("exception");
            output.Write("\":{");
            WriteExceptionTree(exception, ref delim, output, 0);
            output.Write("}");
        }

        private void WriteExceptionTree(Exception exception, ref string delim, TextWriter output, int depth)
        {
            delim = "";
            WriteSingleException(exception, ref delim, output, depth);
            exception = exception.InnerException;
            if (exception != null)
            {
                output.Write(",");
                output.Write("\"innerException\":{");
                WriteExceptionTree(exception, ref delim, output, depth + 1);
                output.Write("}");
            }
        }
    }
}