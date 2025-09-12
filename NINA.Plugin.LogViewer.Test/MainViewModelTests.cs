using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using System.Windows.Data;
using System.Windows.Input;
using NUnit.Framework;

namespace NINA.Plugin.Logviewer.Tests;

// WPF-abhängige Tests benötigen STA
[Apartment(ApartmentState.STA)]
public class MainViewModelTests
{
    private FakeLogService _svc = null!;
    private MainViewModel _vm = null!;

    [SetUp]
    public void SetUp()
    {
        _svc = new FakeLogService();
        _vm = new MainViewModel(_svc);
    }

    [Test]
    public async Task LoadAsync_Ok_PopulatesView_And_SetsStatusOk()
    {
        // Arrange
        var path = @"C:\logs\test.log";
        _svc.ReadPartsAsyncHandler = (_, _, _, _) =>
            Task.FromResult(new ReadResult(ReadStatus.Ok, SampleParts(2)));

        // Act
        await _vm.LoadAsync(path);

        // Assert
        Assert.That(_vm.LastReadStatus, Is.EqualTo(ReadStatus.Ok));
        Assert.That(_vm.SelectedLogPath, Is.EqualTo("test.log")); // Path.GetFileName(...) im VM
        var view = (ListCollectionView)_vm.LogEntriesView;
        Assert.That(view.Count, Is.EqualTo(2));

        // Mapping: Message = parts[4] + " " + parts[5]
        var first = (LogRow)view.GetItemAt(0)!;
        Assert.That(first.Message, Is.EqualTo("Line1 Message1"));
        Assert.That(first.Level, Is.EqualTo("INFO"));
    }

    [Test]
    public async Task LoadAsync_Cancelled_ShowsPartialResultsAndStatusCancelled()
    {
        // Arrange
        var path = @"C:\logs\timeout.log";
        _svc.ReadPartsAsyncHandler = (_, _, _, _) =>
            Task.FromResult(new ReadResult(ReadStatus.Cancelled, SampleParts(1)));

        // Act
        await _vm.LoadAsync(path);

        // Assert
        Assert.That(_vm.LastReadStatus, Is.EqualTo(ReadStatus.Cancelled));
        var view = (ListCollectionView)_vm.LogEntriesView;
        Assert.That(view.Count, Is.EqualTo(1));
        // StatusMessage kommt aus VM bei Cancelled (Timeout-Hinweis)
        Assert.That(_vm.StatusMessage, Does.Contain("timed out").IgnoreCase);
    }

    [Test]
    public async Task LoadAsync_Empty_SetsStatusEmpty_And_SetsSelectedFileName()
    {
        // Arrange
        var path = @"C:\logs\empty.log";
        _svc.ReadPartsAsyncHandler = (_, _, _, _) =>
            Task.FromResult(new ReadResult(ReadStatus.Empty, Array.Empty<string[]>()));

        // Act
        await _vm.LoadAsync(path);

        // Assert
        Assert.That(_vm.LastReadStatus, Is.EqualTo(ReadStatus.Empty));
        Assert.That(_vm.SelectedLogPath, Is.EqualTo("empty.log"));
        var view = (ListCollectionView)_vm.LogEntriesView;
        Assert.That(view.Count, Is.EqualTo(0));
    }

    [Test]
    public void LoadLatestCommand_NoActiveFile_SetsNotFoundStatus()
    {
        // Arrange
        _svc.ActiveLogFileToReturn = null; // VM ruft GetActiveLogFile() auf

        // Act
        _vm.LoadLatestCommand.Execute(null);

        // Assert: VM setzt direkt Status, kein LoadAsync-Aufruf
        Assert.That(_vm.LastReadStatus, Is.EqualTo(ReadStatus.NotFound));
        Assert.That(_vm.StatusMessage, Is.EqualTo("No logfile found."));
    }

    [Test]
    public void BrowseLogCommand_UserCancels_NoChangeInEntries()
    {
        // Arrange
        _svc.PickLogFileToReturn = null;

        // Act
        _vm.BrowseLogCommand.Execute(null);

        // Assert
        var view = (ListCollectionView)_vm.LogEntriesView;
        Assert.That(view.Count, Is.EqualTo(0));
        Assert.That(_vm.SelectedLogPath, Is.Null);
    }

    [Test]
    public void BrowseLogCommand_WithFile_LoadsEntriesAndSetsSelectedPath()
    {
        // Arrange
        _svc.PickLogFileToReturn = @"C:\logs\a.log";
        _svc.ReadPartsAsyncHandler = (_, _, _, _) =>
            Task.FromResult(new ReadResult(ReadStatus.Ok, SampleParts(1)));

        // Act
        _vm.BrowseLogCommand.Execute(null);

        // Assert
        Assert.That(_vm.LastReadStatus, Is.EqualTo(ReadStatus.Ok));
        Assert.That(_vm.SelectedLogPath, Is.EqualTo("a.log"));
        var view = (ListCollectionView)_vm.LogEntriesView;
        Assert.That(view.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task IsLoading_Toggles_TrueDuringLoad_FalseAfter()
    {
        // Arrange – lange laufender Read
        var tcs = new TaskCompletionSource<ReadResult>();
        _svc.ReadPartsAsyncHandler = (_, _, _, _) => tcs.Task;
        var path = @"C:\logs\slow.log";

        // Act
        var loadTask = _vm.LoadAsync(path);

        // Während ReadPartsAsync noch läuft
        Assert.That(_vm.IsLoading, Is.True);

        // Abschluss simulieren
        tcs.SetResult(new ReadResult(ReadStatus.Ok, SampleParts(1)));
        await loadTask;

        // Assert – wieder false
        Assert.That(_vm.IsLoading, Is.False);
    }

    [Test]
    public void LoadLatestCommand_CanExecute_IsFalseWhileRunning()
    {
        // Arrange – ReadPartsAsync blockiert, sodass VM im Loading-Zustand bleibt
        var tcs = new TaskCompletionSource<ReadResult>();
        _svc.ReadPartsAsyncHandler = (_, _, _, _) => tcs.Task;
        _svc.ActiveLogFileToReturn = @"C:\logs\active.log";

        // Act – Start
        _vm.LoadLatestCommand.Execute(null);

        // Assert – Während Ausführung sollte CanExecute false sein
        Assert.That(_vm.LoadLatestCommand.CanExecute(null), Is.False);

        // Cleanup – Task abschließen, damit VM auslädt
        tcs.SetResult(new ReadResult(ReadStatus.Ok, SampleParts(1)));
    }

    // ---- Helpers ----

    private static IEnumerable<string[]> SampleParts(int count)
    {
        if (count >= 1)
            yield return new[] { "2024-01-01 12:00:00", "INFO", "Foo", "Bar", "Line1", "Message1" };
        if (count >= 2)
            yield return new[] { "2024-01-01 12:00:01", "WARN", "Foo", "Baz", "", "Only message" };
    }

    private sealed class FakeLogService : ILogService
    {
        public string LogDirectory { get; set; } = @"C:\logs";
        public string? ActiveLogFileToReturn { get; set; }
        public string? PickLogFileToReturn { get; set; }

        public Func<string, string, int, CancellationToken, Task<ReadResult>>? ReadPartsAsyncHandler { get; set; }

        public string? GetActiveLogFile() => ActiveLogFileToReturn;

        public string? GetLatestLogPath() => @"C:\logs\latest.log";

        public string? PickLogFile() => PickLogFileToReturn;

        public Task<ReadResult> ReadPartsAsync(string path, string delimiter, int markerIndex, CancellationToken ct)
            => ReadPartsAsyncHandler?.Invoke(path, delimiter, markerIndex, ct)
               ?? Task.FromResult(new ReadResult(ReadStatus.Ok, Array.Empty<string[]>()));
    }
}