using NUnit.Framework;
using SIL.Machine.Corpora;
using SIL.Scripture;

namespace SIL.Machine.PunctuationAnalysis;

[TestFixture]
public class UsfmStructureExtractorTests
{
    private MockUsfmParserState _verseTextParserState;

    [SetUp]
    public void SetUp()
    {
        _verseTextParserState = new MockUsfmParserState(new UsfmStylesheet("usfm.sty"), ScrVers.English, []);
        _verseTextParserState.SetVerseNum(1);
    }

    [Test]
    public void GetChaptersFilterByBook()
    {
        var usfmStructureExtractor = new UsfmStructureExtractor();
        usfmStructureExtractor.StartBook(_verseTextParserState, "id", "GEN");
        usfmStructureExtractor.Chapter(_verseTextParserState, "1", "c", null, null);
        usfmStructureExtractor.Verse(_verseTextParserState, "1", "v", null, null);
        usfmStructureExtractor.Text(_verseTextParserState, "test");

        Assert.That(
            usfmStructureExtractor.GetChapters(new Dictionary<int, List<int>> { { 2, [1] } }), // EXO 1
            Has.Count.EqualTo(0)
        );
    }

    [Test]
    public void GetChaptersFilterByChapter()
    {
        var usfmStructureExtractor = new UsfmStructureExtractor();
        usfmStructureExtractor.StartBook(_verseTextParserState, "id", "MAT");
        _verseTextParserState.SetChapterNum(1);
        usfmStructureExtractor.Chapter(_verseTextParserState, "1", "c", null, null);
        usfmStructureExtractor.Verse(_verseTextParserState, "1", "v", null, null);
        usfmStructureExtractor.Text(_verseTextParserState, "test");
        _verseTextParserState.SetChapterNum(2);
        usfmStructureExtractor.Chapter(_verseTextParserState, "2", "c", null, null);
        usfmStructureExtractor.Verse(_verseTextParserState, "1", "v", null, null);
        usfmStructureExtractor.Text(_verseTextParserState, "test2");
        _verseTextParserState.SetChapterNum(3);
        usfmStructureExtractor.Chapter(_verseTextParserState, "3", "c", null, null);
        usfmStructureExtractor.Verse(_verseTextParserState, "1", "v", null, null);
        usfmStructureExtractor.Text(_verseTextParserState, "test3");

        List<Chapter> expectedChapters =
        [
            new Chapter(
                [
                    new Verse(
                        [
                            new TextSegment.Builder()
                                .SetText("test2")
                                .AddPrecedingMarker(UsfmMarkerType.Chapter)
                                .AddPrecedingMarker(UsfmMarkerType.Verse)
                                .Build()
                        ]
                    )
                ]
            )
        ];
        List<Chapter> actualChapters = usfmStructureExtractor.GetChapters(
            new Dictionary<int, List<int>> { { 40, [2] } }
        );
        AssertChapterEqual(expectedChapters, actualChapters);
        Assert.That(actualChapters[0].Verses[0].TextSegments[0].PreviousSegment, Is.Null);
        Assert.That(actualChapters[0].Verses[0].TextSegments[0].NextSegment, Is.Null);
    }

    [Test]
    public void ChapterAndVerseMarkers()
    {
        var usfmStructureExtractor = new UsfmStructureExtractor();
        usfmStructureExtractor.Chapter(_verseTextParserState, "1", "c", null, null);
        usfmStructureExtractor.Verse(_verseTextParserState, "1", "v", null, null);
        usfmStructureExtractor.Text(_verseTextParserState, "test");

        List<Chapter> expectedChapters =
        [
            new Chapter(
                [
                    new Verse(
                        [
                            new TextSegment.Builder()
                                .SetText("test")
                                .AddPrecedingMarker(UsfmMarkerType.Chapter)
                                .AddPrecedingMarker(UsfmMarkerType.Verse)
                                .Build()
                        ]
                    )
                ]
            )
        ];

        List<Chapter> actualChapters = usfmStructureExtractor.GetChapters();
        AssertChapterEqual(expectedChapters, actualChapters);
        Assert.IsNull(actualChapters[0].Verses[0].TextSegments[0].PreviousSegment);
        Assert.IsNull(actualChapters[0].Verses[0].TextSegments[0].NextSegment);
    }

    [Test]
    public void StartParagraphMarker()
    {
        var usfmStructureExtractor = new UsfmStructureExtractor();
        usfmStructureExtractor.Chapter(_verseTextParserState, "1", "c", null, null);
        usfmStructureExtractor.Verse(_verseTextParserState, "1", "v", null, null);
        usfmStructureExtractor.StartPara(_verseTextParserState, "p", false, null);
        usfmStructureExtractor.Text(_verseTextParserState, "test");

        List<Chapter> expectedChapters =
        [
            new Chapter(
                [
                    new Verse(
                        [
                            new TextSegment.Builder()
                                .SetText("test")
                                .AddPrecedingMarker(UsfmMarkerType.Chapter)
                                .AddPrecedingMarker(UsfmMarkerType.Verse)
                                .AddPrecedingMarker(UsfmMarkerType.Paragraph)
                                .Build()
                        ]
                    )
                ]
            )
        ];

        List<Chapter> actualChapters = usfmStructureExtractor.GetChapters();
        AssertChapterEqual(expectedChapters, actualChapters);
        Assert.IsNull(actualChapters[0].Verses[0].TextSegments[0].PreviousSegment);
        Assert.IsNull(actualChapters[0].Verses[0].TextSegments[0].NextSegment);
    }

    [Test]
    public void StartCharacterMarker()
    {
        var usfmStructureExtractor = new UsfmStructureExtractor();
        usfmStructureExtractor.Chapter(_verseTextParserState, "1", "c", null, null);
        usfmStructureExtractor.Verse(_verseTextParserState, "1", "v", null, null);
        usfmStructureExtractor.StartChar(_verseTextParserState, "k", false, null);
        usfmStructureExtractor.Text(_verseTextParserState, "test");

        List<Chapter> expectedChapters =
        [
            new Chapter(
                [
                    new Verse(
                        [
                            new TextSegment.Builder()
                                .SetText("test")
                                .AddPrecedingMarker(UsfmMarkerType.Chapter)
                                .AddPrecedingMarker(UsfmMarkerType.Verse)
                                .AddPrecedingMarker(UsfmMarkerType.Character)
                                .Build()
                        ]
                    )
                ]
            )
        ];

        List<Chapter> actualChapters = usfmStructureExtractor.GetChapters();
        AssertChapterEqual(expectedChapters, actualChapters);
        Assert.IsNull(actualChapters[0].Verses[0].TextSegments[0].PreviousSegment);
        Assert.IsNull(actualChapters[0].Verses[0].TextSegments[0].NextSegment);
    }

    [Test]
    public void EndCharacterMarker()
    {
        var usfmStructureExtractor = new UsfmStructureExtractor();
        usfmStructureExtractor.Chapter(_verseTextParserState, "1", "c", null, null);
        usfmStructureExtractor.Verse(_verseTextParserState, "1", "v", null, null);
        usfmStructureExtractor.EndChar(_verseTextParserState, "k", null, false);
        usfmStructureExtractor.Text(_verseTextParserState, "test");

        List<Chapter> expectedChapters =
        [
            new Chapter(
                [
                    new Verse(
                        [
                            new TextSegment.Builder()
                                .SetText("test")
                                .AddPrecedingMarker(UsfmMarkerType.Chapter)
                                .AddPrecedingMarker(UsfmMarkerType.Verse)
                                .AddPrecedingMarker(UsfmMarkerType.Character)
                                .Build()
                        ]
                    )
                ]
            )
        ];

        List<Chapter> actualChapters = usfmStructureExtractor.GetChapters();
        AssertChapterEqual(expectedChapters, actualChapters);
        Assert.IsNull(actualChapters[0].Verses[0].TextSegments[0].PreviousSegment);
        Assert.IsNull(actualChapters[0].Verses[0].TextSegments[0].NextSegment);
    }

    [Test]
    public void EndNoteMarker()
    {
        var usfmStructureExtractor = new UsfmStructureExtractor();
        usfmStructureExtractor.Chapter(_verseTextParserState, "1", "c", null, null);
        usfmStructureExtractor.Verse(_verseTextParserState, "1", "v", null, null);
        usfmStructureExtractor.EndNote(_verseTextParserState, "f", false);
        usfmStructureExtractor.Text(_verseTextParserState, "test");

        List<Chapter> expectedChapters =
        [
            new Chapter(
                [
                    new Verse(
                        [
                            new TextSegment.Builder()
                                .SetText("test")
                                .AddPrecedingMarker(UsfmMarkerType.Chapter)
                                .AddPrecedingMarker(UsfmMarkerType.Verse)
                                .AddPrecedingMarker(UsfmMarkerType.Embed)
                                .Build()
                        ]
                    )
                ]
            )
        ];

        List<Chapter> actualChapters = usfmStructureExtractor.GetChapters();
        AssertChapterEqual(expectedChapters, actualChapters);
        Assert.IsNull(actualChapters[0].Verses[0].TextSegments[0].PreviousSegment);
        Assert.IsNull(actualChapters[0].Verses[0].TextSegments[0].NextSegment);
    }

    [Test]
    public void EndTableMarker()
    {
        var usfmStructureExtractor = new UsfmStructureExtractor();
        usfmStructureExtractor.Chapter(_verseTextParserState, "1", "c", null, null);
        usfmStructureExtractor.Verse(_verseTextParserState, "1", "v", null, null);
        usfmStructureExtractor.EndNote(_verseTextParserState, "tr", false);
        usfmStructureExtractor.Text(_verseTextParserState, "test");

        List<Chapter> expectedChapters =
        [
            new Chapter(
                [
                    new Verse(
                        [
                            new TextSegment.Builder()
                                .SetText("test")
                                .AddPrecedingMarker(UsfmMarkerType.Chapter)
                                .AddPrecedingMarker(UsfmMarkerType.Verse)
                                .AddPrecedingMarker(UsfmMarkerType.Embed)
                                .Build()
                        ]
                    )
                ]
            )
        ];

        List<Chapter> actualChapters = usfmStructureExtractor.GetChapters();
        AssertChapterEqual(expectedChapters, actualChapters);
        Assert.IsNull(actualChapters[0].Verses[0].TextSegments[0].PreviousSegment);
        Assert.IsNull(actualChapters[0].Verses[0].TextSegments[0].NextSegment);
    }

    [Test]
    public void RefMarker()
    {
        var usfmStructureExtractor = new UsfmStructureExtractor();
        usfmStructureExtractor.Chapter(_verseTextParserState, "1", "c", null, null);
        usfmStructureExtractor.Verse(_verseTextParserState, "1", "v", null, null);
        usfmStructureExtractor.EndNote(_verseTextParserState, "x", false);
        usfmStructureExtractor.Text(_verseTextParserState, "test");

        List<Chapter> expectedChapters =
        [
            new Chapter(
                [
                    new Verse(
                        [
                            new TextSegment.Builder()
                                .SetText("test")
                                .AddPrecedingMarker(UsfmMarkerType.Chapter)
                                .AddPrecedingMarker(UsfmMarkerType.Verse)
                                .AddPrecedingMarker(UsfmMarkerType.Embed)
                                .Build()
                        ]
                    )
                ]
            )
        ];

        List<Chapter> actualChapters = usfmStructureExtractor.GetChapters();
        AssertChapterEqual(expectedChapters, actualChapters);
        Assert.IsNull(actualChapters[0].Verses[0].TextSegments[0].PreviousSegment);
        Assert.IsNull(actualChapters[0].Verses[0].TextSegments[0].NextSegment);
    }

    [Test]
    public void SidebarMarker()
    {
        var usfmStructureExtractor = new UsfmStructureExtractor();
        usfmStructureExtractor.Chapter(_verseTextParserState, "1", "c", null, null);
        usfmStructureExtractor.Verse(_verseTextParserState, "1", "v", null, null);
        usfmStructureExtractor.EndNote(_verseTextParserState, "esb", false);
        usfmStructureExtractor.Text(_verseTextParserState, "test");

        List<Chapter> expectedChapters =
        [
            new Chapter(
                [
                    new Verse(
                        [
                            new TextSegment.Builder()
                                .SetText("test")
                                .AddPrecedingMarker(UsfmMarkerType.Chapter)
                                .AddPrecedingMarker(UsfmMarkerType.Verse)
                                .AddPrecedingMarker(UsfmMarkerType.Embed)
                                .Build()
                        ]
                    )
                ]
            )
        ];

        List<Chapter> actualChapters = usfmStructureExtractor.GetChapters();
        AssertChapterEqual(expectedChapters, actualChapters);
        Assert.IsNull(actualChapters[0].Verses[0].TextSegments[0].PreviousSegment);
        Assert.IsNull(actualChapters[0].Verses[0].TextSegments[0].NextSegment);
    }

    [Test]
    public void MultipleVerses()
    {
        var usfmStructureExtractor = new UsfmStructureExtractor();
        usfmStructureExtractor.Chapter(_verseTextParserState, "1", "c", null, null);
        usfmStructureExtractor.Verse(_verseTextParserState, "1", "v", null, null);
        usfmStructureExtractor.Text(_verseTextParserState, "test");
        usfmStructureExtractor.Verse(_verseTextParserState, "2", "v", null, null);
        usfmStructureExtractor.Text(_verseTextParserState, "test2");

        List<Chapter> expectedChapters =
        [
            new Chapter(
                [
                    new Verse(
                        [
                            new TextSegment.Builder()
                                .SetText("test")
                                .AddPrecedingMarker(UsfmMarkerType.Chapter)
                                .AddPrecedingMarker(UsfmMarkerType.Verse)
                                .Build()
                        ]
                    ),
                    new Verse(
                        [
                            new TextSegment.Builder()
                                .SetText("test2")
                                .AddPrecedingMarker(UsfmMarkerType.Chapter)
                                .AddPrecedingMarker(UsfmMarkerType.Verse)
                                .Build()
                        ]
                    ),
                ]
            )
        ];

        List<Chapter> actualChapters = usfmStructureExtractor.GetChapters();
        AssertChapterEqual(expectedChapters, actualChapters);
        Assert.IsNull(actualChapters[0].Verses[0].TextSegments[0].PreviousSegment);
        Assert.IsNull(actualChapters[0].Verses[0].TextSegments[0].NextSegment);
        Assert.IsNull(actualChapters[0].Verses[1].TextSegments[0].PreviousSegment);
        Assert.IsNull(actualChapters[0].Verses[1].TextSegments[0].NextSegment);
    }

    [Test]
    public void MultipleChapters()
    {
        var usfmStructureExtractor = new UsfmStructureExtractor();
        usfmStructureExtractor.Chapter(_verseTextParserState, "1", "c", null, null);
        usfmStructureExtractor.Verse(_verseTextParserState, "1", "v", null, null);
        usfmStructureExtractor.Text(_verseTextParserState, "test");
        usfmStructureExtractor.Chapter(_verseTextParserState, "2", "c", null, null);
        usfmStructureExtractor.Verse(_verseTextParserState, "1", "v", null, null);
        usfmStructureExtractor.Text(_verseTextParserState, "test2");

        List<Chapter> expectedChapters =
        [
            new Chapter(
                [
                    new Verse(
                        [
                            new TextSegment.Builder()
                                .SetText("test")
                                .AddPrecedingMarker(UsfmMarkerType.Chapter)
                                .AddPrecedingMarker(UsfmMarkerType.Verse)
                                .Build()
                        ]
                    ),
                ]
            ),
            new Chapter(
                [
                    new Verse(
                        [
                            new TextSegment.Builder()
                                .SetText("test2")
                                .AddPrecedingMarker(UsfmMarkerType.Chapter)
                                .AddPrecedingMarker(UsfmMarkerType.Verse)
                                .Build()
                        ]
                    ),
                ]
            ),
        ];

        List<Chapter> actualChapters = usfmStructureExtractor.GetChapters();
        AssertChapterEqual(expectedChapters, actualChapters);
        Assert.IsNull(actualChapters[0].Verses[0].TextSegments[0].PreviousSegment);
        Assert.IsNull(actualChapters[0].Verses[0].TextSegments[0].NextSegment);
        Assert.IsNull(actualChapters[1].Verses[0].TextSegments[0].PreviousSegment);
        Assert.IsNull(actualChapters[1].Verses[0].TextSegments[0].NextSegment);
    }

    [Test]
    public void CharacterMarkerInText()
    {
        var usfmStructureExtractor = new UsfmStructureExtractor();
        usfmStructureExtractor.Chapter(_verseTextParserState, "1", "c", null, null);
        usfmStructureExtractor.Verse(_verseTextParserState, "1", "v", null, null);
        usfmStructureExtractor.Text(_verseTextParserState, "test");
        usfmStructureExtractor.StartChar(_verseTextParserState, "k", false, null);
        usfmStructureExtractor.Text(_verseTextParserState, "test2");

        List<Chapter> expectedChapters =
        [
            new Chapter(
                [
                    new Verse(
                        [
                            new TextSegment.Builder()
                                .SetText("test")
                                .AddPrecedingMarker(UsfmMarkerType.Chapter)
                                .AddPrecedingMarker(UsfmMarkerType.Verse)
                                .Build(),
                            new TextSegment.Builder()
                                .SetText("test2")
                                .AddPrecedingMarker(UsfmMarkerType.Chapter)
                                .AddPrecedingMarker(UsfmMarkerType.Verse)
                                .AddPrecedingMarker(UsfmMarkerType.Character)
                                .Build(),
                        ]
                    ),
                ]
            )
        ];

        List<Chapter> actualChapters = usfmStructureExtractor.GetChapters();
        AssertChapterEqual(expectedChapters, actualChapters);
        Assert.That(
            actualChapters[0].Verses[0].TextSegments[1].PreviousSegment,
            Is.EqualTo(expectedChapters[0].Verses[0].TextSegments[0])
        );
        Assert.That(
            actualChapters[0].Verses[0].TextSegments[0].NextSegment,
            Is.EqualTo(expectedChapters[0].Verses[0].TextSegments[1])
        );
    }

    [Test]
    public void EmptyText()
    {
        var usfmStructureExtractor = new UsfmStructureExtractor();
        usfmStructureExtractor.Chapter(_verseTextParserState, "1", "c", null, null);
        usfmStructureExtractor.Verse(_verseTextParserState, "1", "v", null, null);
        usfmStructureExtractor.Text(_verseTextParserState, "test");
        usfmStructureExtractor.StartChar(_verseTextParserState, "k", false, null);
        usfmStructureExtractor.Text(_verseTextParserState, "");
        usfmStructureExtractor.EndChar(_verseTextParserState, "k", null, false);
        usfmStructureExtractor.Text(_verseTextParserState, "test2");

        List<Chapter> expectedChapters =
        [
            new Chapter(
                [
                    new Verse(
                        [
                            new TextSegment.Builder()
                                .SetText("test")
                                .AddPrecedingMarker(UsfmMarkerType.Chapter)
                                .AddPrecedingMarker(UsfmMarkerType.Verse)
                                .Build(),
                            new TextSegment.Builder()
                                .SetText("test2")
                                .AddPrecedingMarker(UsfmMarkerType.Chapter)
                                .AddPrecedingMarker(UsfmMarkerType.Verse)
                                .AddPrecedingMarker(UsfmMarkerType.Character)
                                .Build(),
                        ]
                    ),
                ]
            )
        ];

        List<Chapter> actualChapters = usfmStructureExtractor.GetChapters();
        AssertChapterEqual(expectedChapters, actualChapters);
        Assert.That(
            actualChapters[0].Verses[0].TextSegments[1].PreviousSegment,
            Is.EqualTo(expectedChapters[0].Verses[0].TextSegments[0])
        );
        Assert.That(
            actualChapters[0].Verses[0].TextSegments[0].NextSegment,
            Is.EqualTo(expectedChapters[0].Verses[0].TextSegments[1])
        );
    }

    private static void AssertChapterEqual(List<Chapter> expectedChapters, List<Chapter> actualChapters)
    {
        Assert.That(expectedChapters.Count, Is.EqualTo(actualChapters.Count));
        foreach ((Chapter expectedChapter, Chapter actualChapter) in expectedChapters.Zip(actualChapters))
        {
            Assert.That(expectedChapter.Verses.Count, Is.EqualTo(actualChapter.Verses.Count));
            foreach ((Verse expectedVerse, Verse actualVerse) in expectedChapter.Verses.Zip(actualChapter.Verses))
            {
                Assert.That(expectedVerse.TextSegments.Count, Is.EqualTo(actualVerse.TextSegments.Count));
                foreach (
                    (TextSegment expectedSegment, TextSegment actualSegment) in expectedVerse.TextSegments.Zip(
                        actualVerse.TextSegments
                    )
                )
                {
                    Assert.That(expectedSegment, Is.EqualTo(actualSegment));
                }
            }
        }
    }

    private class MockUsfmParserState(UsfmStylesheet stylesheet, ScrVers versification, IReadOnlyList<UsfmToken> tokens)
        : UsfmParserState(stylesheet, versification, tokens)
    {
        public void SetVerseNum(int verseNum)
        {
            VerseRef vref = VerseRef;
            vref.VerseNum = verseNum;
            VerseRef = vref;
        }

        public void SetChapterNum(int chapterNum)
        {
            VerseRef vref = VerseRef;
            vref.ChapterNum = chapterNum;
            VerseRef = vref;
        }
    }
}
