using System;
using System.ComponentModel;

namespace NINA.Plugin.Logviewer;

#nullable enable

public class Filters : INotifyPropertyChanged {
    private bool _hideInfo;

    public bool HideInfo {
        get => _hideInfo;
        set { if (_hideInfo == value) return; _hideInfo = value; OnPropertyChanged(nameof(HideInfo)); }
    }

    private bool _hideTrace;

    public bool HideTrace {
        get => _hideTrace;
        set { if (_hideTrace == value) return; _hideTrace = value; OnPropertyChanged(nameof(HideTrace)); }
    }

    private bool _hideDebug;

    public bool HideDebug {
        get => _hideDebug;
        set { if (_hideDebug == value) return; _hideDebug = value; OnPropertyChanged(nameof(HideDebug)); }
    }

    // DATE|LEVEL|SOURCE|MEMBER|LINE|MESSAGE

    private string? _dateFilter;
    private string? _timeFilter;
    private string? _levelFilter;
    private string? _sourceFilter;
    private string? _memberFilter;
    private string? _messageFilter;

    public string? DateFilter {
        get => _dateFilter;
        set { if (_dateFilter == value) return; _dateFilter = value; OnPropertyChanged(nameof(DateFilter)); }
    }

    public string? TimeFilter {
        get => _timeFilter;
        set { if (_timeFilter == value) return; _timeFilter = value; OnPropertyChanged(nameof(TimeFilter)); }
    }

    public string? LevelFilter {
        get => _levelFilter;
        set { if (_levelFilter == value) return; _levelFilter = value; OnPropertyChanged(nameof(LevelFilter)); }
    }

    public string? SourceFilter {
        get => _sourceFilter;
        set { if (_sourceFilter == value) return; _sourceFilter = value; OnPropertyChanged(nameof(SourceFilter)); }
    }

    public string? MemberFilter {
        get => _memberFilter;
        set { if (_memberFilter == value) return; _memberFilter = value; OnPropertyChanged(nameof(MemberFilter)); }
    }

    public string? MessageFilter {
        get => _messageFilter;
        set { if (_messageFilter == value) return; _messageFilter = value; OnPropertyChanged(nameof(MessageFilter)); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public void ClearFilters() {
        HideInfo = false;
        HideTrace = false;
        HideDebug = false;
        DateFilter = null;
        TimeFilter = null;
        LevelFilter = null;
        SourceFilter = null;
        MemberFilter = null;
        MessageFilter = null;
    }

    /// <summary> Prüft, ob eine Zeile alle aktiven Filter passiert. </summary>
    public bool Matches(LogRow row) {
        if (HideInfo && string.Equals(row.Level, "INFO", StringComparison.OrdinalIgnoreCase))
            return false;

        if (HideTrace && string.Equals(row.Level, "TRACE", StringComparison.OrdinalIgnoreCase))
            return false;

        if (HideDebug && string.Equals(row.Level, "DEBUG", StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.IsNullOrWhiteSpace(DateFilter) && DateFilter.Length > 1) {
            var s = row.Date.ToString("yyyy-MM-dd  HH:mm:ss.ffff");
            if (s.IndexOf(DateFilter, StringComparison.OrdinalIgnoreCase) < 0)
                return false;
        }

        // Level
        if (!string.IsNullOrWhiteSpace(LevelFilter) && LevelFilter.Length > 1) {
            if (string.IsNullOrEmpty(row.Level) ||
                row.Level.IndexOf(LevelFilter, StringComparison.OrdinalIgnoreCase) < 0)
                return false;
        }

        // Source
        if (!string.IsNullOrWhiteSpace(SourceFilter) && SourceFilter.Length > 1) {
            if (string.IsNullOrEmpty(row.Source) ||
                row.Source.IndexOf(SourceFilter, StringComparison.OrdinalIgnoreCase) < 0)
                return false;
        }

        // Member
        if (!string.IsNullOrWhiteSpace(MemberFilter) && MemberFilter.Length > 1) {
            if (string.IsNullOrEmpty(row.Member) ||
                row.Member.IndexOf(MemberFilter, StringComparison.OrdinalIgnoreCase) < 0)
                return false;
        }

        // Message
        if (!string.IsNullOrWhiteSpace(MessageFilter) && MessageFilter.Length > 1) {
            if (string.IsNullOrEmpty(row.Message) ||
                row.Message.IndexOf(MessageFilter, StringComparison.OrdinalIgnoreCase) < 0)
                return false;
        }

        return true;
    }
}