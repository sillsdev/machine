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

        var lines = corpus.ExtractScripture().ToList();
        Assert.That(lines, Has.Count.EqualTo(41899));

        (string text, VerseRef origRef, VerseRef? corpusRef) = lines[0];
        Assert.That(text, Is.EqualTo(""));
        Assert.That(origRef, Is.EqualTo(new VerseRef("GEN 1:1", ScrVers.Original)));
        Assert.That(corpusRef, Is.EqualTo(new VerseRef("GEN 1:1", corpus.Versification)));

        (text, origRef, corpusRef) = lines[3167];
        Assert.That(text, Is.EqualTo("Chapter fourteen, verse fifty-five. Segment b."));
        Assert.That(origRef, Is.EqualTo(new VerseRef("LEV 14:56", ScrVers.Original)));
        Assert.That(corpusRef, Is.EqualTo(new VerseRef("LEV 14:55", corpus.Versification)));

        (text, origRef, corpusRef) = lines[10726];
        Assert.That(text, Is.EqualTo("Chapter twelve, verses three through seven."));
        Assert.That(origRef, Is.EqualTo(new VerseRef("1CH 12:3", ScrVers.Original)));
        Assert.That(corpusRef, Is.EqualTo(new VerseRef("1CH 12:3", corpus.Versification)));

        (text, origRef, corpusRef) = lines[10727];
        Assert.That(text, Is.EqualTo("<range>"));
        Assert.That(origRef, Is.EqualTo(new VerseRef("1CH 12:4", ScrVers.Original)));
        Assert.That(corpusRef, Is.EqualTo(new VerseRef("1CH 12:4", corpus.Versification)));

        (text, origRef, corpusRef) = lines[10731];
        Assert.That(text, Is.EqualTo("<range>"));
        Assert.That(origRef, Is.EqualTo(new VerseRef("1CH 12:8", ScrVers.Original)));
        Assert.That(corpusRef, Is.EqualTo(new VerseRef("1CH 12:7", corpus.Versification)));

        (text, origRef, corpusRef) = lines[10732];
        Assert.That(text, Is.EqualTo("Chapter twelve, verse eight."));
        Assert.That(origRef, Is.EqualTo(new VerseRef("1CH 12:9", ScrVers.Original)));
        Assert.That(corpusRef, Is.EqualTo(new VerseRef("1CH 12:8", corpus.Versification)));

        (text, origRef, corpusRef) = lines[23213];
        Assert.That(text, Is.EqualTo("Chapter one, verse one."));
        Assert.That(origRef, Is.EqualTo(new VerseRef("MAT 1:1", ScrVers.Original)));
        Assert.That(corpusRef, Is.EqualTo(new VerseRef("MAT 1:1", corpus.Versification)));

        (text, origRef, corpusRef) = lines[23240];
        Assert.That(text, Is.EqualTo("<range>"));
        Assert.That(origRef, Is.EqualTo(new VerseRef("MAT 2:3", ScrVers.Original)));
        Assert.That(corpusRef, Is.EqualTo(new VerseRef("MAT 2:3", corpus.Versification)));

        (text, origRef, corpusRef) = lines[23248];
        Assert.That(text, Is.EqualTo(""));
        Assert.That(origRef, Is.EqualTo(new VerseRef("MAT 2:11", ScrVers.Original)));
        Assert.That(corpusRef, Is.EqualTo(new VerseRef("MAT 2:11", corpus.Versification)));

        (text, origRef, corpusRef) = lines[23249];
        Assert.That(text, Is.EqualTo(""));
        Assert.That(origRef, Is.EqualTo(new VerseRef("MAT 2:12", ScrVers.Original)));
        Assert.That(corpusRef, Is.EqualTo(new VerseRef("MAT 2:12", corpus.Versification)));
    }

    [Test]
    public void MergedCorpus_SelectFirst()
    {
        var corpus1 = new DictionaryTextCorpus(
            new MemoryText("text1", new[] { TextRow("text1", 1, "source 1 segment 1 ."), TextRow("text1", 3) })
        );
        var corpus2 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 1, "source 2 segment 1 ."),
                    TextRow("text1", 2, "source 2 segment 2 ."),
                    TextRow("text1", 3)
                }
            )
        );
        var corpus3 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 1, "source 3 segment 1 ."),
                    TextRow("text1", 2, "source 3 segment 2 ."),
                    TextRow("text1", 3, "source 3 segment 3 .")
                }
            )
        );
        var nParallelCorpus = new NParallelTextCorpus([corpus1, corpus2, corpus3]) { AllRowsList = [true, true, true] };
        var mergedCorpus = nParallelCorpus.ChooseFirst();
        var rows = mergedCorpus.ToArray();
        Assert.That(rows, Has.Length.EqualTo(3), JsonSerializer.Serialize(rows));
        Assert.That(rows[0].Text, Is.EqualTo("source 1 segment 1 ."));
        Assert.That(rows[1].Text, Is.EqualTo("source 2 segment 2 ."));
        Assert.That(rows[2].Text, Is.EqualTo("source 3 segment 3 ."));
    }

    [Test]
    public void MergedCorpus_SelectRandom_Seed123456()
    {
        var corpus1 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 1, "source 1 segment 1 ."),
                    TextRow("text1", 2, "source 1 segment 2 ."),
                    TextRow("text1", 3, "source 1 segment 3 .")
                }
            )
        );
        var corpus2 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 1, "source 2 segment 1 ."),
                    TextRow("text1", 2, "source 2 segment 2 ."),
                    TextRow("text1", 3, "source 2 segment 3 .")
                }
            )
        );
        var corpus3 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 1, "source 3 segment 1 ."),
                    TextRow("text1", 2, "source 3 segment 2 ."),
                    TextRow("text1", 3, "source 3 segment 3 .")
                }
            )
        );
        var nParallelCorpus = new NParallelTextCorpus([corpus1, corpus2, corpus3]) { AllRowsList = [true, true, true] };
        var mergedCorpus = nParallelCorpus.ChooseRandom(123456);
        var rows = mergedCorpus.ToArray();
        Assert.That(rows, Has.Length.EqualTo(3), JsonSerializer.Serialize(rows));
        Assert.Multiple(() =>
        {
            Assert.That(rows[0].Text, Is.EqualTo("source 1 segment 1 ."));
            Assert.That(rows[1].Text, Is.EqualTo("source 1 segment 2 ."));
            Assert.That(rows[2].Text, Is.EqualTo("source 1 segment 3 ."));
        });
    }

    [Test]
    public void MergedCorpus_SelectRandom_Seed4501()
    {
        var corpus1 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 1, "source 1 segment 1 ."),
                    TextRow("text1", 2, "source 1 segment 2 ."),
                    TextRow("text1", 3, "source 1 segment 3 .")
                }
            )
        );
        var corpus2 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 1, "source 2 segment 1 ."),
                    TextRow("text1", 2, "source 2 segment 2 ."),
                    TextRow("text1", 3, "source 2 segment 3 .")
                }
            )
        );
        var corpus3 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 1, "source 3 segment 1 ."),
                    TextRow("text1", 2, "source 3 segment 2 ."),
                    TextRow("text1", 3, "source 3 segment 3 .")
                }
            )
        );
        var nParallelCorpus = new NParallelTextCorpus([corpus1, corpus2, corpus3]) { AllRowsList = [true, true, true] };
        var mergedCorpus = nParallelCorpus.ChooseRandom(4501);
        var rows = mergedCorpus.ToArray();
        Assert.That(rows, Has.Length.EqualTo(3), JsonSerializer.Serialize(rows));
        Assert.Multiple(() =>
        {
            Assert.That(rows[0].Text, Is.EqualTo("source 1 segment 1 ."));
            Assert.That(rows[1].Text, Is.EqualTo("source 2 segment 2 ."));
            Assert.That(rows[2].Text, Is.EqualTo("source 3 segment 3 ."));
        });
    }

    [Test]
    public void AlignMergedCorpora()
    {
        var sourceCorpus1 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 1, "source 1 segment 1 ."),
                    TextRow("text1", 2, "source 1 segment 2 ."),
                    TextRow("text1", 3, "source 1 segment 3 .")
                }
            )
        );
        var sourceCorpus2 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 1, "source 2 segment 1 ."),
                    TextRow("text1", 2, "source 2 segment 2 ."),
                    TextRow("text1", 3, "source 2 segment 3 .")
                }
            )
        );
        var sourceCorpus3 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 1, "source 3 segment 1 ."),
                    TextRow("text1", 2, "source 3 segment 2 ."),
                    TextRow("text1", 3, "source 3 segment 3 .")
                }
            )
        );

        ITextCorpus sourceCorpus = (new ITextCorpus[] { sourceCorpus1, sourceCorpus1, sourceCorpus3 })
            .AlignMany([true, true, true])
            .ChooseFirst();

        var targetCorpus1 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 1, "target 1 segment 1 ."),
                    TextRow("text1", 2, "target 1 segment 2 ."),
                    TextRow("text1", 3, "target 1 segment 3 .")
                }
            )
        );
        var targetCorpus2 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 1, "target 2 segment 1 ."),
                    TextRow("text1", 2, "target 2 segment 2 ."),
                    TextRow("text1", 3, "target 2 segment 3 .")
                }
            )
        );
        var targetCorpus3 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 1, "target 3 segment 1 ."),
                    TextRow("text1", 2, "target 3 segment 2 ."),
                    TextRow("text1", 3, "target 3 segment 3 .")
                }
            )
        );

        ITextCorpus targetCorpus = (new ITextCorpus[] { targetCorpus1, targetCorpus2, targetCorpus3 })
            .AlignMany([true, true, true])
            .ChooseFirst();

        IParallelTextCorpus alignedCorpus = sourceCorpus.AlignRows(targetCorpus);
        ParallelTextRow[] rows = alignedCorpus.GetRows().ToArray();
        Assert.That(rows, Has.Length.EqualTo(3));
        Assert.That(rows[0].SourceText, Is.EqualTo("source 1 segment 1 ."));
        Assert.That(rows[2].TargetText, Is.EqualTo("target 1 segment 3 ."));
    }

    private static TextRow TextRow(
        string textId,
        object rowRef,
        string text = "",
        TextRowFlags flags = TextRowFlags.SentenceStart
    )
    {
        return new TextRow(textId, rowRef)
        {
            Segment = text.Length == 0 ? Array.Empty<string>() : text.Split(),
            Flags = flags
        };
    }
}
