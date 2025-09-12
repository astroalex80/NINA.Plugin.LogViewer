using NUnit.Framework;
using System;

namespace NINA.Plugin.Logviewer.Tests;

[TestFixture]
public class FiltersTests
{
    private static LogRow Row(DateTime? dt = null, string lvl = "INFO", string src = "Core", string mem = "DoWork", string msg = "Hello World")
        => new LogRow { Date = dt ?? new DateTime(2025, 1, 2, 3, 4, 5, 678), Level = lvl, Source = src, Member = mem, Message = msg };

    [Test]
    public void Matches_Default_NoFilters_ReturnsTrue()
    {
        var f = new Filters();
        Assert.That(f.Matches(Row()), Is.True); // keine Filter aktiv → passt
    }

    [Test]
    public void Matches_HideInfo_FiltersInfoOut()
    {
        var f = new Filters { HideInfo = true };
        Assert.That(f.Matches(Row(lvl: "INFO")), Is.False); // INFO wird ausgeblendet
        Assert.That(f.Matches(Row(lvl: "WARN")), Is.True);
    }

    [Test]
    public void Matches_LevelFilter_MustContainSubstring_CaseInsensitive()
    {
        var f = new Filters { LevelFilter = "war" };
        Assert.That(f.Matches(Row(lvl: "WARN")), Is.True);
        Assert.That(f.Matches(Row(lvl: "INFO")), Is.False);
    }

    [Test]
    public void Matches_Source_Member_Message_Filters()
    {
        var f = new Filters { SourceFilter = "cor", MemberFilter = "work", MessageFilter = "hello" };
        Assert.That(f.Matches(Row(src: "CORE", mem: "DoWork", msg: "Hello World")), Is.True);
        Assert.That(f.Matches(Row(src: "Other", mem: "Do", msg: "World")), Is.False);
    }

    [Test]
    public void Matches_DateFilter_UsesFormattedString()
    {
        // Filters formatiert: "yyyy-MM-dd  HH:mm:ss.ffff" (beachte 2 Spaces vor der Uhrzeit) :contentReference[oaicite:3]{index=3}
        var row = Row(new DateTime(2025, 9, 11, 20, 15, 30, 0));
        var f = new Filters { DateFilter = "2025-09-11  20:15" };
        Assert.That(f.Matches(row), Is.True);

        f.DateFilter = "2025-09-10";
        Assert.That(f.Matches(row), Is.False);
    }
}