using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Tokenization;

namespace SIL.Machine.Translation.Llm
{
    public class TfidfExampleRetriever : TopKExampleRetriever
    {
        private readonly LatinWordTokenizer _tokenizer = new LatinWordTokenizer();
        private TfidfIndex _index;

        public TfidfExampleRetriever(IEnumerable<Example> examples)
            : base(examples)
        {
            Fit();
        }

        protected override void Fit()
        {
            _index = TfidfIndex.Create(ExampleList.Select(e => Tokenize(e.Source)).ToArray());
        }

        protected override double[] ScoreExamples(string query)
        {
            if (_index == null)
                return Array.Empty<double>();
            return _index.Similarities(_index.Vectorize(Tokenize(query)));
        }

        private IReadOnlyList<string> Tokenize(string text)
        {
            // Strip all punctuation-only tokens.
            return _tokenizer
                .Tokenize(text.ToLowerInvariant())
                .Where(token => token.Any(char.IsLetterOrDigit))
                .ToArray();
        }
    }
}
