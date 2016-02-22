using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Serilog.Sinks.Elasticsearch.Tests
{
    public static class TestDataHelper
    {
        public static string ReadEmbeddedResource(
            Assembly assembly,
            string embeddedResourceNameEndsWith)
        {
            var resourceNames = assembly.GetManifestResourceNames();
            var resourceName = resourceNames.SingleOrDefault(n => n.EndsWith(embeddedResourceNameEndsWith));

            if (string.IsNullOrEmpty(resourceName))
            {
                throw new ArgumentException(
                    string.Format(
                        "Could not find embedded resouce name that ends with '{0}', only found these: {1}",
                        embeddedResourceNameEndsWith,
                        string.Join(", ", resourceNames)),
                    "embeddedResourceNameEndsWith");
            }

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new ArgumentException(
                        string.Format("Failed to open embedded resource stream for resource '{0}'", resourceName));
                }

                using (var streamReader = new StreamReader(stream))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }
    }
}
