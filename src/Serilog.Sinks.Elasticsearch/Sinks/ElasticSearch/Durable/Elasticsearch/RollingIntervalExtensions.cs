using System;
using System.Runtime.CompilerServices;

[assembly:
    InternalsVisibleTo(
        "Serilog.Sinks.Elasticsearch.Tests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100fb8d13fd344a1c6fe0fe83ef33c1080bf30690765bc6eb0df26ebfdf8f21670c64265b30db09f73a0dea5b3db4c9d18dbf6d5a25af5ce9016f281014d79dc3b4201ac646c451830fc7e61a2dfd633d34c39f87b81894191652df5ac63cc40c77f3542f702bda692e6e8a9158353df189007a49da0f3cfd55eb250066b19485ec")]

namespace Serilog.Sinks.Elasticsearch.Durable
{
    internal static class RollingIntervalExtensions
    {
        // From https://github.com/serilog/serilog-sinks-file/blob/dev/src/Serilog.Sinks.File/Sinks/File/RollingIntervalExtensions.cs#L19
        public static string GetFormat(this RollingInterval interval)
        {
            switch (interval)
            {
                case RollingInterval.Infinite:
                    return "";
                case RollingInterval.Year:
                    return "yyyy";
                case RollingInterval.Month:
                    return "yyyyMM";
                case RollingInterval.Day:
                    return "yyyyMMdd";
                case RollingInterval.Hour:
                    return "yyyyMMddHH";
                case RollingInterval.Minute:
                    return "yyyyMMddHHmm";
                default:
                    throw new ArgumentException("Invalid rolling interval");
            }
        }

        public static string GetMatchingDateRegularExpressionPart(this RollingInterval interval)
        {
            switch (interval)
            {
                case RollingInterval.Infinite:
                    return "";
                case RollingInterval.Year:
                    return "\\d{4}";
                case RollingInterval.Month:
                    return "\\d{6}";
                case RollingInterval.Day:
                    return "\\d{8}";
                case RollingInterval.Hour:
                    return "\\d{10}";
                case RollingInterval.Minute:
                    return "\\d{12}";
                default:
                    throw new ArgumentException("Invalid rolling interval");
            }
        }
    }
}