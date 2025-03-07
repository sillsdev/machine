using System.Text.Json;
using NUnit.Framework;

namespace SIL.Machine.Corpora;

[TestFixture]
public class NParallelTextCorpusTests
{
    [Test]
    public void GetRows_ZeroCorpora()
    {
        var nParallelCorpus = new NParallelTextCorpus([]);
        NParallelTextRow[] rows = nParallelCorpus.ToArray();
        Assert.That(rows.Length, Is.EqualTo(0));
    }

    [Test]
    public void GetRows_ThreeCorpora()
    {
        var corpus1 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 1, "source segment 1 .", TextRowFlags.None),
                    TextRow("text1", 2, "source segment 2 ."),
                    TextRow("text1", 3, "source segment 3 .")
                }
            )
        );
        var corpus2 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 1, "source segment 1 ."),
                    TextRow("text1", 2, "source segment 2 ."),
                    TextRow("text1", 3, "source segment 3 .", TextRowFlags.None)
                }
            )
        );
        var corpus3 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 1, "source segment 1 ."),
                    TextRow("text1", 2, "source segment 2 .", TextRowFlags.None),
                    TextRow("text1", 3, "source segment 3 .")
                }
            )
        );
        var nParallelCorpus = new NParallelTextCorpus([corpus1, corpus2, corpus3]);
        NParallelTextRow[] rows = nParallelCorpus.ToArray();
        Assert.That(rows.Length, Is.EqualTo(3));
        Assert.That(rows[0].NRefs.All(r => (int)r[0] == 1));
        Assert.That(rows[0].NSegments.All(r => r.SequenceEqual("source segment 1 .".Split())));
        Assert.That(rows[0].IsSentenceStart(0), Is.False);
        Assert.That(rows[0].IsSentenceStart(1), Is.True);
        Assert.That(rows[2].NRefs.All(r => (int)r[0] == 3));
        Assert.That(rows[2].NSegments.All(r => r.SequenceEqual("source segment 3 .".Split())));
        Assert.That(rows[2].IsSentenceStart(1), Is.False);
        Assert.That(rows[2].IsSentenceStart(2), Is.True);
    }

    [Test]
    public void GetRows_ThreeCorpora_MissingRows()
    {
        var corpus1 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 1, "source segment 1 .", TextRowFlags.None),
                    TextRow("text1", 3, "source segment 3 .")
                }
            )
        );
        var corpus2 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 2, "source segment 2 ."),
                    TextRow("text1", 3, "source segment 3 .", TextRowFlags.None)
                }
            )
        );
        var corpus3 = new DictionaryTextCorpus(
            new MemoryText("text1", new[] { TextRow("text1", 3, "source segment 3 .") })
        );
        var nParallelCorpus = new NParallelTextCorpus([corpus1, corpus2, corpus3]);
        NParallelTextRow[] rows = nParallelCorpus.ToArray();
        Assert.That(rows.Length, Is.EqualTo(1));
        Assert.That(rows[0].NRefs.All(r => (int)r[0] == 3));
        Assert.That(rows[0].NSegments.All(r => r.SequenceEqual("source segment 3 .".Split())));
        Assert.That(rows[0].IsSentenceStart(0), Is.True);
        Assert.That(rows[0].IsSentenceStart(1), Is.False);
    }

    [Test]
    public void GetRows_ThreeCorpora_MissingRows_AllAllRows()
    {
        var corpus1 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 1, "source segment 1 .", TextRowFlags.None),
                    TextRow("text1", 3, "source segment 3 .")
                }
            )
        );
        var corpus2 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 2, "source segment 2 ."),
                    TextRow("text1", 3, "source segment 3 .", TextRowFlags.None)
                }
            )
        );
        var corpus3 = new DictionaryTextCorpus(
            new MemoryText("text1", new[] { TextRow("text1", 3, "source segment 3 .") })
        );
        var nParallelCorpus = new NParallelTextCorpus([corpus1, corpus2, corpus3]) { AllRows = [true, true, true] };
        NParallelTextRow[] rows = nParallelCorpus.ToArray();
        Assert.That(rows.Length, Is.EqualTo(3));
        Assert.That(rows[2].NRefs.All(r => (int)r[0] == 3));
        Assert.That(rows[2].NSegments.All(r => r.SequenceEqual("source segment 3 .".Split())));
        Assert.That(rows[2].IsSentenceStart(0), Is.True);
        Assert.That(rows[2].IsSentenceStart(1), Is.False);
    }

    [Test]
    public void GetRows_ThreeCorpora_MissingRows_SomeAllRows()
    {
        var corpus1 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 1, "source segment 1 .", TextRowFlags.None),
                    TextRow("text1", 3, "source segment 3 .")
                }
            )
        );
        var corpus2 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 2, "source segment 2 ."),
                    TextRow("text1", 3, "source segment 3 .", TextRowFlags.None)
                }
            )
        );
        var corpus3 = new DictionaryTextCorpus(
            new MemoryText("text1", new[] { TextRow("text1", 3, "source segment 3 .") })
        );
        var nParallelCorpus = new NParallelTextCorpus([corpus1, corpus2, corpus3]) { AllRows = [true, false, true] };
        NParallelTextRow[] rows = nParallelCorpus.ToArray();
        Assert.That(rows.Length, Is.EqualTo(2));
        Assert.That(rows[1].NRefs.All(r => (int)r[0] == 3));
        Assert.That(rows[1].NSegments.All(r => r.SequenceEqual("source segment 3 .".Split())));
        Assert.That(rows[1].IsSentenceStart(0), Is.True);
        Assert.That(rows[1].IsSentenceStart(1), Is.False);
    }

    [Test]
    public void GetRows_ThreeCorpora_MissingRows_AllAllRows_MissingMiddle()
    {
        var corpus1 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 1, "source segment 1 .", TextRowFlags.None),
                    TextRow("text1", 3, "source segment 3 .")
                }
            )
        );
        var corpus2 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 1, "source segment 1 .", TextRowFlags.None),
                    TextRow("text1", 2, "source segment 2 ."),
                    TextRow("text1", 3, "source segment 3 .", TextRowFlags.None)
                }
            )
        );
        var corpus3 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 1, "source segment 1 ."),
                    TextRow("text1", 2, "source segment 2 ."),
                    TextRow("text1", 3, "source segment 3 .", TextRowFlags.None)
                }
            )
        );
        var nParallelCorpus = new NParallelTextCorpus([corpus1, corpus2, corpus3]) { AllRows = [true, true, true] };
        NParallelTextRow[] rows = nParallelCorpus.ToArray();
        Assert.That(rows.Length, Is.EqualTo(3));
        Assert.That(rows[1].NRefs.All(r => r.Count == 0 || (int)r[0] == 2));
        Assert.That(rows[1].NSegments.All(r => r.Count == 0 || r.SequenceEqual("source segment 2 .".Split())));
        Assert.That(rows[1].IsSentenceStart(1), Is.True);
    }

    [Test]
    public void GetRows_ThreeCorpora_MissingRows_MissingLastRows()
    {
        var corpus1 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 1, "source segment 1 .", TextRowFlags.None),
                    TextRow("text1", 2, "source segment 2 ."),
                    TextRow("text1", 3, "source segment 3 .")
                }
            )
        );
        var corpus2 = new DictionaryTextCorpus(
            new MemoryText("text1", new[] { TextRow("text1", 1, "source segment 1 ."), })
        );
        var corpus3 = new DictionaryTextCorpus(
            new MemoryText("text1", new[] { TextRow("text1", 1, "source segment 1 .") })
        );
        var nParallelCorpus = new NParallelTextCorpus([corpus1, corpus2, corpus3]) { AllRows = [true, false, false] };
        NParallelTextRow[] rows = nParallelCorpus.ToArray();
        Assert.That(rows.Length, Is.EqualTo(3));
        Assert.That(rows[1].NRefs.All(r => r.Count == 0 || (int)r[0] == 2));
        Assert.That(rows[1].NSegments.All(r => r.Count == 0 || r.SequenceEqual("source segment 2 .".Split())));
        Assert.That(rows[1].IsSentenceStart(0), Is.True);
    }

    [Test]
    public void GetRows_OneCorpus()
    {
        var corpus1 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 1, "source segment 1 .", TextRowFlags.None),
                    TextRow("text1", 3, "source segment 3 .")
                }
            )
        );
        var nParallelCorpus = new NParallelTextCorpus([corpus1]) { AllRows = [true] };
        NParallelTextRow[] rows = nParallelCorpus.ToArray();
        Assert.That(rows.Length, Is.EqualTo(2));
        Assert.That(rows[0].NRefs.All(r => (int)r[0] == 1));
        Assert.That(rows[0].NSegments.All(r => r.SequenceEqual("source segment 1 .".Split())));
        Assert.That(rows[0].IsSentenceStart(0), Is.False);
    }

    [Test]
    public void GetRows_ThreeCorpora_Range()
    {
        var corpus1 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 1, "source segment 1 ."),
                    TextRow(
                        "text1",
                        2,
                        "source segment 2 . source segment 3 .",
                        TextRowFlags.InRange | TextRowFlags.RangeStart
                    ),
                    TextRow("text1", 3, flags: TextRowFlags.InRange),
                    TextRow("text1", 4, "source segment 4 .")
                }
            )
        );
        var corpus2 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 1, "source segment 1 ."),
                    TextRow("text1", 2, "source segment 2 ."),
                    TextRow("text1", 3, "source segment 3 ."),
                    TextRow("text1", 4, "source segment 4 .")
                }
            )
        );
        var corpus3 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 1, "source segment 1 ."),
                    TextRow("text1", 2, "source segment 2 ."),
                    TextRow("text1", 3, "source segment 3 ."),
                    TextRow("text1", 4, "source segment 4 .")
                }
            )
        );
        var nParallelCorpus = new NParallelTextCorpus([corpus1, corpus2, corpus3]);
        NParallelTextRow[] rows = nParallelCorpus.ToArray();
        Assert.That(rows.Length, Is.EqualTo(3));
        Assert.That(rows[1].NRefs.All(r => r.SequenceEqual([2, 3])));
        Assert.That(rows[1].NSegments[0], Is.EqualTo("source segment 2 . source segment 3 .".Split()));
    }

    [Test]
    public void GetRows_ThreeCorpora_OverlappingRanges()
    {
        var corpus1 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 1, "source segment 1 ."),
                    TextRow(
                        "text1",
                        2,
                        "source segment 2 . source segment 3 .",
                        TextRowFlags.SentenceStart | TextRowFlags.InRange | TextRowFlags.RangeStart
                    ),
                    TextRow("text1", 3, flags: TextRowFlags.InRange)
                }
            )
        );
        var corpus2 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow(
                        "text1",
                        1,
                        "source segment 1 . source segment 2 .",
                        TextRowFlags.SentenceStart | TextRowFlags.InRange | TextRowFlags.RangeStart
                    ),
                    TextRow("text1", 2, flags: TextRowFlags.InRange),
                    TextRow("text1", 3, "source segment 3 .")
                }
            )
        );
        var corpus3 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 1, "source segment 1 ."),
                    TextRow("text1", 2, "source segment 2 ."),
                    TextRow("text1", 3, "source segment 3 .")
                }
            )
        );
        var nParallelCorpus = new NParallelTextCorpus([corpus1, corpus2, corpus3]);
        NParallelTextRow[] rows = nParallelCorpus.ToArray();
        Assert.That(rows.Length, Is.EqualTo(1), JsonSerializer.Serialize(rows));
    }

    [Test]
    public void GetRows_ThreeCorpora_OverlappingRanges_AllIndividualRows()
    {
        var corpus1 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 1, "source segment 1 ."),
                    TextRow(
                        "text1",
                        2,
                        "source segment 2 . source segment 3 .",
                        TextRowFlags.SentenceStart | TextRowFlags.InRange | TextRowFlags.RangeStart
                    ),
                    TextRow("text1", 3, flags: TextRowFlags.InRange)
                }
            )
        );
        var corpus2 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow(
                        "text1",
                        1,
                        "source segment 1 . source segment 2 .",
                        TextRowFlags.SentenceStart | TextRowFlags.InRange | TextRowFlags.RangeStart
                    ),
                    TextRow("text1", 2, flags: TextRowFlags.InRange),
                    TextRow("text1", 3, "source segment 3 .")
                }
            )
        );
        var corpus3 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 1, "source segment 1 ."),
                    TextRow("text1", 2, "source segment 2 ."),
                    TextRow("text1", 3, "source segment 3 .")
                }
            )
        );
        var nParallelCorpus = new NParallelTextCorpus([corpus1, corpus2, corpus3]) { AllRows = [false, false, true] };
        NParallelTextRow[] rows = nParallelCorpus.ToArray();
        Assert.That(rows.Length, Is.EqualTo(3), JsonSerializer.Serialize(rows));
        Assert.That(rows[0].NRefs[0], Is.EquivalentTo(new object[] { 1 }));
    }

    [Test]
    public void GetRows_ThreeCorpora_OverlappingRanges_AllRangeOneThroughTwoRows()
    {
        var corpus1 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 1, "source segment 1 ."),
                    TextRow(
                        "text1",
                        2,
                        "source segment 2 . source segment 3 .",
                        TextRowFlags.SentenceStart | TextRowFlags.InRange | TextRowFlags.RangeStart
                    ),
                    TextRow("text1", 3, flags: TextRowFlags.InRange)
                }
            )
        );
        var corpus2 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow(
                        "text1",
                        1,
                        "source segment 1 . source segment 2 .",
                        TextRowFlags.SentenceStart | TextRowFlags.InRange | TextRowFlags.RangeStart
                    ),
                    TextRow("text1", 2, flags: TextRowFlags.InRange),
                    TextRow("text1", 3, "source segment 3 .")
                }
            )
        );
        var corpus3 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 1, "source segment 1 ."),
                    TextRow("text1", 2, "source segment 2 ."),
                    TextRow("text1", 3, "source segment 3 .")
                }
            )
        );
        var nParallelCorpus = new NParallelTextCorpus([corpus1, corpus2, corpus3]) { AllRows = [false, true, false] };
        NParallelTextRow[] rows = nParallelCorpus.ToArray();
        Assert.That(rows.Length, Is.EqualTo(2), JsonSerializer.Serialize(rows));
        Assert.That(rows[0].NRefs[0], Is.EquivalentTo(new object[] { 1, 2 }));
    }

    [Test]
    public void GetRows_ThreeCorpora_OverlappingRanges_AllRangeTwoThroughThreeRows()
    {
        var corpus1 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 1, "source segment 1 ."),
                    TextRow(
                        "text1",
                        2,
                        "source segment 2 . source segment 3 .",
                        TextRowFlags.SentenceStart | TextRowFlags.InRange | TextRowFlags.RangeStart
                    ),
                    TextRow("text1", 3, flags: TextRowFlags.InRange)
                }
            )
        );
        var corpus2 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow(
                        "text1",
                        1,
                        "source segment 1 . source segment 2 .",
                        TextRowFlags.SentenceStart | TextRowFlags.InRange | TextRowFlags.RangeStart
                    ),
                    TextRow("text1", 2, flags: TextRowFlags.InRange),
                    TextRow("text1", 3, "source segment 3 .")
                }
            )
        );
        var corpus3 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 1, "source segment 1 ."),
                    TextRow("text1", 2, "source segment 2 ."),
                    TextRow("text1", 3, "source segment 3 .")
                }
            )
        );
        var nParallelCorpus = new NParallelTextCorpus([corpus1, corpus2, corpus3]) { AllRows = [true, false, false] };
        NParallelTextRow[] rows = nParallelCorpus.ToArray();
        Assert.That(rows.Length, Is.EqualTo(2), JsonSerializer.Serialize(rows));
        Assert.That(rows[0].NRefs[0], Is.EquivalentTo(new object[] { 1 }));
    }

    [Test]
    public void GetRows_ThreeCorpora_SameRefManyToMany()
    {
        var corpus1 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 1, "source segment 1 ."),
                    TextRow("text1", 2, "source segment 2-1 ."),
                    TextRow("text1", 2, "source segment 2-2 ."),
                    TextRow("text1", 3, "source segment 3 .")
                }
            )
        );
        var corpus2 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 1, "source segment 1 ."),
                    TextRow("text1", 2, "source segment 2-1 ."),
                    TextRow("text1", 2, "source segment 2-2 ."),
                    TextRow("text1", 3, "source segment 3 .")
                }
            )
        );
        var corpus3 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 1, "source segment 1 ."),
                    TextRow("text1", 2, "source segment 2-1 ."),
                    TextRow("text1", 2, "source segment 2-2 ."),
                    TextRow("text1", 3, "source segment 3 .")
                }
            )
        );
        var nParallelCorpus = new NParallelTextCorpus([corpus1, corpus2, corpus3]);
        NParallelTextRow[] rows = nParallelCorpus.ToArray();
        Assert.That(rows.Length, Is.EqualTo(10));
    }

    [Test]
    public void GetRows_ThreeCorpora_SameRefCorporaOfDifferentSizes()
    {
        var corpus1 = new DictionaryTextCorpus(
            new MemoryText("text1", new[] { TextRow("text1", 2, "source segment 2 .") })
        );
        var corpus2 = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                new[]
                {
                    TextRow("text1", 1, "source segment 1 ."),
                    TextRow("text1", 2, "source segment 2-1 ."),
                    TextRow("text1", 2, "source segment 2-2 ."),
                    TextRow("text1", 3, "source segment 3 . ")
                }
            )
        );
        var corpus3 = new DictionaryTextCorpus(
            new MemoryText("text1", new[] { TextRow("text1", 1, "source segment 1 .") })
        );
        var nParallelCorpus = new NParallelTextCorpus([corpus1, corpus2, corpus3]) { AllRows = [true, true, true] };
        NParallelTextRow[] rows = nParallelCorpus.ToArray();
        Assert.That(rows.Length, Is.EqualTo(4), JsonSerializer.Serialize(rows));
        Assert.That(rows[0].NRefs[1], Is.EquivalentTo(new object[] { 1 }));
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
