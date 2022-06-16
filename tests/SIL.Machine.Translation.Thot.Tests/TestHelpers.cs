using System;
using System.Collections.Generic;
using System.IO;
using SIL.Machine.Corpora;

namespace SIL.Machine.Translation.Thot
{
    public static class TestHelpers
    {
        public static string ToyCorpusHmmFolderName =>
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "data", "toy_corpus_hmm");
        public static string ToyCorpusHmmConfigFileName => Path.Combine(ToyCorpusHmmFolderName, "smt.cfg");

        public static string ToyCorpusFastAlignFolderName =>
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "data", "toy_corpus_fa");
        public static string ToyCorpusFastAlignConfigFileName => Path.Combine(ToyCorpusFastAlignFolderName, "smt.cfg");

        public static IEnumerable<string> Split(this string segment)
        {
            return segment.Split(' ');
        }

        public static ParallelTextCorpus CreateTestParallelCorpus()
        {
            var srcCorpus = new DictionaryTextCorpus(
                new MemoryText(
                    "text1",
                    new[]
                    {
                        Row(1, "isthay isyay ayay esttay-N ."),
                        Row(2, "ouyay ouldshay esttay-V oftenyay ."),
                        Row(3, "isyay isthay orkingway ?"),
                        Row(4, "isthay ouldshay orkway-V ."),
                        Row(5, "ityay isyay orkingway ."),
                        Row(6, "orkway-N ancay ebay ardhay !"),
                        Row(7, "ayay esttay-N ancay ebay ardhay ."),
                        Row(8, "isthay isyay ayay ordway !")
                    }
                )
            );

            var trgCorpus = new DictionaryTextCorpus(
                new MemoryText(
                    "text1",
                    new[]
                    {
                        Row(1, "this is a test N ."),
                        Row(2, "you should test V often ."),
                        Row(3, "is this working ?"),
                        Row(4, "this should work V ."),
                        Row(5, "it is working ."),
                        Row(6, "work N can be hard !"),
                        Row(7, "a test N can be hard ."),
                        Row(8, "this is a word !")
                    }
                )
            );

            return new ParallelTextCorpus(srcCorpus, trgCorpus);
        }

        private static TextRow Row(int rowRef, string segment)
        {
            return new TextRow("text1", rowRef) { Segment = segment.Split() };
        }
    }
}
