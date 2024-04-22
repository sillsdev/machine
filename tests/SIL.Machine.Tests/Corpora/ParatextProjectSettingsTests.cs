using System.Text;
using NUnit.Framework;
using SIL.Scripture;

namespace SIL.Machine.Corpora;

[TestFixture]
public class ParatextProjectSettingsTests
{
    [Test]
    public void GetBookFileName_BookNum()
    {
        ParatextProjectSettings settings = CreateSettings("41");
        Assert.That(settings.GetBookFileName("MRK"), Is.EqualTo("PROJ42.SFM"));
    }

    [Test]
    public void GetBookFileName_BookNumBookId()
    {
        ParatextProjectSettings settings = CreateSettings("41MAT");
        Assert.That(settings.GetBookFileName("MRK"), Is.EqualTo("PROJ42MRK.SFM"));
    }

    [Test]
    public void GetBookFileName_BookId()
    {
        ParatextProjectSettings settings = CreateSettings("MAT");
        Assert.That(settings.GetBookFileName("MRK"), Is.EqualTo("PROJMRK.SFM"));
    }

    [Test]
    public void GetBookFileName_BookNumDoubleDigit()
    {
        ParatextProjectSettings settings = CreateSettings("41");
        Assert.That(settings.GetBookFileName("GEN"), Is.EqualTo("PROJ01.SFM"));
    }

    [Test]
    public void GetBookFileName_BookNumXXG()
    {
        ParatextProjectSettings settings = CreateSettings("41");
        Assert.That(settings.GetBookFileName("XXG"), Is.EqualTo("PROJ100.SFM"));
    }

    [Test]
    public void GetBookFileName_BookNumPrefixA()
    {
        ParatextProjectSettings settings = CreateSettings("41");
        Assert.That(settings.GetBookFileName("FRT"), Is.EqualTo("PROJA0.SFM"));
    }

    [Test]
    public void GetBookFileName_BookNumPrefixB()
    {
        ParatextProjectSettings settings = CreateSettings("41");
        Assert.That(settings.GetBookFileName("TDX"), Is.EqualTo("PROJB0.SFM"));
    }

    [Test]
    public void GetBookFileName_BookNumPrefixC()
    {
        ParatextProjectSettings settings = CreateSettings("41");
        Assert.That(settings.GetBookFileName("3MQ"), Is.EqualTo("PROJC0.SFM"));
    }

    [Test]
    public void IsBookFileName_BookNum()
    {
        ParatextProjectSettings settings = CreateSettings("41");
        Assert.That(settings.IsBookFileName("PROJ42.SFM", out string bookId), Is.True);
        Assert.That(bookId, Is.EqualTo("MRK"));
    }

    [Test]
    public void IsBookFileName_BookNumBookId()
    {
        ParatextProjectSettings settings = CreateSettings("41MAT");
        Assert.That(settings.IsBookFileName("PROJ42MRK.SFM", out string bookId), Is.True);
        Assert.That(bookId, Is.EqualTo("MRK"));
    }

    [Test]
    public void IsBookFileName_BookId()
    {
        ParatextProjectSettings settings = CreateSettings("MAT");
        Assert.That(settings.IsBookFileName("PROJMRK.SFM", out string bookId), Is.True);
        Assert.That(bookId, Is.EqualTo("MRK"));
    }

    [Test]
    public void IsBookFileName_BookNumDoubleDigit()
    {
        ParatextProjectSettings settings = CreateSettings("41");
        Assert.That(settings.IsBookFileName("PROJ01.SFM", out string bookId), Is.True);
        Assert.That(bookId, Is.EqualTo("GEN"));
    }

    [Test]
    public void IsBookFileName_BookNumXXG()
    {
        ParatextProjectSettings settings = CreateSettings("41");
        Assert.That(settings.IsBookFileName("PROJ100.SFM", out string bookId), Is.True);
        Assert.That(bookId, Is.EqualTo("XXG"));
    }

    [Test]
    public void IsBookFileName_BookNumPrefixA()
    {
        ParatextProjectSettings settings = CreateSettings("41");
        Assert.That(settings.IsBookFileName("PROJA0.SFM", out string bookId), Is.True);
        Assert.That(bookId, Is.EqualTo("FRT"));
    }

    [Test]
    public void IsBookFileName_BookNumPrefixB()
    {
        ParatextProjectSettings settings = CreateSettings("41");
        Assert.That(settings.IsBookFileName("PROJB0.SFM", out string bookId), Is.True);
        Assert.That(bookId, Is.EqualTo("TDX"));
    }

    [Test]
    public void IsBookFileName_BookNumPrefixC()
    {
        ParatextProjectSettings settings = CreateSettings("41");
        Assert.That(settings.IsBookFileName("PROJC0.SFM", out string bookId), Is.True);
        Assert.That(bookId, Is.EqualTo("3MQ"));
    }

    [Test]
    public void IsBookFileName_WrongPrefix()
    {
        ParatextProjectSettings settings = CreateSettings("41");
        Assert.That(settings.IsBookFileName("WRONG42.SFM", out _), Is.False);
    }

    [Test]
    public void IsBookFileName_WrongSuffix()
    {
        ParatextProjectSettings settings = CreateSettings("41");
        Assert.That(settings.IsBookFileName("PROJ42.TXT", out _), Is.False);
    }

    [Test]
    public void IsBookFileName_WrongBookPart()
    {
        ParatextProjectSettings settings = CreateSettings("41");
        Assert.That(settings.IsBookFileName("PROJ42MRK.SFM", out _), Is.False);
    }

    private static ParatextProjectSettings CreateSettings(string fileNameForm)
    {
        return new ParatextProjectSettings(
            "Name",
            "Name",
            Encoding.UTF8,
            ScrVers.English,
            new UsfmStylesheet("usfm.sty"),
            "PROJ",
            fileNameForm,
            ".SFM",
            "Major",
            "",
            "BiblicalTerms.xml"
        );
    }
}
