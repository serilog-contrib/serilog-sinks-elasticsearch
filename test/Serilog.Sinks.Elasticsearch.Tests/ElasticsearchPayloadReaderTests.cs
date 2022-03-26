using System;
using System.IO;
using System.Text;
using FluentAssertions;
using Serilog.Sinks.Elasticsearch.Durable;
using Xunit;

namespace Serilog.Sinks.Elasticsearch.Tests;

public class ElasticsearchPayloadReaderTests : IDisposable
{
    private readonly string _tempFileFullPathTemplate;
    private string _bufferFileName;

    public ElasticsearchPayloadReaderTests()
    {
        _tempFileFullPathTemplate = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")) + "-{0}.json";
    }

    public void Dispose()
    {
        System.IO.File.Delete(_bufferFileName);
    }

    [Theory]
    [InlineData(RollingInterval.Day)]
    [InlineData(RollingInterval.Hour)]
    [InlineData(RollingInterval.Infinite)]
    [InlineData(RollingInterval.Minute)]
    [InlineData(RollingInterval.Month)]
    [InlineData(RollingInterval.Year)]
    public void ElasticsearchPayloadReader_ReadPayload_ShouldReadSpecifiedTypesOfRollingFile(
        RollingInterval rollingInterval)
    {
        // Arrange
        var format = rollingInterval.GetFormat();
        var payloadReader = new ElasticsearchPayloadReader("testPipelineName",
            "TestTypeName",
            null,
            (_, _) => "TestIndex",
            ElasticOpType.Index);
        var lines = new[]
        {
            rollingInterval.ToString()
        };
        _bufferFileName = string.Format(_tempFileFullPathTemplate,
            string.IsNullOrEmpty(format) ? string.Empty : new DateTime(2000, 1, 1).ToString(format));
        // Important to use UTF8 with BOM if we are starting from 0 position 
        System.IO.File.WriteAllLines(_bufferFileName, lines, new UTF8Encoding(true));

        // Act
        var fileSetPosition = new FileSetPosition(0, _bufferFileName);
        var count = 0;
        var payload =
            payloadReader.ReadPayload(int.MaxValue,
                null,
                ref fileSetPosition,
                ref count,
                _bufferFileName);

        // Assert
        payload.Count.Should().Be(lines.Length * 2);
        payload[1].Should().Be(lines[0]);
    }
}