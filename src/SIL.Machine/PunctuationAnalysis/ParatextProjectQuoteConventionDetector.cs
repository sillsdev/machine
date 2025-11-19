using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SIL.Machine.Corpora;
using SIL.Scripture;

namespace SIL.Machine.PunctuationAnalysis
{
    public abstract class ParatextProjectQuoteConventionDetector
    {
        private readonly ParatextProjectSettings _settings;
        private readonly IParatextProjectFileHandler _paratextProjectFileHandler;

        protected ParatextProjectQuoteConventionDetector(
            IParatextProjectFileHandler paratextProjectFileHandler,
            ParatextProjectSettings settings
        )
        {
            _settings = settings;
            _paratextProjectFileHandler = paratextProjectFileHandler;
        }

        public QuoteConventionAnalysis GetQuoteConventionAnalysis(
            IReadOnlyDictionary<int, List<int>> includeChapters = null
        )
        {
            var bookQuoteConventionsAnalyses = new List<QuoteConventionAnalysis>();

            foreach (
                string bookId in Canon
                    .AllBookNumbers.Where(num => Canon.IsCanonical(num))
                    .Select(num => Canon.BookNumberToId(num))
            )
            {
                if (includeChapters != null && !includeChapters.ContainsKey(Canon.BookIdToNumber(bookId)))
                    continue;

                var handler = new QuoteConventionDetector();

                string fileName = _settings.GetBookFileName(bookId);
                if (!_paratextProjectFileHandler.Exists(fileName))
                    continue;

                string usfm;
                using (var reader = new StreamReader(_paratextProjectFileHandler.Open(fileName)))
                {
                    usfm = reader.ReadToEnd();
                }

                try
                {
                    UsfmParser.Parse(usfm, handler, _settings.Stylesheet, _settings.Versification);
                }
                catch (Exception ex)
                {
                    var sb = new StringBuilder();
                    sb.Append($"An error occurred while parsing the usfm for '{fileName}`");
                    if (!string.IsNullOrEmpty(_settings.Name))
                        sb.Append($" in project '{_settings.Name}'");
                    sb.Append($". Error: '{ex.Message}'");
                    throw new InvalidOperationException(sb.ToString(), ex);
                }
                bookQuoteConventionsAnalyses.Add(handler.DetectQuoteConvention(includeChapters));
            }
            return QuoteConventionAnalysis.CombineWithWeightedAverage(bookQuoteConventionsAnalyses);
        }
    }
}
