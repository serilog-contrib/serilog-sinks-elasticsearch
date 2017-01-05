using System;
using System.Collections.Generic;
using System.IO;
using Elasticsearch.Net;
using Serilog.Events;

namespace Serilog.Sinks.Elasticsearch.Sinks.ElasticSearch
{
    /// <summary>
    /// Custom Json formatter that treats a specified destructing property as the full source.
    /// Suitable for situations where you want only your destructed property as the content 
    /// that ends up in elasticsearch and want to avoid any other extra information. For example
    /// if your object if fully self contained with its own timestamp and everything then you would 
    /// want to avoid the extra time and other properties added by the json formatter and also want to
    /// avoid your main object appearing as a property of another top level object.
    /// For example if your object has properties Timestamp and Level then it will look like the following
    /// in elasticearch and no other extra information will be added
    ///     "_source": {
    ///        "Timestamp": "2017-01-03T10:56:56.0000000+11:00",
    ///        "Level": "Information",
    ///      } 
    /// </summary>
    public class TreatPropertyAsSourceElasticsearchJsonFormatter : ElasticsearchJsonFormatter
    {
        private readonly string _propertyNameToBeTreatedAsSource;

        /// <summary>
        /// Construct a <see cref="TreatPropertyAsSourceElasticsearchJsonFormatter"/>.
        /// </summary>
        /// <param name="propertyNameToBeTreatedAsSource">
        /// This is the property that will be assigned as the elasticsearch source. For example if you are writing the property like
        /// Logger.Information("@MyProperty", myProperty) then myProperty will become the root object and its properties will be become the 
        /// properties of the _source object. For example if myProperty has properties Timestamp and Level then it will look like the following
        /// in elasticearch and no other extra information will be added
        ///     "_source": {
        ///        "Timestamp": "2017-01-03T10:56:56.0000000+11:00",
        ///        "Level": "Information",
        ///      }
        /// </param>
        public TreatPropertyAsSourceElasticsearchJsonFormatter(string propertyNameToBeTreatedAsSource)
            : base(renderMessage:false, inlineFields:true)
        {
            _propertyNameToBeTreatedAsSource = propertyNameToBeTreatedAsSource.ToLowerInvariant();
        }

        /// <summary>
        /// Writes the _propertyNameToBeTreatedAsSource property as the _source for Object and Dictionary types
        /// This will behave exactly as ElasticsearchJsonFormatter if _propertyNameToBeTreatedAsSource is NOT specified
        /// </summary>
        protected override void WriteProperties(IReadOnlyDictionary<string, LogEventPropertyValue> properties, TextWriter output)
        {
            var precedingDelimiter = "";
            foreach (var property in properties)
            {
                if (property.Key.ToLowerInvariant() == _propertyNameToBeTreatedAsSource)
                {
                    if (property.Value is DictionaryValue)
                        WriteDictionaryWithoutWrapping(((DictionaryValue)property.Value).Elements, output);
                    else if (property.Value is StructureValue)
                        WriteStructureWithoutWrapping(((StructureValue)property.Value).Properties, output);
                    else
                        WriteJsonProperty(property.Key, property.Value, ref precedingDelimiter, output);
                }
                else
                    WriteJsonProperty(property.Key, property.Value, ref precedingDelimiter, output);
            }
        }
        /// <summary>
        /// Writes out the Structure without Wrapping in any top level object
        /// </summary>
        protected void WriteStructureWithoutWrapping(IEnumerable<LogEventProperty> properties, TextWriter output)
        {
            var delim = "";

            foreach (var property in properties)
                WriteJsonProperty(property.Name, property.Value, ref delim, output);
        }

        /// <summary>
        /// Writes out the Dictionary without Wrapping in any top level object
        /// </summary>
        protected void WriteDictionaryWithoutWrapping(IReadOnlyDictionary<ScalarValue, LogEventPropertyValue> elements, TextWriter output)
        {
            var delim = "";

            foreach (var e in elements)
                WriteJsonProperty(e.Key.Value.ToString(), e.Value, ref delim, output);
        }

        /// <summary>
        /// Writes out the Structure without any other extra properties like _typeTag etc.
        /// </summary>
        protected override void WriteStructure(string typeTag, IEnumerable<LogEventProperty> properties, TextWriter output)
        {
            output.Write("{");

            var delim = "";

            foreach (var property in properties)
                WriteJsonProperty(property.Name, property.Value, ref delim, output);

            output.Write("}");
        }

        /// <summary>
        /// Do not write any extra message
        /// </summary>
        protected override void WriteRenderedMessage(string message, ref string delim, TextWriter output)
        {
            // do nothing
        }

        /// <summary>
        /// Do not write any message template
        /// </summary>
        protected override void WriteMessageTemplate(string template, ref string delim, TextWriter output)
        {
            // do nothing
        }

        /// <summary>
        /// Do not write any extra level as it is likely part of the destructed logged object already
        /// </summary>
        protected override void WriteLevel(LogEventLevel level, ref string delim, TextWriter output)
        {
            // do nothing
        }

        /// <summary>
        /// Do not write any extra timestamp as it is likely part of the destructed logged object already
        /// </summary>
        protected override void WriteTimestamp(DateTimeOffset timestamp, ref string delim, TextWriter output)
        {
            // do nothing

        }
    }
}
