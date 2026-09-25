using System.Text.Json;
using NUnit.Framework;
using SIL.Scripture;

namespace SIL.Machine.Corpora;

[TestFixture]
public class CorporaExtensionsTests
{
    [Test]
    public void ExtractScripture()
    {
        var corpus = new ParatextTextCorpus(CorporaTestHelpers.UsfmTestProjectPath, includeAllText: true);

        List<(string Text, VerseRef RefCorpusVerseRef, VerseRef CorpusVerseRef)> lines = [.. corpus.ExtractScripture()];
        Assert.That(lines, Has.Count.EqualTo(41899));

        (string text, VerseRef origRef, VerseRef? corpusRef) = lines[0];
        using (Assert.EnterMultipleScope())
        {
            Assert.That(text, Is.EqualTo(""));
            Assert.That(origRef, Is.EqualTo(new VerseRef("GEN 1:1", ScrVers.Original)));
            Assert.That(corpusRef, Is.EqualTo(new VerseRef("GEN 1:1", corpus.Versification)));
        }

        (text, origRef, corpusRef) = lines[3167];
        using (Assert.EnterMultipleScope())
        {
            Assert.That(text, Is.EqualTo("Chapter fourteen, verse fifty-five. Segment b."));
            Assert.That(origRef, Is.EqualTo(new VerseRef("LEV 14:56", ScrVers.Original)));
            Assert.That(corpusRef, Is.EqualTo(new VerseRef("LEV 14:55", corpus.Versification)));
        }

        (text, origRef, corpusRef) = lines[10726];
        using (Assert.EnterMultipleScope())
        {
            Assert.That(text, Is.EqualTo("Chapter twelve, verses three through seven."));
            Assert.That(origRef, Is.EqualTo(new VerseRef("1CH 12:3", ScrVers.Original)));
            Assert.That(corpusRef, Is.EqualTo(new VerseRef("1CH 12:3", corpus.Versification)));
        }

        (text, origRef, corpusRef) = lines[10727];
        using (Assert.EnterMultipleScope())
        {
            Assert.That(text, Is.EqualTo("<range>"));
            Assert.That(origRef, Is.EqualTo(new VerseRef("1CH 12:4", ScrVers.Original)));
            Assert.That(corpusRef, Is.EqualTo(new VerseRef("1CH 12:4", corpus.Versification)));
        }

        (text, origRef, corpusRef) = lines[10731];
        using (Assert.EnterMultipleScope())
        {
            Assert.That(text, Is.EqualTo("<range>"));
            Assert.That(origRef, Is.EqualTo(new VerseRef("1CH 12:8", ScrVers.Original)));
            Assert.That(corpusRef, Is.EqualTo(new VerseRef("1CH 12:7", corpus.Versification)));
        }

        (text, origRef, corpusRef) = lines[10732];
        using (Assert.EnterMultipleScope())
        {
            Assert.That(text, Is.EqualTo("Chapter twelve, verse eight."));
            Assert.That(origRef, Is.EqualTo(new VerseRef("1CH 12:9", ScrVers.Original)));
            Assert.That(corpusRef, Is.EqualTo(new VerseRef("1CH 12:8", corpus.Versification)));
        }

        (text, origRef, corpusRef) = lines[23213];
        using (Assert.EnterMultipleScope())
        {
            Assert.That(text, Is.EqualTo("Chapter one, verse one."));
            Assert.That(origRef, Is.EqualTo(new VerseRef("MAT 1:1", ScrVers.Original)));
            Assert.That(corpusRef, Is.EqualTo(new VerseRef("MAT 1:1", corpus.Versification)));
        }

        (text, origRef, corpusRef) = lines[23240];
        using (Assert.EnterMultipleScope())
        {
            Assert.That(text, Is.EqualTo("<range>"));
            Assert.That(origRef, Is.EqualTo(new VerseRef("MAT 2:3", ScrVers.Original)));
            Assert.That(corpusRef, Is.EqualTo(new VerseRef("MAT 2:3", corpus.Versification)));
        }

        (text, origRef, corpusRef) = lines[23248];
        using (Assert.EnterMultipleScope())
        {
            Assert.That(text, Is.EqualTo(""));
            Assert.That(origRef, Is.EqualTo(new VerseRef("MAT 2:11", ScrVers.Original)));
            Assert.That(corpusRef, Is.EqualTo(new VerseRef("MAT 2:11", corpus.Versification)));
        }

        (text, origRef, corpusRef) = lines[23249];
        using (Assert.EnterMultipleScope())
        {
            Assert.That(text, Is.EqualTo(""));
            Assert.That(origRef, Is.EqualTo(new VerseRef("MAT 2:12", ScrVers.Original)));
            Assert.That(corpusRef, Is.EqualTo(new VerseRef("MAT 2:12", corpus.Versification)));
        }
    }

    [Test]
    public void FlattenTextCorpus()
    {
        ITextCorpus corpus1 = CorporaTestHelpers.CreateTextCorpus("text1", ["source 1", "source 2"]);
        ITextCorpus corpus2 = CorporaTestHelpers.CreateTextCorpus("text2", ["source 3"]);
        ITextCorpus corpus = new[] { corpus1, corpus2 }.Flatten();

        List<TextRow> rows = [.. corpus.GetRows()];
        Assert.That(rows, Has.Count.EqualTo(3));
        Assert.That(rows.Select(r => r.Text), Is.EqualTo(["source 1", "source 2", "source 3"]));
        // The corpus is re-iterable
        Assert.That(rows, Has.Count.EqualTo(3));
    }

    [Test]
    public void FlattenParallelTextCorpus()
    {
        IParallelTextCorpus corpus1 = CorporaTestHelpers
            .CreateTextCorpus("text1", ["source 1"])
            .AlignRows(CorporaTestHelpers.CreateTextCorpus("text1", ["target 1"]));
        IParallelTextCorpus corpus2 = CorporaTestHelpers
            .CreateTextCorpus("text2", ["source 2"])
            .AlignRows(CorporaTestHelpers.CreateTextCorpus("text2", ["target 2"]));
        IParallelTextCorpus corpus = new[] { corpus1, corpus2 }.Flatten();

        List<ParallelTextRow> rows = [.. corpus.GetRows()];
        Assert.That(rows, Has.Count.EqualTo(2));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(rows.Select(r => r.SourceText), Is.EqualTo(["source 1", "source 2"]));
            Assert.That(rows.Select(r => r.TargetText), Is.EqualTo(["target 1", "target 2"]));
        }

        // The corpus is re-iterable
        Assert.That(corpus.GetRows().Count(), Is.EqualTo(2));
    }

    [Test]
    public void FlattenParallelTextCorpusDifferentClasses()
    {
        IParallelTextCorpus corpus1 = CorporaTestHelpers
            .CreateTextCorpus("text1", ["Source 1"])
            .AlignRows(CorporaTestHelpers.CreateTextCorpus("text1", ["Target 1"]));
        IParallelTextCorpus corpus2 = CorporaTestHelpers
            .CreateTextCorpus("text2", ["Source 2"])
            .AlignRows(CorporaTestHelpers.CreateTextCorpus("text2", ["Target 2"]));
        IParallelTextCorpus corpus = new[] { corpus1, corpus2.Lowercase() }.Flatten();

        List<ParallelTextRow> rows = [.. corpus.GetRows()];
        Assert.That(rows.Select(r => r.SourceText), Is.EqualTo(["Source 1", "source 2"]));
    }

    [Test]
    public void FlattenParallelTextCorpusTextIds()
    {
        IParallelTextCorpus corpus1 = CorporaTestHelpers
            .CreateTextCorpus("text1", ["source 1"])
            .AlignRows(CorporaTestHelpers.CreateTextCorpus("text1", ["target 1"]));
        IParallelTextCorpus corpus2 = CorporaTestHelpers
            .CreateTextCorpus("text2", ["source 2"])
            .AlignRows(CorporaTestHelpers.CreateTextCorpus("text2", ["target 2"]));
        IParallelTextCorpus corpus = new[] { corpus1, corpus2 }.Flatten();

        List<ParallelTextRow> rows = [.. corpus.GetRows(["text2"])];
        Assert.That(rows.Select(r => r.SourceText), Is.EqualTo(["source 2"]));
    }

    [Test]
    public void MergedCorpus_SelectFirst()
    {
        var corpus1 = new DictionaryTextCorpus(
            new MemoryText("text1", [TextRow("text1", 1, "source 1 segment 1 ."), TextRow("text1", 3)])
        );
        var corpus2 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                [
                    TextRow("text1", 1, "source 2 segment 1 ."),
                    TextRow("text1", 2, "source 2 segment 2 ."),
                    TextRow("text1", 3),
                ]
            )
        );
        var corpus3 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                [
                    TextRow("text1", 1, "source 3 segment 1 ."),
                    TextRow("text1", 2, "source 3 segment 2 ."),
                    TextRow("text1", 3, "source 3 segment 3 ."),
                ]
            )
        );
        ITextCorpus mergedCorpus = new List<ITextCorpus> { corpus1, corpus2, corpus3 }.ChooseFirst();
        TextRow[] rows = [.. mergedCorpus];
        Assert.That(rows, Has.Length.EqualTo(3), JsonSerializer.Serialize(rows));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(rows[0].Text, Is.EqualTo("source 1 segment 1 ."));
            Assert.That(rows[1].Text, Is.EqualTo("source 2 segment 2 ."));
            Assert.That(rows[2].Text, Is.EqualTo("source 3 segment 3 ."));
        }
    }

    [Test]
    public void MergedCorpus_SelectRandom_Seed123456()
    {
        var corpus1 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                [
                    TextRow("text1", 1, "source 1 segment 1 ."),
                    TextRow("text1", 2, "source 1 segment 2 ."),
                    TextRow("text1", 3, "source 1 segment 3 ."),
                ]
            )
        );
        var corpus2 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                [
                    TextRow("text1", 1, "source 2 segment 1 ."),
                    TextRow("text1", 2, "source 2 segment 2 ."),
                    TextRow("text1", 3, "source 2 segment 3 ."),
                ]
            )
        );
        var corpus3 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                [
                    TextRow("text1", 1, "source 3 segment 1 ."),
                    TextRow("text1", 2, "source 3 segment 2 ."),
                    TextRow("text1", 3, "source 3 segment 3 ."),
                ]
            )
        );
        ITextCorpus mergedCorpus = new List<ITextCorpus> { corpus1, corpus2, corpus3 }.ChooseRandom(123456);
        TextRow[] rows = [.. mergedCorpus];
        Assert.That(rows, Has.Length.EqualTo(3), JsonSerializer.Serialize(rows));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(rows[0].Text, Is.EqualTo("source 1 segment 1 ."));
            Assert.That(rows[1].Text, Is.EqualTo("source 1 segment 2 ."));
            Assert.That(rows[2].Text, Is.EqualTo("source 1 segment 3 ."));
        }
    }

    [Test]
    public void MergedCorpus_SelectRandom_Seed4501()
    {
        var corpus1 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                [
                    TextRow("text1", 1, "source 1 segment 1 ."),
                    TextRow("text1", 2, "source 1 segment 2 ."),
                    TextRow("text1", 3, "source 1 segment 3 ."),
                ]
            )
        );
        var corpus2 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                [
                    TextRow("text1", 1, "source 2 segment 1 ."),
                    TextRow("text1", 2, "source 2 segment 2 ."),
                    TextRow("text1", 3, "source 2 segment 3 ."),
                ]
            )
        );
        var corpus3 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                [
                    TextRow("text1", 1, "source 3 segment 1 ."),
                    TextRow("text1", 2, "source 3 segment 2 ."),
                    TextRow("text1", 3, "source 3 segment 3 ."),
                ]
            )
        );
        ITextCorpus mergedCorpus = new List<ITextCorpus> { corpus1, corpus2, corpus3 }.ChooseRandom(4501);
        TextRow[] rows = [.. mergedCorpus];
        Assert.That(rows, Has.Length.EqualTo(3), JsonSerializer.Serialize(rows));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(rows[0].Text, Is.EqualTo("source 1 segment 1 ."));
            Assert.That(rows[1].Text, Is.EqualTo("source 2 segment 2 ."));
            Assert.That(rows[2].Text, Is.EqualTo("source 3 segment 3 ."));
        }
    }

    [Test]
    public void AlignMergedCorpora()
    {
        var sourceCorpus1 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                [
                    TextRow("text1", 1, "source 1 segment 1 ."),
                    TextRow("text1", 2, "source 1 segment 2 ."),
                    TextRow("text1", 3, "source 1 segment 3 ."),
                ]
            )
        );
        var sourceCorpus2 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                [
                    TextRow("text1", 1, "source 2 segment 1 ."),
                    TextRow("text1", 2, "source 2 segment 2 ."),
                    TextRow("text1", 3, "source 2 segment 3 ."),
                ]
            )
        );
        var sourceCorpus3 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                [
                    TextRow("text1", 1, "source 3 segment 1 ."),
                    TextRow("text1", 2, "source 3 segment 2 ."),
                    TextRow("text1", 3, "source 3 segment 3 ."),
                ]
            )
        );

        ITextCorpus sourceCorpus = new List<ITextCorpus> { sourceCorpus1, sourceCorpus2, sourceCorpus3 }.ChooseFirst();

        var targetCorpus1 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                [
                    TextRow("text1", 1, "target 1 segment 1 ."),
                    TextRow("text1", 2, "target 1 segment 2 ."),
                    TextRow("text1", 3, "target 1 segment 3 ."),
                ]
            )
        );
        var targetCorpus2 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                [
                    TextRow("text1", 1, "target 2 segment 1 ."),
                    TextRow("text1", 2, "target 2 segment 2 ."),
                    TextRow("text1", 3, "target 2 segment 3 ."),
                ]
            )
        );
        var targetCorpus3 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                [
                    TextRow("text1", 1, "target 3 segment 1 ."),
                    TextRow("text1", 2, "target 3 segment 2 ."),
                    TextRow("text1", 3, "target 3 segment 3 ."),
                ]
            )
        );

        ITextCorpus targetCorpus = new List<ITextCorpus> { targetCorpus1, targetCorpus2, targetCorpus3 }.ChooseFirst();

        IParallelTextCorpus alignedCorpus = sourceCorpus.AlignRows(targetCorpus);
        ParallelTextRow[] rows = [.. alignedCorpus.GetRows()];
        Assert.That(rows, Has.Length.EqualTo(3));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(rows[0].SourceText, Is.EqualTo("source 1 segment 1 ."));
            Assert.That(rows[2].TargetText, Is.EqualTo("target 1 segment 3 ."));
        }
    }

    private static TextRow TextRow(
        string textId,
        object rowRef,
        string text = "",
        TextRowFlags flags = TextRowFlags.SentenceStart
    ) => new(textId, rowRef) { Segment = text.Length == 0 ? Array.Empty<string>() : text.Split(), Flags = flags };
}
