using System;
using System.Collections.Generic;

namespace SIL.Machine.Tokenization.SentencePiece
{
    public enum SentencePieceModelType
    {
        Unigram,
        Bpe,
        Word,
        Char
    }

    public class SentencePieceTrainer
    {
        public int? VocabSize { get; set; }
        public double? CharacterCoverage { get; set; }
        public SentencePieceModelType? ModelType { get; set; }
        public int? InputSentenceSize { get; set; }
        public bool? ShuffleInputSentence { get; set; }
        public IReadOnlyList<string> ControlSymbols => new List<string>();
        public IReadOnlyList<string> UserDefinedSymbols => new List<string>();
        public string ExtraOptions { get; set; }

        public void Train(IEnumerable<string> inputFilenames, string modelPrefix)
        {
            var args = new List<string>();
            if (VocabSize != null)
                args.Add($"--vocab_size={VocabSize}");
            if (CharacterCoverage != null)
                args.Add($"--character_coverage={CharacterCoverage}");
            if (ModelType != null)
                args.Add($"--model_type={ModelType.ToString().ToLowerInvariant()}");
            if (InputSentenceSize != null)
                args.Add($"--input_sentence_size={InputSentenceSize}");
            if (ShuffleInputSentence != null)
                args.Add($"--shuffle_input_sentence={ShuffleInputSentence}");
            if (ControlSymbols.Count > 0)
                args.Add($"--control_symbols={string.Join(",", ControlSymbols)}");
            if (UserDefinedSymbols.Count > 0)
                args.Add($"--user_defined_symbols={string.Join(",", UserDefinedSymbols)}");
            if (!string.IsNullOrEmpty(ExtraOptions))
                args.Add(ExtraOptions);

            StatusCode code = SentencePieceApi.sp_train(
                string.Join(",", inputFilenames),
                modelPrefix,
                string.Join(" ", args)
            );
            if (code != StatusCode.Ok)
                throw new InvalidOperationException($"Error occurred while training, code: {code}.");
        }

        public void Train(string inputFilename, string modelPrefix)
        {
            Train(new[] { inputFilename }, modelPrefix);
        }
    }
}
