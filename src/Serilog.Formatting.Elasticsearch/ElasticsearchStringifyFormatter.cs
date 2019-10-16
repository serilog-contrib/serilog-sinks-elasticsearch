using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Serilog.Events;
using Serilog.Formatting.Elasticsearch;

namespace Host
{
    /// <summary>
    /// A JSON formatter which ensures no type conflicts arieses in Elasticsearch by casting everything to strings.
    /// 
    /// Note that using this formatter comes at the cost that the exception tree 
    /// with inner exceptions can grow deep.
    /// </summary>
    public class ElasticsearchStringifyFormatter : ExceptionAsObjectJsonFormatter
    {
        /// <summary>
        /// Construct a <see cref="ElasticsearchJsonFormatter"/>.
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
        /// <param name="formatStackTraceAsArray">If true, splits the StackTrace by new line and writes it as a an array of strings</param>
        /// <param name="renderMessageTemplate">If true, the message template will be rendered and written to the output as a
        /// property named RenderedMessageTemplate.</param>
        public ElasticsearchStringifyFormatter(
            bool omitEnclosingObject = false,
            string closingDelimiter = null,
            bool renderMessage = true,
            IFormatProvider formatProvider = null,
            ISerializer serializer = null,
            bool inlineFields = false,
            bool formatStackTraceAsArray = false,
            bool renderMessageTemplate = true) : base(omitEnclosingObject, closingDelimiter, renderMessage,
            formatProvider, serializer, inlineFields, formatStackTraceAsArray, renderMessageTemplate)
        {
        }

        /// <summary>
        /// Writes out the attached exception
        /// </summary>
        protected override void WriteProperties(
            IReadOnlyDictionary<string, LogEventPropertyValue> properties, TextWriter output) =>
            base.WriteProperties(Stringify(properties), output);

        private static IReadOnlyDictionary<string, LogEventPropertyValue> Stringify(
            IReadOnlyDictionary<string, LogEventPropertyValue> properties) =>
            properties.ToDictionary(entry => entry.Key, entry => Stringify(entry.Value));

        private static LogEventPropertyValue Stringify(LogEventPropertyValue value)
        {
            switch (value)
            {
                case ScalarValue scalar:
                    var element = scalar.Value.ToString();
                    return new ScalarValue(element);

                case SequenceValue sequence:
                    var elements = sequence.Elements.Select(Stringify);
                    return new SequenceValue(elements);

                case DictionaryValue dictionary:
                    var entries = dictionary.Elements.Select(entry =>
                        new KeyValuePair<ScalarValue, LogEventPropertyValue>(
                            (ScalarValue) Stringify(entry.Key), Stringify(entry.Value)));
                    return new DictionaryValue(entries);

                case StructureValue structure:
                    var properties = structure.Properties.Select(property =>
                        new LogEventProperty(property.Name, Stringify(property.Value)));
                    return new StructureValue(properties);

                default:
                    throw new ArgumentException("Invalid property type");
            }
        }
    }
}