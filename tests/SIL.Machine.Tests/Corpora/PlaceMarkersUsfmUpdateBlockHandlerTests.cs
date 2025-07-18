namespace SIL.Machine.Corpora;

using NUnit.Framework;
using SIL.Machine.Tokenization;
using SIL.Machine.Translation;

[TestFixture]
public class PlaceMarkersUsfmUpdateBlockHandlerTests
{
    private static readonly LatinWordTokenizer Tokenizer = new LatinWordTokenizer();

    [Test]
    public void UpdateUsfm_ParagraphMarkers()
    {
        string source = "This is the first paragraph. This text is in English, and this test is for paragraph markers.";
        string pretranslation =
            "Este es el primer párrafo. Este texto está en inglés y esta prueba es para marcadores de párrafo.";
        IReadOnlyList<(IReadOnlyList<ScriptureRef>, string)> rows = [(ScrRef("MAT 1:1"), pretranslation)];
        string usfm =
            @"\id MAT
\c 1
\v 1 This is the first paragraph.
\p This text is in English,
\p and this test is for paragraph markers.
";
        IReadOnlyList<PlaceMarkersAlignmentInfo> alignInfo =
        [
            new PlaceMarkersAlignmentInfo(
                refs: ["MAT 1:1"],
                sourceTokens: Tokenizer.Tokenize(source).ToList(),
                translationTokens: Tokenizer.Tokenize(pretranslation).ToList(),
                alignment: ToWordAlignmentMatrix(
                    "0-0 1-1 2-2 3-3 4-4 5-5 6-6 7-7 8-8 9-9 10-10 12-11 13-12 14-13 15-14 16-15 17-18 18-16 19-19"
                )
            )
        ];

        string target = UpdateUsfm(
            rows,
            usfm,
            paragraphBehavior: UpdateUsfmMarkerBehavior.Preserve,
            usfmUpdateBlockHandlers: [new PlaceMarkersUsfmUpdateBlockHandler(alignInfo)]
        );

        string result =
            @"\id MAT
\c 1
\v 1 Este es el primer párrafo.
\p Este texto está en inglés
\p y esta prueba es para marcadores de párrafo.
";

        AssertUsfmEquals(target, result);
    }

    [Test]
    public void UpdateUsfm_StyleMarkers()
    {
        string source = "This is the first sentence. This text is in English, and this test is for style markers.";
        string pretranslation =
            "Esta es la primera oración. Este texto está en inglés y esta prueba es para marcadores de estilo.";
        IReadOnlyList<(IReadOnlyList<ScriptureRef>, string)> rows = [(ScrRef("MAT 1:1"), pretranslation)];
        string usfm =
            @"\id MAT
\c 1
\v 1 This is the \w first\w* sentence. This text is in \w English\w*, and this test is \w for\w* style markers.
";
        IReadOnlyList<PlaceMarkersAlignmentInfo> alignInfo =
        [
            new PlaceMarkersAlignmentInfo(
                refs: ["MAT 1:1"],
                sourceTokens: Tokenizer.Tokenize(source).ToList(),
                translationTokens: Tokenizer.Tokenize(pretranslation).ToList(),
                alignment: ToWordAlignmentMatrix(
                    "0-0 1-1 2-2 3-3 4-4 5-5 6-6 7-7 8-8 9-9 10-10 12-11 13-12 14-13 15-14 16-15 17-18 18-16 19-19"
                )
            )
        ];

        string target = UpdateUsfm(
            rows,
            usfm,
            styleBehavior: UpdateUsfmMarkerBehavior.Preserve,
            usfmUpdateBlockHandlers: [new PlaceMarkersUsfmUpdateBlockHandler(alignInfo)]
        );

        string result =
            @"\id MAT
\c 1
\v 1 Esta es la \w primera\w* oración. Este texto está en \w inglés\w* y esta prueba es \w para\w* marcadores de estilo.
";

        AssertUsfmEquals(target, result);

        target = UpdateUsfm(
            rows,
            usfm,
            styleBehavior: UpdateUsfmMarkerBehavior.Strip,
            usfmUpdateBlockHandlers: [new PlaceMarkersUsfmUpdateBlockHandler(alignInfo)]
        );

        result =
            @"\id MAT
\c 1
\v 1 Esta es la primera oración. Este texto está en inglés y esta prueba es para marcadores de estilo.
";

        AssertUsfmEquals(target, result);
    }

    // NOTE: Not currently updating embeds, will need to change test when we do
    [Test]
    public void UpdateUsfm_EmbedMarkers()
    {
        IReadOnlyList<(IReadOnlyList<ScriptureRef>, string)> rows =
        [
            (ScrRef("MAT 1:1"), "New verse 1"),
            (ScrRef("MAT 1:2"), "New verse 2"),
            (ScrRef("MAT 1:3"), "New verse 3"),
            (ScrRef("MAT 1:4"), "New verse 4"),
            (ScrRef("MAT 1:4/1:f"), "New embed text"),
            (ScrRef("MAT 1:5"), "New verse 5"),
            (ScrRef("MAT 1:6"), "New verse 6"),
            (ScrRef("MAT 1:6/1:f"), "New verse 6 embed text")
        ];
        string usfm =
            @"\id MAT
\c 1
\v 1 \f \fr 1.1 \ft Some note \f*Start of sentence embed
\v 2 Middle of sentence \f \fr 1.2 \ft Some other note \f*embed
\v 3 End of sentence embed\f \fr 1.3 \ft A third note \f*
\v 4 Updated embed\f \fr 1.4 \ft A fourth note \f*
\v 5 Embed with style markers \f \fr 1.5 \ft A \+w stylish\+w* note \f*
\v 6 Updated embed with style markers \f \fr 1.6 \ft Another \+w stylish\+w* note \f*
";
        IReadOnlyList<PlaceMarkersAlignmentInfo> alignInfo = [];

        string target = UpdateUsfm(
            rows,
            usfm,
            embedBehavior: UpdateUsfmMarkerBehavior.Preserve,
            usfmUpdateBlockHandlers: [new PlaceMarkersUsfmUpdateBlockHandler(alignInfo)]
        );

        string result =
            @"\id MAT
\c 1
\v 1 New verse 1 \f \fr 1.1 \ft Some note \f*
\v 2 New verse 2 \f \fr 1.2 \ft Some other note \f*
\v 3 New verse 3 \f \fr 1.3 \ft A third note \f*
\v 4 New verse 4 \f \fr 1.4 \ft A fourth note \f*
\v 5 New verse 5 \f \fr 1.5 \ft A \+w stylish\+w* note \f*
\v 6 New verse 6 \f \fr 1.6 \ft Another \+w stylish\+w* note \f*
";

        AssertUsfmEquals(target, result);

        target = UpdateUsfm(
            rows,
            usfm,
            embedBehavior: UpdateUsfmMarkerBehavior.Strip,
            usfmUpdateBlockHandlers: [new PlaceMarkersUsfmUpdateBlockHandler(alignInfo)]
        );

        result =
            @"\id MAT
\c 1
\v 1 New verse 1
\v 2 New verse 2
\v 3 New verse 3
\v 4 New verse 4
\v 5 New verse 5
\v 6 New verse 6
";

        AssertUsfmEquals(target, result);
    }

    [Test]
    public void UpdateUsfm_TrailingEmptyParagraphs()
    {
        IReadOnlyList<(IReadOnlyList<ScriptureRef>, string)> rows = [(ScrRef("MAT 1:1"), "New verse 1")];
        string usfm =
            @"\id MAT
\c 1
\v 1 \f embed 1 \f*Verse 1
\p
\b
\q1 \f embed 2 \f*
";
        IReadOnlyList<PlaceMarkersAlignmentInfo> alignInfo =
        [
            new PlaceMarkersAlignmentInfo(
                refs: ["MAT 1:1"],
                sourceTokens: ["Verse", "1"],
                translationTokens: ["New", "verse", "1"],
                alignment: ToWordAlignmentMatrix("0-1 1-2")
            )
        ];

        string target = UpdateUsfm(
            rows,
            usfm,
            paragraphBehavior: UpdateUsfmMarkerBehavior.Preserve,
            usfmUpdateBlockHandlers: [new PlaceMarkersUsfmUpdateBlockHandler(alignInfo)]
        );

        string result =
            @"\id MAT
\c 1
\v 1 New verse 1 \f embed 1 \f*\f embed 2 \f*
\p
\b
\q1
";
        AssertUsfmEquals(target, result);
    }

    [Test]
    public void UpdateUsfm_Headers()
    {
        IReadOnlyList<(IReadOnlyList<ScriptureRef>, string)> rows =
        [
            (ScrRef("MAT 1:1"), "X Y Z"),
            (ScrRef("MAT 1:2"), "X"),
            (ScrRef("MAT 1:3"), "Y"),
            (ScrRef("MAT 1:3/1:s1"), "Updated header")
        ];
        string usfm =
            @"\id MAT
\c 1
\s1 Start of chapter header
\p
\v 1 A
\p B
\s1 Mid-verse header
\p C
\s1 Header between verse text and empty end-of-verse paragraphs
\p
\p
\p
\s1 Header after all verse paragraphs
\p
\v 2 A
\s1 Header followed by a reference
\r (reference)
\p
\v 3 B
\s1 Header to be updated
";
        IReadOnlyList<PlaceMarkersAlignmentInfo> alignInfo =
        [
            new PlaceMarkersAlignmentInfo(
                refs: ["MAT 1:1"],
                sourceTokens: ["A", "B", "C"],
                translationTokens: ["X", "Y", "Z"],
                alignment: ToWordAlignmentMatrix("0-0 1-1 2-2")
            ),
            new PlaceMarkersAlignmentInfo(
                refs: ["MAT 1:2"],
                sourceTokens: ["A"],
                translationTokens: ["X"],
                alignment: ToWordAlignmentMatrix("0-0")
            )
        ];

        string target = UpdateUsfm(
            rows,
            usfm,
            paragraphBehavior: UpdateUsfmMarkerBehavior.Preserve,
            usfmUpdateBlockHandlers: [new PlaceMarkersUsfmUpdateBlockHandler(alignInfo)]
        );

        string result =
            @"\id MAT
\c 1
\s1 Start of chapter header
\p
\v 1 X
\p Y
\s1 Mid-verse header
\p Z
\s1 Header between verse text and empty end-of-verse paragraphs
\p
\p
\p
\s1 Header after all verse paragraphs
\p
\v 2 X
\s1 Header followed by a reference
\r (reference)
\p
\v 3 Y
\s1 Updated header
";

        AssertUsfmEquals(target, result);
    }

    [Test]
    public void UpdateUsfm_ConsecutiveMarkers()
    {
        IReadOnlyList<(IReadOnlyList<ScriptureRef>, string)> rows = [(ScrRef("MAT 1:1"), "New verse 1 WORD"),];
        string usfm =
            @"\id MAT
\c 1
\v 1 Old verse 1
\p \qt \+w word\+w*\qt*
";
        IReadOnlyList<PlaceMarkersAlignmentInfo> alignInfo =
        [
            new PlaceMarkersAlignmentInfo(
                refs: ["MAT 1:1"],
                sourceTokens: ["Old", "verse", "1", "word"],
                translationTokens: ["New", "verse", "1", "WORD"],
                alignment: ToWordAlignmentMatrix("0-0 1-1 2-2 3-3")
            )
        ];

        string target = UpdateUsfm(
            rows,
            usfm,
            paragraphBehavior: UpdateUsfmMarkerBehavior.Preserve,
            styleBehavior: UpdateUsfmMarkerBehavior.Preserve,
            usfmUpdateBlockHandlers: [new PlaceMarkersUsfmUpdateBlockHandler(alignInfo)]
        );

        string result =
            @"\id MAT
\c 1
\v 1 New verse 1
\p \qt \+w WORD\+w*\qt*
";

        AssertUsfmEquals(target, result);
    }

    [Test]
    public void UpdateUsfm_VerseRanges()
    {
        IReadOnlyList<(IReadOnlyList<ScriptureRef>, string)> rows =
        [
            (
                Enumerable.Range(1, 6).Select(i => ScriptureRef.Parse($"MAT 1:{i}")).ToList(),
                "New verse range text new paragraph 2"
            )
        ];
        string usfm =
            @"\id MAT
\c 1
\v 1-5 Verse range
\p old paragraph 2
";
        IReadOnlyList<PlaceMarkersAlignmentInfo> alignInfo =
        [
            new PlaceMarkersAlignmentInfo(
                refs: Enumerable.Range(1, 6).Select(i => ScriptureRef.Parse($"MAT 1:{i}").ToString()).ToList(),
                sourceTokens: ["Verse", "range", "old", "paragraph", "2"],
                translationTokens: ["New", "verse", "range", "text", "new", "paragraph", "2"],
                alignment: ToWordAlignmentMatrix("0-1 1-2 2-4 3-5 4-6")
            )
        ];

        string target = UpdateUsfm(
            rows,
            usfm,
            paragraphBehavior: UpdateUsfmMarkerBehavior.Preserve,
            usfmUpdateBlockHandlers: [new PlaceMarkersUsfmUpdateBlockHandler(alignInfo)]
        );

        string result =
            @"\id MAT
\c 1
\v 1-5 New verse range text
\p new paragraph 2
";

        AssertUsfmEquals(target, result);
    }

    [Test]
    public void UpdateUsfm_NoUpdate()
    {
        IReadOnlyList<(IReadOnlyList<ScriptureRef>, string)> rows =
        [
            (ScrRef("MAT 1:1"), "New paragraph 1 New paragraph 2"),
        ];
        string usfm =
            @"\id MAT
\c 1
\v 1 Old paragraph 1
\p Old paragraph 2
";

        //Strip paragraphs
        IReadOnlyList<PlaceMarkersAlignmentInfo> alignInfo =
        [
            new PlaceMarkersAlignmentInfo(
                refs: ["MAT 1:1"],
                sourceTokens: ["Old", "paragraph", "1", "Old", "paragraph", "2"],
                translationTokens: ["New", "paragraph", "1", "New", "paragraph", "2"],
                alignment: ToWordAlignmentMatrix("0-0 1-1 2-2 3-3 4-4 5-5")
            )
        ];

        string target = UpdateUsfm(
            rows,
            usfm,
            paragraphBehavior: UpdateUsfmMarkerBehavior.Strip,
            usfmUpdateBlockHandlers: [new PlaceMarkersUsfmUpdateBlockHandler(alignInfo)]
        );

        string result =
            @"\id MAT
\c 1
\v 1 New paragraph 1 New paragraph 2
";

        AssertUsfmEquals(target, result);

        //No alignment
        alignInfo =
        [
            new PlaceMarkersAlignmentInfo(
                refs: ["MAT 1:1"],
                sourceTokens: [],
                translationTokens: [],
                alignment: ToWordAlignmentMatrix("")
            )
        ];

        target = UpdateUsfm(
            rows,
            usfm,
            paragraphBehavior: UpdateUsfmMarkerBehavior.Preserve,
            usfmUpdateBlockHandlers: [new PlaceMarkersUsfmUpdateBlockHandler(alignInfo)]
        );

        result =
            @"\id MAT
\c 1
\v 1 New paragraph 1 New paragraph 2
\p
";

        AssertUsfmEquals(target, result);

        // No text update
        rows = [];
        alignInfo = [];
        target = UpdateUsfm(
            rows,
            usfm,
            paragraphBehavior: UpdateUsfmMarkerBehavior.Preserve,
            usfmUpdateBlockHandlers: [new PlaceMarkersUsfmUpdateBlockHandler(alignInfo)]
        );

        result =
            @"\id MAT
\c 1
\v 1 Old paragraph 1
\p Old paragraph 2
";
        AssertUsfmEquals(target, result);
    }

    [Test]
    public void UpdateUsfm_SplitTokens()
    {
        IReadOnlyList<(IReadOnlyList<ScriptureRef>, string)> rows =
        [
            (ScrRef("MAT 1:1"), "words split words split words split"),
        ];
        string usfm =
            @"\id MAT
\c 1
\v 1 words spl
\p it words spl
\p it words split
";

        IReadOnlyList<PlaceMarkersAlignmentInfo> alignInfo =
        [
            new PlaceMarkersAlignmentInfo(
                refs: ["MAT 1:1"],
                sourceTokens: ["words", "split", "words", "split", "words", "split"],
                translationTokens: ["words", "split", "words", "split", "words", "split"],
                alignment: ToWordAlignmentMatrix("0-0 1-1 2-2 3-3 4-4 5-5")
            )
        ];

        string target = UpdateUsfm(
            rows,
            usfm,
            paragraphBehavior: UpdateUsfmMarkerBehavior.Preserve,
            usfmUpdateBlockHandlers: [new PlaceMarkersUsfmUpdateBlockHandler(alignInfo)]
        );

        string result =
            @"\id MAT
\c 1
\v 1 words split
\p words split
\p words split
";

        AssertUsfmEquals(target, result);
    }

    [Test]
    public void UpdateUsfm_NoText()
    {
        IReadOnlyList<(IReadOnlyList<ScriptureRef>, string)> rows = [(ScrRef("MAT 1:1"), ""),];
        string usfm =
            @"\id MAT
\c 1
\v 1 \w \w*
";

        IReadOnlyList<PlaceMarkersAlignmentInfo> alignInfo =
        [
            new PlaceMarkersAlignmentInfo(
                refs: ["MAT 1:1"],
                sourceTokens: [],
                translationTokens: [],
                alignment: ToWordAlignmentMatrix("")
            )
        ];

        string target = UpdateUsfm(
            rows,
            usfm,
            paragraphBehavior: UpdateUsfmMarkerBehavior.Preserve,
            styleBehavior: UpdateUsfmMarkerBehavior.Preserve,
            usfmUpdateBlockHandlers: [new PlaceMarkersUsfmUpdateBlockHandler(alignInfo)]
        );

        string result =
            @"\id MAT
\c 1
\v 1  \w \w*
";

        AssertUsfmEquals(target, result);
    }

    [Test]
    public void UpdateUsfm_ConsecutiveSubstring()
    {
        IReadOnlyList<(IReadOnlyList<ScriptureRef>, string)> rows = [(ScrRef("MAT 1:1"), "string ring"),];
        string usfm =
            @"\id MAT
\c 1
\v 1 string
\p ring
";

        IReadOnlyList<PlaceMarkersAlignmentInfo> alignInfo =
        [
            new PlaceMarkersAlignmentInfo(
                refs: ["MAT 1:1"],
                sourceTokens: ["string", "ring"],
                translationTokens: ["string", "ring"],
                alignment: ToWordAlignmentMatrix("0-0 1-1")
            )
        ];

        string target = UpdateUsfm(
            rows,
            usfm,
            paragraphBehavior: UpdateUsfmMarkerBehavior.Preserve,
            usfmUpdateBlockHandlers: [new PlaceMarkersUsfmUpdateBlockHandler(alignInfo)]
        );

        string result =
            @"\id MAT
\c 1
\v 1 string
\p ring
";

        AssertUsfmEquals(target, result);
    }

    [Test]
    public void UpdateUsfm_VersesOutOfOrder()
    {
        IReadOnlyList<(IReadOnlyList<ScriptureRef>, string)> rows =
        [
            (ScrRef("MAT 1:1"), "new verse 1 new paragraph 2"),
            (ScrRef("MAT 1:2"), "new verse 2")
        ];
        string usfm =
            @"\id MAT
\c 1
\v 2 verse 2
\v 1 verse 1
\p paragraph 2
";

        IReadOnlyList<PlaceMarkersAlignmentInfo> alignInfo =
        [
            new PlaceMarkersAlignmentInfo(
                refs: ["MAT 1:1"],
                sourceTokens: ["verse", "1", "paragraph", "2"],
                translationTokens: ["new", "verse", "1", "new", "paragraph", "2"],
                alignment: ToWordAlignmentMatrix("0-1 1-2 2-4 3-5")
            ),
            new PlaceMarkersAlignmentInfo(
                refs: ["MAT 1:2"],
                sourceTokens: ["verse", "2"],
                translationTokens: ["new", "verse", "2"],
                alignment: ToWordAlignmentMatrix("0-1 1-2")
            )
        ];

        string target = UpdateUsfm(
            rows,
            usfm,
            textBehavior: UpdateUsfmTextBehavior.StripExisting,
            usfmUpdateBlockHandlers: [new PlaceMarkersUsfmUpdateBlockHandler(alignInfo)]
        );

        string result =
            @"\id MAT
\c 1
\v 2 new verse 2
\v 1
\p
";

        AssertUsfmEquals(target, result);
    }

    private static ScriptureRef[] ScrRef(params string[] refs)
    {
        return refs.Select(r => ScriptureRef.Parse(r)).ToArray();
    }

    private static WordAlignmentMatrix ToWordAlignmentMatrix(string alignment)
    {
        IReadOnlyList<AlignedWordPair> wordPairs = AlignedWordPair.Parse(alignment).ToList();
        int rowCount = 0;
        int columnCount = 0;
        foreach (AlignedWordPair pair in wordPairs)
        {
            if (pair.SourceIndex + 1 > rowCount)
                rowCount = pair.SourceIndex + 1;
            if (pair.TargetIndex + 1 > columnCount)
                columnCount = pair.TargetIndex + 1;
        }
        return new WordAlignmentMatrix(rowCount, columnCount, wordPairs.Select(wp => (wp.SourceIndex, wp.TargetIndex)));
    }

    private static string UpdateUsfm(
        IReadOnlyList<(IReadOnlyList<ScriptureRef>, string)> rows,
        string source,
        string? idText = null,
        UpdateUsfmTextBehavior textBehavior = UpdateUsfmTextBehavior.PreferNew,
        UpdateUsfmMarkerBehavior paragraphBehavior = UpdateUsfmMarkerBehavior.Preserve,
        UpdateUsfmMarkerBehavior embedBehavior = UpdateUsfmMarkerBehavior.Preserve,
        UpdateUsfmMarkerBehavior styleBehavior = UpdateUsfmMarkerBehavior.Strip,
        IEnumerable<string>? preserveParagraphStyles = null,
        IEnumerable<IUsfmUpdateBlockHandler>? usfmUpdateBlockHandlers = null
    )
    {
        source = source.Trim().ReplaceLineEndings("\r\n") + "\r\n";
        var updater = new UpdateUsfmParserHandler(
            rows,
            idText,
            textBehavior,
            paragraphBehavior,
            embedBehavior,
            styleBehavior,
            preserveParagraphStyles,
            usfmUpdateBlockHandlers
        );
        UsfmParser.Parse(source, updater);
        return updater.GetUsfm();
    }

    private static void AssertUsfmEquals(string target, string truth)
    {
        Assert.That(target, Is.Not.Null);
        var target_lines = target.Split(["\n"], StringSplitOptions.None);
        var truth_lines = truth.Split(["\n"], StringSplitOptions.None);
        for (int i = 0; i < truth_lines.Length; i++)
        {
            Assert.That(target_lines[i].Trim(), Is.EqualTo(truth_lines[i].Trim()), message: $"Line {i}");
        }
    }
}
