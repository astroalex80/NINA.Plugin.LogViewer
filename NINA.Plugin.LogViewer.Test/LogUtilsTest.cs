using System;
using System.IO;
using System.Text.RegularExpressions;
using NINA.Plugin.Logviewer;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NINA.Plugin.Logviewer.Tests;

[TestFixture]
public class LogUtilsTests
{
    [Test]
    public void GetActiveLogFileRE_MatchesVersionPidPattern()
    {
        // Regex: yyyyMMdd-HHmmss-VERSION.PID-yyyyMM(log/rollover)
        var re = new Regex(LogUtils.GetActiveLogFileRE(), RegexOptions.Compiled);
        // Wir konstruieren einen Namen mit aktueller Version/ProcessId, damit der Test robust bleibt
        var version = NINA.Core.Utility.CoreUtil.Version;
        var pid = Environment.ProcessId;
        var name = $"20250101-120000-{version}.{pid}-202501.log";

        Assert.That(re.IsMatch(name), Is.True, "constructed name should match active log regex");
        Assert.That(re.IsMatch("random.log"), Is.False);
    }

    [Test]
    public void GetLogDirectory_EndsWith_Logs()
    {
        var dir = LogUtils.GetLogDirectory();
        Assert.That(dir, Does.EndWith(Path.DirectorySeparatorChar + "Logs"));
    }

    [Test]
    public void GetActiveLogFile_FindsMatchingFile_InDirectory()
    {
        using var tmp = new TempDir();

        // tow files one regex mismatch
        var version = NINA.Core.Utility.CoreUtil.Version;
        var pid = Environment.ProcessId;

        var matching = Path.Combine(tmp.Path, $"20250101-120000-{version}.{pid}-202501.log");
        var nonMatching = Path.Combine(tmp.Path, "other.log");

        File.WriteAllText(matching, "x");
        File.WriteAllText(nonMatching, "y");

        var found = LogUtils.GetActiveLogFile(tmp.Path);
        Assert.That(found, Is.EqualTo(matching));
    }

    [Test]
    public void GetActiveLogFile_NoMatch_ReturnsEmptyString()
    {
        using var tmp = new TempDir();
        File.WriteAllText(Path.Combine(tmp.Path, "foo.log"), "x");

        var found = LogUtils.GetActiveLogFile(tmp.Path);
        Assert.That(found, Is.EqualTo("")); // returns "" not null
    }

    private sealed class TempDir : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"logutils_{Guid.NewGuid():N}");

        public TempDir() => Directory.CreateDirectory(Path);

        public void Dispose()
        { try { Directory.Delete(Path, true); } catch { /* ignore */ } }
    }
}