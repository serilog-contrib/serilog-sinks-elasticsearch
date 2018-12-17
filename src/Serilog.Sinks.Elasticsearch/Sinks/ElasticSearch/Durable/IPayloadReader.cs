namespace Serilog.Sinks.Elasticsearch.Durable
{
    /// <summary>
    /// Reads logs from logfiles and formats it for logserver
    /// </summary>
    /// <typeparam name="TPayload"></typeparam>
    public interface IPayloadReader<TPayload>
    {
        TPayload ReadPayload(int batchPostingLimit, long? eventBodyLimitBytes, ref FileSetPosition position, ref int count,string fileName);
        TPayload GetNoPayload();
    }
}