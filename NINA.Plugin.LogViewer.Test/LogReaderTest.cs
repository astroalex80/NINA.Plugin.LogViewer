using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NINA.Plugin.Logviewer.Tests;

[TestFixture]
public class LogReaderTests
{
    private static async Task<string> MakeTempLogAsync(IEnumerable<string> lines)
    {
        var path = Path.Combine(Path.GetTempPath(), $"log_{Guid.NewGuid():N}.log");
        await File.WriteAllLinesAsync(path, lines);
        return path;
    }

    [Test]
    public async Task ReadAsync_BasicGroups_ParsesIntoEntries()
    {
        // Aufbau: Jede neue Zeile, deren parseablePart (0) ein Datum ist, startet einen neuen Block :contentReference[oaicite:5]{index=5}
        var lines = new[]
        {
            "Header without date|IGNORED",
            "2025-01-01 12:00:00|INFO|Core|A|Line1|Msg1",
            "continued part|of msg1",
            "2025-01-01 12:00:01|WARN|Core|B||Msg2"
        };
        var path = await MakeTempLogAsync(lines);

        var entries = await LogReader.ReadAsync(path, "|", 0, CancellationToken.None);

        Assert.That(entries.Count, Is.EqualTo(2));
        Assert.That(entries, Is.All.Not.Null);
        Assert.That(entries, Has.Exactly(1).Matches<LogEntry>(e => e.Parts[1] == "INFO"));
        Assert.That(entries, Has.Exactly(1).Matches<LogEntry>(e => e.Parts[1] == "WARN"));
    }

    [Test]
    public async Task ReadAsync_CancelledToken_ReturnsPartialOrEmpty_NoThrow()
    {
        // Sofort abgebrochener Token → OCE wird gefangen und bisheriges Ergebnis (leer) zurückgegeben :contentReference[oaicite:7]{index=7}
        var path = await MakeTempLogAsync(new[] { "2025-01-01 00:00:00|INFO|Core|A|L|M" });
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var entries = await LogReader.ReadAsync(path, "|", 0, cts.Token);

        Assert.That(entries, Is.Not.Null);
        // in der Regel leer, weil Abbruch vor erstem ReadLineAsync(ct)
        Assert.That(entries.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task ReadAsync_LastBufferGetsFlushed_AsEntry()
    {
        // Kein nachfolgender Start → letzter Buffer wird am Ende aufgenommen, sofern parseablePart ein Datum ist :contentReference[oaicite:8]{index=8}
        var path = await MakeTempLogAsync(new[] {
            "2025-01-01 00:00:00|INFO|Core|A|L|M",
            "cont of message"
        });

        var entries = await LogReader.ReadAsync(path, "|", 0, CancellationToken.None);

        Assert.That(entries.Count, Is.EqualTo(1));
        Assert.That(entries, Has.Exactly(1).Matches<LogEntry>(e => e.Parts[0].StartsWith("2025-01-01")));
    }
}