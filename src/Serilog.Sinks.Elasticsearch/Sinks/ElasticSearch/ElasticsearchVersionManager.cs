#nullable enable
using Elasticsearch.Net;
using Elasticsearch.Net.Specification.CatApi;
using Serilog.Debugging;
using System;
using System.Linq;

namespace Serilog.Sinks.Elasticsearch.Sinks.ElasticSearch
{
    /// <summary>
    /// Encapsulates detection of Elasticsearch version
    /// and fallback in case of detection failiure.
    /// </summary>
    internal class ElasticsearchVersionManager
    {
        private readonly bool _detectElasticsearchVersion;
        private readonly IElasticLowLevelClient _client;

        /// <summary>
        /// We are defaulting to version 7.17.0
        /// as currently supported versions are 7 and 8,
        /// while version 8 retains wire backward compatibility with 7.17.0
        /// and index backward compatibility with 7.0.0
        /// </summary>
        public readonly Version DefaultVersion = new(7, 17);
        public Version? DetectedVersion { get; private set; }
        public bool DetectionAttempted { get; private set; }

        public ElasticsearchVersionManager(
            bool detectElasticsearchVersion,
            IElasticLowLevelClient client)
        {
            _detectElasticsearchVersion = detectElasticsearchVersion;
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public Version EffectiveVersion
        {
            get
            {
                if (DetectedVersion is not null)
                    return DetectedVersion;

                if (_detectElasticsearchVersion == false
                    || DetectionAttempted == true)
                    return DefaultVersion;

                // Attemp once
                DetectedVersion = DiscoverClusterVersion();

                return DetectedVersion ?? DefaultVersion;
            }
        }

        internal Version? DiscoverClusterVersion()
        {
            try
            {
                var response = _client.Cat.Nodes<StringResponse>(new CatNodesRequestParameters()
                {
                    Headers = new[] { "v" }
                });
                if (!response.Success) return null;

                var discoveredVersion = response.Body.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault();

                if (discoveredVersion == null)
                    return null;

                return new Version(discoveredVersion);

            }
            catch (Exception ex)
            {
                SelfLog.WriteLine("Failed to discover the cluster version. {0}", ex);
                return null;
            }
            finally
            {
                DetectionAttempted = true;
            }
        }
    }
}
