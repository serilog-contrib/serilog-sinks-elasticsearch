namespace Serilog.Sinks.Elasticsearch.Sinks.ElasticSearch;

public class ModifyConnectionSettingsOptions
{
    public string ConnectionGlobalHeaders { get; set; }
    
    public bool IgnoreSslRemoteCertificateChainErrors { get; set; }
    
    public bool IgnoreSslRemoteCertificateNameMismatchErrors { get; set; }
    
    public bool IgnoreSslRemoteCertificateNotAvailableErrors { get; set; }
}