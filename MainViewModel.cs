using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace NINA.Plugin.Logviewer;
#nullable enable

public sealed class MainViewModel : INotifyPropertyChanged {
    private const int TimeoutMs = 10000;

    private readonly ILogService _service;

    // Keine ObservableCollection mehr – performantes Bulk-Set über ListCollectionView
    private ICollectionView? _logEntriesView;

    public Filters Filters { get; } = new();

    public ICollectionView LogEntriesView {
        get => _logEntriesView ??= new ListCollectionView(new List<LogRow>());
        private set {
            if (!ReferenceEquals(_logEntriesView, value)) {
                _logEntriesView = value;
                OnPropertyChanged();
            }
        }
    }

    public string LogDir => _service.LogDirectory;

    private bool _isLoading;

    public bool IsLoading {
        get => _isLoading;
        private set {
            if (_isLoading == value) return;
            _isLoading = value;
            OnPropertyChanged();
            _loadLatestCommand.RaiseCanExecuteChanged();
            _browseLogCommand.RaiseCanExecuteChanged();
        }
    }

    private string? _selectedLogPath;

    public string? SelectedLogPath {
        get => _selectedLogPath;
        private set { if (_selectedLogPath == value) return; _selectedLogPath = value; OnPropertyChanged(); }
    }

    private ReadStatus _lastReadStatus;

    public ReadStatus LastReadStatus {
        get => _lastReadStatus;
        private set { if (_lastReadStatus == value) return; _lastReadStatus = value; OnPropertyChanged(); }
    }

    private string? _statusMessage;

    public string? StatusMessage {
        get => _statusMessage;
        private set { if (_statusMessage == value) return; _statusMessage = value; OnPropertyChanged(); }
    }

    private const string Delimiter = "|";
    private const int MarkerIndex = 0;

    private readonly AsyncRelayCommand _loadLatestCommand;
    private readonly AsyncRelayCommand _browseLogCommand;
    public ICommand LoadLatestCommand => _loadLatestCommand;
    public ICommand BrowseLogCommand => _browseLogCommand;

    public MainViewModel(ILogService service) {
        _service = service;

        // Filters steuert die Sichtbarkeit (HideInfo etc.) – VM-eigene HideInfo wurde entfernt.
        Filters.PropertyChanged += (_, __) => LogEntriesView.Refresh();

        _loadLatestCommand = new AsyncRelayCommand(async _ => {
            if (IsLoading) return;
            var latest = _service.GetActiveLogFile(); // kein Fallback – wie von Dir gewünscht
            if (latest is null) { SetStatus(ReadStatus.NotFound, "No logfile found."); return; }
            await LoadAsync(latest);
        }, _ => !IsLoading);

        _browseLogCommand = new AsyncRelayCommand(async _ => {
            if (IsLoading) return;
            var chosen = _service.PickLogFile();
            if (!string.IsNullOrEmpty(chosen)) {
                await LoadAsync(chosen);
            }
        }, _ => !IsLoading);
    }

    public async Task LoadAsync(string path) {
        // Interner Timeout-Fallback: nach 10 s abbrechen (keine manuelle Cancellation-UI)
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(TimeoutMs));
        var ct = timeoutCts.Token;

        try {
            await RunOnUIAsync(() => { IsLoading = true; });

            var result = await _service.ReadPartsAsync(path, Delimiter, MarkerIndex, ct).ConfigureAwait(false);

            if (result.Status == ReadStatus.Ok || result.Status == ReadStatus.Cancelled) {
                var buffer = new List<LogRow>(capacity: 4096);

                // Responsiveness-Throttle (alle ~40ms ein UI-Yield);
                var sw = Stopwatch.StartNew();
                long lastMs = 0;

                foreach (var parts in result.Parts) {
                    buffer.Add(LogRowMapper.ToLogRow(parts));

                    if (sw.ElapsedMilliseconds - lastMs >= 40) {
                        await RunOnUIAsync(() => { /* no-op */ });
                        lastMs = sw.ElapsedMilliseconds;
                    }
                }

                await RunOnUIAsync(() => {
                    LogEntriesView = new ListCollectionView(buffer) {
                        Filter = o => o is LogRow r && Filters.Matches(r)
                    };
                    SelectedLogPath = Path.GetFileName(path);
                });

                if (result.Status == ReadStatus.Cancelled) {
                    SetStatus(ReadStatus.Cancelled, "Reading timed out after 10 seconds. Showing partial results.");
                    Notification.ShowError("Reading timed out after 10 seconds.\nShowing partial results.");
                    Logger.Warning("Reading log was cancelled after timeout, showing partial results.");
                } else {
                    SetStatus(ReadStatus.Ok, $"Loaded {buffer.Count} log entries");
                }
            } else {
                await RunOnUIAsync(() => HandleNonOk(result, path));
            }
        } finally {
            await RunOnUIAsync(() => { IsLoading = false; });
        }
    }

    private void HandleNonOk(ReadResult result, string path) {
        switch (result.Status) {
            case ReadStatus.Empty:
                SelectedLogPath = Path.GetFileName(path);
                Logger.Error($"The selected log file is empty\n{path}.");
                Notification.ShowError("The selected log file is empty.");
                break;

            case ReadStatus.NotFound:
                Logger.Error("No log file found.");
                Notification.ShowError("No log file found.");
                break;

            case ReadStatus.Locked:
                Logger.Error($"File locked by another process.\n{result.Error?.Message}");
                Notification.ShowError($"File locked by another process.\n{result.Error?.Message}");
                break;

            case ReadStatus.Cancelled:
                Logger.Error("Reading log was cancelled (timeout).");
                Notification.ShowError("Reading log was cancelled after 10 seconds.");
                break;

            case ReadStatus.Error:
                Logger.Error($"An error occurred while reading the log file.\n{result.Error?.Message}");
                Notification.ShowError($"An error occurred while reading the log file.\n{result.Error?.Message}");
                break;
        }
        SetStatus(result.Status, result.Error?.Message);
    }

    private static Task RunOnUIAsync(Action action) {
        var disp = Application.Current?.Dispatcher;
        if (disp == null || disp.CheckAccess()) { action(); return Task.CompletedTask; }
        return disp.InvokeAsync(action).Task;
    }

    private void SetStatus(ReadStatus status, string? message) {
        LastReadStatus = status;
        StatusMessage = message;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

// Vermeidet async-void in Commands und unterbindet Reentrancy während der Ausführung.
public sealed class AsyncRelayCommand : ICommand {
    private readonly Func<object?, Task> _executeAsync;
    private readonly Predicate<object?>? _canExecute;
    private bool _isExecuting;

    public AsyncRelayCommand(Func<object?, Task> executeAsync, Predicate<object?>? canExecute = null) {
        _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter)
        => !_isExecuting && (_canExecute?.Invoke(parameter) ?? true);

    public async void Execute(object? parameter) {
        if (_isExecuting) return;
        _isExecuting = true;
        RaiseCanExecuteChanged();
        try { await _executeAsync(parameter); } finally { _isExecuting = false; RaiseCanExecuteChanged(); }
    }

    public event EventHandler? CanExecuteChanged {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
}