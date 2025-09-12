using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Plugin.Logviewer;
#nullable enable

public class LogReader {

    public static async Task<ICollection<LogEntry>> ReadAsync(string path, string delimiter, int parseablePart, CancellationToken ct) {
        var result = new List<LogEntry>();
        using var fs = new FileStream(path,
                              FileMode.Open,
                              FileAccess.Read,
                              FileShare.ReadWrite | FileShare.Delete);
        using var r = new StreamReader(fs);
        var buf = new List<string>();
        string? line;

        try {
            while ((line = await r.ReadLineAsync(ct)) is not null) {
                ct.ThrowIfCancellationRequested();

                var parts = line.Split(delimiter);

                bool startsNew = DateTime.TryParse(
                    parts.ElementAtOrDefault(parseablePart),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out _
                );

                // ignores header lines
                if (!startsNew && buf.Count == 0)
                    continue;

                if (startsNew) {
                    if (buf.Count > 0)
                        result.Add(Make(buf, delimiter));
                    buf.Clear();
                }

                buf.Add(line);
            }

            if (buf.Count > 0) {
                var first = buf[0].Split(delimiter);
                if (DateTime.TryParse(first.ElementAtOrDefault(parseablePart),
                                      CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                    result.Add(Make(buf, delimiter));
                buf.Clear();
            }

            return result;
        } catch (OperationCanceledException) //on user cancellation
          {
            Logger.Info("Log reading cancelled by user.");
            return result;
        } catch (Exception ex) // when error
          {
            Notification.ShowError($"Error reading: {path}");
            Logger.Error($"Error reading log: {path} exception: {ex.Message}");
            return result;
        }

        static LogEntry Make(List<string> lines, string delimiter) {
            var all = string.Join("\n", lines).Split(delimiter);
            //DateTime.TryParse(all[ptp], CultureInfo.InvariantCulture, DateTimeStyles.None, out var ts);
            return new(all);
        }
    }
}