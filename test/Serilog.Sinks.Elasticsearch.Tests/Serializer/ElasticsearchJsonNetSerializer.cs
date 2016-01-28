using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elasticsearch.Net;
using Newtonsoft.Json;

namespace Serilog.Sinks.Elasticsearch.Tests.Serializer
{
    public class ElasticsearchJsonNetSerializer : IElasticsearchSerializer
    {
        private readonly JsonSerializerSettings _settings;

        public ElasticsearchJsonNetSerializer()
        {
            this._settings = this.CreateSettings();
        }

        public T Deserialize<T>(Stream stream)
        {
            JsonSerializerSettings settings = this._settings;

            settings = settings ?? this._settings;
            return (T)JsonSerializer.Create(settings).Deserialize((JsonReader)new JsonTextReader((TextReader)new StreamReader(stream)), typeof(T));
        }

        public Task<T> DeserializeAsync<T>(Stream responseStream, CancellationToken cancellationToken = new CancellationToken())
        {
            TaskCompletionSource<T> completionSource = new TaskCompletionSource<T>();
            T result = this.Deserialize<T>(responseStream);
            completionSource.SetResult(result);
            return completionSource.Task;
        }

        public void Serialize(object data, Stream writableStream, SerializationFormatting formatting = SerializationFormatting.Indented)
        {
            var jsonFormatting =
                formatting == SerializationFormatting.Indented
                    ? Newtonsoft.Json.Formatting.Indented
                    : Newtonsoft.Json.Formatting.None;

            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data, jsonFormatting, this._settings));
            writableStream.Write(bytes, 0, bytes.Length);
        }

        public string CreatePropertyName(MemberInfo memberInfo)
        {
            return memberInfo.Name;
        }

        private JsonSerializerSettings CreateSettings()
        {
            return new JsonSerializerSettings()
            {
                DefaultValueHandling = DefaultValueHandling.Include,
                NullValueHandling = NullValueHandling.Ignore
            };
        }
    }
}
