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

    public static ParallelTextCorpus CreateTestParallelCorpus()
    {
        var srcCorpus = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                [
                    Row(1, "isthay isyay ayay esttay-N ."),
                    Row(2, "ouyay ouldshay esttay-V oftenyay ."),
                    Row(3, "isyay isthay orkingway ?"),
                    Row(4, "isthay ouldshay orkway-V ."),
                    Row(5, "ityay isyay orkingway ."),
                    Row(6, "orkway-N ancay ebay ardhay !"),
                    Row(7, "ayay esttay-N ancay ebay ardhay ."),
                    Row(8, "isthay isyay ayay ordway !"),
                ]
            )
        );

        var trgCorpus = new DictionaryTextCorpus(
            new MemoryText(
                "text1",
                [
                    Row(1, "this is a test N ."),
                    Row(2, "you should test V often ."),
                    Row(3, "is this working ?"),
                    Row(4, "this should work V ."),
                    Row(5, "it is working ."),
                    Row(6, "work N can be hard !"),
                    Row(7, "a test N can be hard ."),
                    Row(8, "this is a word !"),
                ]
            )
        );

        return new ParallelTextCorpus(srcCorpus, trgCorpus);
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

    public static async Task<ThotSymmetrizedWordAlignmentModel> CreateWordAligner<T>(IParallelTextCorpus corpus)
        where T : ThotWordAlignmentModel, new()
    {
        var aligner = new ThotSymmetrizedWordAlignmentModel(new T(), new T())
        {
            Heuristic = SymmetrizationHeuristic.GrowDiagFinalAnd,
            // Retain the alignments computed during training so that the corpus can be aligned
            // without a separate, potentially expensive, inference pass.
            EmitTrainingAlignments = true,
        };
        ITrainer trainer = aligner.CreateTrainer(corpus);
        await trainer.TrainAsync();
        await trainer.SaveAsync();
        return aligner;
    }

    private static TextRow Row(int rowRef, string segment)
    {
        return new TextRow("text1", rowRef) { Segment = segment.Split() };
    }
}
