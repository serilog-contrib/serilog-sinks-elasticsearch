using System.IO;
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
            _settings = CreateSettings();
        }

        public T Deserialize<T>(Stream stream)
        {
            JsonSerializerSettings settings = _settings;

            settings = settings ?? _settings;
            return (T)JsonSerializer.Create(settings).Deserialize((JsonReader)new JsonTextReader((TextReader)new StreamReader(stream)), typeof(T));
        }

        public Task<T> DeserializeAsync<T>(Stream responseStream, CancellationToken cancellationToken = new CancellationToken())
        {
            TaskCompletionSource<T> completionSource = new TaskCompletionSource<T>();
            T result = Deserialize<T>(responseStream);
            completionSource.SetResult(result);
            return completionSource.Task;
        }

        public void Serialize(object data, Stream writableStream, SerializationFormatting formatting = SerializationFormatting.Indented)
        {
            var jsonFormatting =
                formatting == SerializationFormatting.Indented
                    ? Newtonsoft.Json.Formatting.Indented
                    : Newtonsoft.Json.Formatting.None;

            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data, jsonFormatting, _settings));
            writableStream.Write(bytes, 0, bytes.Length);
        }

        public IPropertyMapping CreatePropertyMapping(MemberInfo memberInfo)
        {
            return new PropertyMapping { Name = memberInfo.Name };
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
