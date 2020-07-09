using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Elastic.Elasticsearch.Xunit;

namespace Serilog.Sinks.Elasticsearch.IntegrationTests.Bootstrap
{
	/// <summary> Feeding TestClient.Configuration options to the runner</summary>
	public class SerilogSinkElasticsearchXunitRunOptions : ElasticXunitRunOptions
	{
		public SerilogSinkElasticsearchXunitRunOptions()
		{
			RunIntegrationTests = true;
			RunUnitTests = false;
			ClusterFilter = null;
			TestFilter = null;
			IntegrationTestsMayUseAlreadyRunningNode = false;
		}

        public override void OnBeforeTestsRun() { }

        public override void OnTestsFinished(Dictionary<string, Stopwatch> clusterTotals, ConcurrentBag<Tuple<string, string>> failedCollections)
		{
			Console.Out.Flush();
			DumpClusterTotals(clusterTotals);
			DumpFailedCollections(failedCollections);
		}

		private static void DumpClusterTotals(Dictionary<string, Stopwatch> clusterTotals)
		{
			Console.WriteLine("--------");
			Console.WriteLine("Individual cluster running times:");
			foreach (var kv in clusterTotals) Console.WriteLine($"- {kv.Key}: {kv.Value.Elapsed}");
			Console.WriteLine("--------");
		}

		private static void DumpFailedCollections(ConcurrentBag<Tuple<string, string>> failedCollections)
		{
			if (failedCollections.Count <= 0) return;

			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("Failed collections:");
			foreach (var t in failedCollections.OrderBy(p => p.Item1).ThenBy(t => t.Item2))

			{
				var cluster = t.Item1;
				Console.WriteLine($" - {cluster}: {t.Item2}");
			}
			DumpReproduceFilters(failedCollections);
			Console.ResetColor();
		}

		private static void DumpReproduceFilters(ConcurrentBag<Tuple<string, string>> failedCollections)
		{
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine("---Reproduce: -----");
			var reproduceLine = ReproduceCommandLine(failedCollections);
			Console.WriteLine(reproduceLine);
			if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TEAMCITY_VERSION")))
				Console.WriteLine($"##teamcity[buildProblem description='{reproduceLine}']");
			if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TF_BUILD")))
			{
				var count = failedCollections.Count;
				Console.WriteLine($"##vso[task.logissue type=error;]{count} test failures");
				Console.WriteLine($"##vso[task.logissue type=error;]{reproduceLine}");
			}
			Console.WriteLine("--------");
		}

		private static string ReproduceCommandLine(ConcurrentBag<Tuple<string, string>> failedCollections)
        {
            var sb = new StringBuilder("build.bat ");

			if (failedCollections.Count > 0)
			{
				var clusters = string.Join(",", failedCollections
					.Select(c => c.Item1.ToLowerInvariant())
					.Distinct());
				sb.Append(" \"");
				sb.Append(clusters);
				sb.Append("\"");
			}

			if ((failedCollections.Count < 30) && failedCollections.Count > 0)
			{
				sb.Append(" \"");
				var tests = string.Join(",", failedCollections
					.OrderBy(t => t.Item2)
					.Select(c => c.Item2.ToLowerInvariant()
						.Split('.')
						.Last()
						.Replace("apitests", "")
						.Replace("usagetests", "")
						.Replace("tests", "")
					));
				sb.Append(tests);
				sb.Append("\"");
			}

			var reproduceLine = sb.ToString();
			return reproduceLine;
		}
	}
}
