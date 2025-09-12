using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Plugin.Logviewer;

#nullable enable

public sealed class LogRow {
    public DateTime Date { get; set; }
    public string Level { get; set; } = "";
    public string Source { get; set; } = "";
    public string Member { get; set; } = "";

    // Line and message together
    public string Message { get; set; } = "";
}

public record LogEntry(string[] Parts);

public enum ReadStatus { Ok, Empty, NotFound, Locked, Cancelled, Error }

public record ReadResult(
    ReadStatus Status,
    IEnumerable<string[]> Parts,
    Exception? Error = null
);

public interface ILogService {
    string LogDirectory { get; }

    string? GetLatestLogPath();

    string? PickLogFile();

    string? GetActiveLogFile();

    Task<ReadResult> ReadPartsAsync(string path, string delimiter, int markerIndex, CancellationToken ct);
}

//war internal
public static class LogRowMapper {

    public static LogRow ToLogRow(string[] parts) {
        var tsText = parts.ElementAtOrDefault(0) ?? "";
        var level = parts.ElementAtOrDefault(1) ?? "";
        var source = parts.ElementAtOrDefault(2) ?? "";
        var member = parts.ElementAtOrDefault(3) ?? "";
        // Line parts[4] + message parts[5]
        var msg = ((parts.ElementAtOrDefault(4) ?? "") + " " + (parts.ElementAtOrDefault(5) ?? "")).Trim();

        DateTime ts = DateTime.TryParse(tsText,
                                        System.Globalization.CultureInfo.InvariantCulture,
                                        System.Globalization.DateTimeStyles.None,
                                        out var dt)
                      ? dt : DateTime.MinValue;

        return new LogRow {
            Date = ts,
            Level = level,
            Source = source,
            Member = member,
            Message = msg,
        };
    }
}

public sealed class LogService : ILogService {
    public string LogDirectory { get; } = LogUtils.GetLogDirectory();

    public string? PickLogFile() => LogUtils.PickLogFile();

    public string? GetActiveLogFile() => LogUtils.GetActiveLogFile(LogDirectory);

    public string? GetLatestLogPath() {
        var files = Directory.EnumerateFiles(LogDirectory, "*.log").ToArray();
        if (files.Length == 0) return null;

        var byTime = files
            .Select(f => (Path: f, Ts: SafeGetUtc(f)))
            .OrderBy(t => t.Ts)
            .Select(t => t.Path)
            .LastOrDefault();

        return byTime ?? files.OrderBy(f => f, StringComparer.OrdinalIgnoreCase).LastOrDefault();

        static DateTime SafeGetUtc(string f) {
            try { return File.GetLastWriteTimeUtc(f); } catch { return DateTime.MinValue; }
        }
    }

    public async Task<ReadResult> ReadPartsAsync(string path, string delimiter, int markerIndex, CancellationToken ct) {
        if (!File.Exists(path))
            return new ReadResult(ReadStatus.NotFound, Array.Empty<string[]>());

        try {
            var rows = await LogReader.ReadAsync(path, delimiter, markerIndex, ct).ConfigureAwait(false);

            IEnumerable<string[]> parts = rows.Select(r => r.Parts);

            if (rows.Count == 0)
                return new ReadResult(ReadStatus.Empty, parts);

            var status = ct.IsCancellationRequested ? ReadStatus.Cancelled : ReadStatus.Ok;
            return new ReadResult(status, parts);
        } catch (OperationCanceledException oce) {
            return new ReadResult(ReadStatus.Cancelled, Array.Empty<string[]>(), oce);
        } catch (IOException ioe) {
            return new ReadResult(ReadStatus.Locked, Array.Empty<string[]>(), ioe);
        } catch (Exception ex) {
            return new ReadResult(ReadStatus.Error, Array.Empty<string[]>(), ex);
        }
    }
}