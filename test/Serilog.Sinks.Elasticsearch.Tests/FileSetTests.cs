using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FluentAssertions;
using Serilog.Sinks.Elasticsearch.Durable;
using Xunit;

namespace Serilog.Sinks.Elasticsearch.Tests;

public class FileSetTests : IDisposable
{
    private readonly string _fileNameBase;
    private readonly string _tempFileFullPathTemplate;
    private Dictionary<RollingInterval, string> _bufferFileNames;

    public FileSetTests()
    {
        _fileNameBase = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        _tempFileFullPathTemplate = _fileNameBase + "-{0}.json";
    }

    public void Dispose()
    {
        foreach (var bufferFileName in _bufferFileNames.Values) System.IO.File.Delete(bufferFileName);
    }

    [Theory]
    [InlineData(RollingInterval.Day)]
    [InlineData(RollingInterval.Hour)]
    [InlineData(RollingInterval.Infinite)]
    [InlineData(RollingInterval.Minute)]
    [InlineData(RollingInterval.Month)]
    [InlineData(RollingInterval.Year)]
    public void GetBufferFiles_ReturnsOnlySpecifiedTypeOfRollingFile(RollingInterval rollingInterval)
    {
        // Arrange
        var format = rollingInterval.GetFormat();
        _bufferFileNames = GenerateFilesUsingFormat(format);
        var fileSet = new FileSet(_fileNameBase);
        var bufferFileForInterval = _bufferFileNames[rollingInterval];

        // Act
        var bufferFiles = fileSet.GetBufferFiles();

        // Assert
        bufferFiles.Should().BeEquivalentTo(bufferFileForInterval);
    }

    /// <summary>
    ///     Generates buffer files for all RollingIntervals and returns dictionary of {rollingInterval, fileName} pairs.
    /// </summary>
    /// <param name="format"></param>
    /// <returns></returns>
    private Dictionary<RollingInterval, string> GenerateFilesUsingFormat(string format)
    {
        var result = new Dictionary<RollingInterval, string>();
        foreach (var rollingInterval in Enum.GetValues(typeof(RollingInterval)))
        {
            var bufferFileName = string.Format(_tempFileFullPathTemplate,
                string.IsNullOrEmpty(format) ? string.Empty : new DateTime(2000, 1, 1).ToString(format));
            var lines = new[] {rollingInterval.ToString()};
            // Important to use UTF8 with BOM if we are starting from 0 position 
            System.IO.File.WriteAllLines(bufferFileName, lines, new UTF8Encoding(true));
            result.Add((RollingInterval) rollingInterval, bufferFileName);
        }

        return result;
    }
}