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
        if (!string.IsNullOrEmpty(_bufferFileName))
        {
            System.IO.File.Delete(_bufferFileName);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData(" ")]
    public void ReadPayload_SkipsEmptyLines(string emptyLine)
    {
        // Arrange
        var payloadReader = new ElasticsearchPayloadReader("testPipelineName", "TestTypeName", null,
            (_, _) => "TestIndex", ElasticOpType.Index, RollingInterval.Day);
        var lines = new[]
        {
            "line1",
            emptyLine,
            "line2"
        };
        _bufferFileName = string.Format(_tempFileFullPathTemplate, new DateTime(2000, 1, 1).ToString(RollingInterval.Day.GetFormat()));
        // Important to use UTF8 with BOM if we are starting from 0 position 
        System.IO.File.WriteAllLines(_bufferFileName, lines, new UTF8Encoding(true));

        // Act
        var fileSetPosition = new FileSetPosition(0, _bufferFileName);
        var count = 0;
        var payload = payloadReader.ReadPayload(int.MaxValue, null, ref fileSetPosition, ref count, _bufferFileName);

        // Assert
        payload.Count.Should().Be((lines.Length - 1) * 2);
        payload[1].Should().Be(lines[0]);
        payload[3].Should().Be(lines[2]);
        if (!string.IsNullOrEmpty(_bufferFileName))
        {
            System.IO.File.Delete(_bufferFileName);
        }
    }

    [Theory]
    [InlineData(RollingInterval.Day)]
    [InlineData(RollingInterval.Hour)]
    [InlineData(RollingInterval.Minute)]
    public void ReadPayload_ShouldReadSpecifiedTypesOfRollingFile(RollingInterval rollingInterval)
    {
        // Arrange
        var format = rollingInterval.GetFormat();
        var payloadReader = new ElasticsearchPayloadReader("testPipelineName",
            "TestTypeName",
            null,
            (_, _) => "TestIndex",
            ElasticOpType.Index,
            rollingInterval);
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
        var payload = payloadReader.ReadPayload(int.MaxValue,
            null,
            ref fileSetPosition,
            ref count,
            _bufferFileName);

        // Assert
        // Thus we ensure that file was properly handled by PayloadReader  
        payload.Count.Should().Be(lines.Length * 2);
        payload[1].Should().Be(lines[0]);
    }

    [Theory]
    [InlineData(RollingInterval.Infinite)]
    [InlineData(RollingInterval.Year)]
    [InlineData(RollingInterval.Month)]
    public void ElasticsearchPayloadReader_CannotUseRollingIntervalLessFrequentThanDay(RollingInterval rollingInterval)
    {
        // Arrange

        // Act
        Action act = () => new ElasticsearchPayloadReader("testPipelineName",
            "TestTypeName",
            null,
            (_, _) => "TestIndex",
            ElasticOpType.Index,
            rollingInterval);

        // Assert
        act.ShouldThrow<ArgumentException>()
            .WithMessage("Rolling intervals less frequent than RollingInterval.Day are not supported");
    }
}