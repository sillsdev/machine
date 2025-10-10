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

        protected ParatextProjectQuoteConventionDetector(ParatextProjectSettings settings)
        {
            _settings = settings;
        }

        protected ParatextProjectQuoteConventionDetector(ParatextProjectSettingsParserBase settingsParser)
        {
            _settings = settingsParser.Parse();
        }

        public QuoteConventionAnalysis GetQuoteConventionAnalysis(QuoteConventionDetector handler = null)
        {
            Dictionary<int, List<int>> includeChapters = null;
            return GetQuoteConventionAnalysis(handler, includeChapters);
        }

        public QuoteConventionAnalysis GetQuoteConventionAnalysis(
            QuoteConventionDetector handler = null,
            IReadOnlyDictionary<string, List<int>> includeChapters = null
        )
        {
            return GetQuoteConventionAnalysis(
                handler,
                includeChapters?.ToDictionary(kvp => Canon.BookIdToNumber(kvp.Key), kvp => kvp.Value)
            );
        }

        public QuoteConventionAnalysis GetQuoteConventionAnalysis(
            QuoteConventionDetector handler = null,
            IReadOnlyDictionary<int, List<int>> includeChapters = null
        )
        {
            handler = handler ?? new QuoteConventionDetector();
            foreach (
                string bookId in Canon
                    .AllBookNumbers.Where(num => Canon.IsCanonical(num))
                    .Select(num => Canon.BookNumberToId(num))
            )
            {
                if (includeChapters != null && !includeChapters.ContainsKey(Canon.BookIdToNumber(bookId)))
                    continue;

                string fileName = _settings.GetBookFileName(bookId);
                if (!Exists(fileName))
                    continue;

                string usfm;
                using (var reader = new StreamReader(Open(fileName)))
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
            }
            return handler.DetectQuoteConvention(includeChapters);
        }

        protected abstract bool Exists(string fileName);
        protected abstract Stream Open(string fileName);
    }
}
