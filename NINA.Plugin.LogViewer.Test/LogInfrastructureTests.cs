using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Plugin.Logviewer.Tests;

[TestFixture]
public class LogInfrastructureTests
{
    [Test]
    public void LogRowMapper_ToLogRow_ValidFields_ParsesCorrectly()
    {
        var parts = new[]
        {
            "2025-01-01 12:00:00",  // date
            "INFO",                 // level
            "ascom.cs",             // source
            "GetEquipment",         // member
            "63",                   // line
            "Found 10micron mount"  // message
        };

        var row = LogRowMapper.ToLogRow(parts);

        Assert.That(row.Date, Is.EqualTo(new DateTime(2025, 1, 1, 12, 0, 0)));
        Assert.That(row.Level, Is.EqualTo("INFO"));
        Assert.That(row.Source, Is.EqualTo("ascom.cs"));
        Assert.That(row.Member, Is.EqualTo("GetEquipment"));
        Assert.That(row.Message, Is.EqualTo("63 Found 10micron mount"));
    }

    [Test]
    public void LogRowMapper_ToLogRow_InvalidDate_SetsMinValue()
    {
        var parts = new[] { "NotADate", "WARN", "", "", "L", "M" };

        var row = LogRowMapper.ToLogRow(parts);

        Assert.That(row.Date, Is.EqualTo(DateTime.MinValue));
        Assert.That(row.Level, Is.EqualTo("WARN"));
        Assert.That(row.Message, Is.EqualTo("L M"));
    }

    [Test]
    public void LogRowMapper_ToLogRow_MissingFields_DefaultsToEmpty()
    {
        var parts = Array.Empty<string>();

        var row = LogRowMapper.ToLogRow(parts);

        Assert.That(row.Level, Is.EqualTo(""));
        Assert.That(row.Source, Is.EqualTo(""));
        Assert.That(row.Member, Is.EqualTo(""));
        Assert.That(row.Message, Is.EqualTo(""));
    }

    [Test]
    public async Task ReadPartsAsync_FileNotFound_ReturnsNotFound()
    {
        var svc = new LogService();

        var result = await svc.ReadPartsAsync("nonexistent.log", "|", 0, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ReadStatus.NotFound));
        Assert.That(result.Parts, Is.Empty);
    }

    [Test]
    public async Task ReadPartsAsync_EmptyResult_ReturnsEmpty()
    {
        var fake = new FakeLogService(() => new List<LogEntry>());
        var result = await fake.ReadPartsAsync("dummy", "|", 0, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ReadStatus.Empty));
    }

    [Test]
    public async Task ReadPartsAsync_WithEntries_ReturnsOk()
    {
        var fake = new FakeLogService(() => new List<LogEntry> {
            new LogEntry(new[]{"2025-01-01","INFO","","","","Msg"})
        });
        var result = await fake.ReadPartsAsync("dummy", "|", 0, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ReadStatus.Ok));
        Assert.That(result.Parts, Is.Not.Empty);
    }

    [Test]
    public async Task ReadPartsAsync_CancelledToken_ReturnsCancelled()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var fake = new FakeLogService(() => new List<LogEntry> {
            new LogEntry(new[]{"2025-01-01","INFO","","","","Msg"})
        });
        var result = await fake.ReadPartsAsync("dummy", "|", 0, cts.Token);

        Assert.That(result.Status, Is.EqualTo(ReadStatus.Cancelled));
    }

    // ----- Hilfs-Fake -----
    private sealed class FakeLogService : ILogService
    {
        private readonly Func<ICollection<LogEntry>> _supplier;

        public FakeLogService(Func<ICollection<LogEntry>> supplier) => _supplier = supplier;

        public string LogDirectory => Path.GetTempPath();

        public string? GetActiveLogFile() => null;

        public string? GetLatestLogPath() => null;

        public string? PickLogFile() => null;

        public Task<ReadResult> ReadPartsAsync(string path, string delimiter, int markerIndex, CancellationToken ct)
        {
            try
            {
                var rows = _supplier();
                var parts = rows.Select(r => r.Parts);
                if (rows.Count == 0)
                    return Task.FromResult(new ReadResult(ReadStatus.Empty, parts));

                var status = ct.IsCancellationRequested ? ReadStatus.Cancelled : ReadStatus.Ok;
                return Task.FromResult(new ReadResult(status, parts));
            }
            catch (IOException ioe)
            {
                return Task.FromResult(new ReadResult(ReadStatus.Locked, Array.Empty<string[]>(), ioe));
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ReadResult(ReadStatus.Error, Array.Empty<string[]>(), ex));
            }
        }
    }
}