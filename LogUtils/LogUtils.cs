//
// Credíts and thanks to tcpalmer for the original code.
// https://github.com/tcpalmer
//

using Microsoft.WindowsAPICodePack.Dialogs;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace NINA.Plugin.Logviewer;

#nullable enable

public static class LogUtils {
    private static string? LogDirectory { get; } = GetLogDirectory();

    //private LogUtils() {
    //}

    public static string GetActiveLogFile(string logDirectory) {
        try {
            Regex re = new Regex(GetActiveLogFileRE(), RegexOptions.Compiled);
            List<string> fileList = new List<string>(Directory.GetFiles(logDirectory));

            foreach (string file in fileList) {
                if (re.IsMatch(Path.GetFileName(file))) {
                    return file;
                }
            }

            Logger.Warning($"failed to find active NINA log file in {logDirectory}, cannot process log for LogViewer.");
            return "";
        } catch (Exception e) {
            Logger.Warning($"failed to find active NINA log file in {logDirectory}, cannot process log for LogViewer: {e.Message} {e.StackTrace}");
            return "";
        }
    }

    public static string GetActiveLogFileRE() {
        // NINA log files match yyyyMMdd-HHmmss-VERSION.PID-yyyyMM.log - see NINA.Core.Utility.Logger.
        // Note that daily log file rolling was added in Sept 2023 - that added the '-yyyyMMdd' to the end.
        // It was then switched to monthly rolling in Oct 2023 so now '-yyyyMM' but RE will match either.

        string version = CoreUtil.Version;
        int processId = Environment.ProcessId;
        return @"^\d{8}-\d{6}-" + $"{version}.{processId}" + @"-\d{6,8}.log$";
    }

    public static string GetLogDirectory() {
        return Path.Combine(CoreUtil.APPLICATIONTEMPPATH, "Logs");
    }

    public static string? PickLogFile() {
        var dialog = new CommonOpenFileDialog {
            Title = "NINA Log Directory",
            Filters = {
                    new CommonFileDialogFilter("NINA Logs", "*.log"),
                    new CommonFileDialogFilter("All Files", "*.*")
                },
            IsFolderPicker = false,
            EnsureFileExists = true,
            Multiselect = false,
            DefaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            InitialDirectory = LogDirectory
        };

        return dialog.ShowDialog() == CommonFileDialogResult.Ok ? dialog.FileName : null;
    }
}