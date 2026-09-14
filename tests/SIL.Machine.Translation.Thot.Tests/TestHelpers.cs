using SIL.Machine.Corpora;

namespace SIL.Machine.Translation.Thot;

public static class TestHelpers
{
    public static string ToyCorpusHmmFolderName =>
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "data", "toy_corpus_hmm");
    public static string ToyCorpusHmmConfigFileName => Path.Combine(ToyCorpusHmmFolderName, "smt.cfg");

    public static string ToyCorpusFastAlignFolderName =>
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "data", "toy_corpus_fa");
    public static string ToyCorpusFastAlignConfigFileName => Path.Combine(ToyCorpusFastAlignFolderName, "smt.cfg");

    public static IReadOnlyList<string> AlignmentStrings(
        IParallelTextCorpus corpus,
        IEnumerable<string>? textIds = null
    )
    {
        return
        [
            .. corpus
                .GetRows(textIds)
                .SelectMany(row =>
                    row.AlignedWordPairs.Select(wp => new AlignedWordPair(wp.SourceIndex, wp.TargetIndex).ToString())
                ),
        ];
    }

    private static readonly string[] SourceLines =
    [
        "isthay isyay ayay esttay-N .",
        "ouyay ouldshay esttay-V oftenyay .",
        "isyay isthay orkingway ?",
        "isthay ouldshay orkway-V .",
        "ityay isyay orkingway .",
        "orkway-N ancay ebay ardhay !",
        "ayay esttay-N ancay ebay ardhay .",
        "isthay isyay ayay ordway !",
    ];

    private static readonly string[] TargetLines =
    [
        "this is a test N .",
        "you should test V often .",
        "is this working ?",
        "this should work V .",
        "it is working .",
        "work N can be hard !",
        "a test N can be hard .",
        "this is a word !",
    ];

    public static ParallelTextCorpus CreateTestParallelCorpus()
    {
        return new ParallelTextCorpus(TextCorpus(SourceLines, Row), TextCorpus(TargetLines, Row));
    }

    // The test corpus with each segment left as one untokenized string, as read from disk.
    public static ParallelTextCorpus CreateUntokenizedTestParallelCorpus()
    {
        return new ParallelTextCorpus(TextCorpus(SourceLines, UntokenizedRow), TextCorpus(TargetLines, UntokenizedRow));
    }

    private static DictionaryTextCorpus TextCorpus(string[] lines, Func<int, string, TextRow> rowFactory)
    {
        return new DictionaryTextCorpus(
            new MemoryText("text1", [.. lines.Select((line, i) => rowFactory(i + 1, line))])
        );
    }

    public static ThotSymmetrizedWordAlignmentModel CreateTrainedModel(
        IParallelTextCorpus corpus,
        ThotWordAlignmentModelType modelType = ThotWordAlignmentModelType.FastAlign
    )
    {
        var model = ThotSymmetrizedWordAlignmentModel.Create(modelType);
        model.Heuristic = SymmetrizationHeuristic.GrowDiagFinalAnd;
        model.EmitTrainingAlignments = true;
        using ITrainer trainer = model.CreateTrainer(corpus);
        trainer.TrainAsync().GetAwaiter().GetResult();
        trainer.SaveAsync().GetAwaiter().GetResult();
        return model;
    }

    public static ParallelTextCorpus CreateTwoTextParallelCorpus()
    {
        var src = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                [
                    new TextRow("text1", 1) { Segment = "el gato".Split(' ') },
                    new TextRow("text1", 2) { Segment = "la casa".Split(' ') },
                ]
            ),
            new MemoryText(
                "text2",
                [
                    new TextRow("text2", 1) { Segment = "el perro corre".Split(' ') },
                    new TextRow("text2", 2) { Segment = "la mesa".Split(' ') },
                ]
            )
        );

        var trg = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                [
                    new TextRow("text1", 1) { Segment = "the cat".Split(' ') },
                    new TextRow("text1", 2) { Segment = "the house".Split(' ') },
                ]
            ),
            new MemoryText(
                "text2",
                [
                    new TextRow("text2", 1) { Segment = "the dog runs".Split(' ') },
                    new TextRow("text2", 2) { Segment = "the table".Split(' ') },
                ]
            )
        );

        return new ParallelTextCorpus(src, trg);
    }

    private static TextRow Row(int rowRef, string segment)
    {
        return new TextRow("text1", rowRef) { Segment = segment.Split() };
    }

    private static TextRow UntokenizedRow(int rowRef, string segment)
    {
        return new TextRow("text1", rowRef) { Segment = [segment] };
    }
}
