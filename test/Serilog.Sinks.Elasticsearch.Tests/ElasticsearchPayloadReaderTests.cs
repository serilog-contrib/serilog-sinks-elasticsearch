using System;
using System.IO;
using System.Text;
using FluentAssertions;
using Serilog.Sinks.Elasticsearch.Durable;
using Xunit;

namespace Serilog.Sinks.Elasticsearch.Tests;

public class ElasticsearchPayloadReaderTests : IDisposable
{
    private readonly string _tempFileFullPath;

    public ElasticsearchPayloadReaderTests()
    {
        _tempFileFullPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-{20000101}.json");
    }

    public void Dispose()
    {
        System.IO.File.Delete(_tempFileFullPath);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData(" ")]
    public void ReadPayload_SkipsEmptyLines(string emptyLine)
    {
        // Arrange
        var payloadReader = new ElasticsearchPayloadReader("testPipelineName", "TestTypeName", null,
            (_, _) => "TestIndex", ElasticOpType.Index);
        var lines = new[]
        {
            "line1",
            emptyLine,
            "line2"
        };
        // Important to use UTF8 with BOM if we are starting from 0 position 
        System.IO.File.WriteAllLines(_tempFileFullPath, lines, new UTF8Encoding(true));

        // Act
        var fileSetPosition = new FileSetPosition(0, _tempFileFullPath);
        var count = 0;
        var payload = payloadReader.ReadPayload(int.MaxValue, null, ref fileSetPosition, ref count, _tempFileFullPath);

        // Assert
        payload.Count.Should().Be((lines.Length - 1) * 2);
        payload[1].Should().Be(lines[0]);
        payload[3].Should().Be(lines[2]);
    }
}