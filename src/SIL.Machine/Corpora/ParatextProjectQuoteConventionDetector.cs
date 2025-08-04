using System;
using System.IO;
using System.Text;
using SIL.Machine.Corpora.PunctuationAnalysis;

namespace SIL.Machine.Corpora
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
            handler = handler ?? new QuoteConventionDetector();
            foreach (string fileName in _settings.GetAllBookFileNames())
            {
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
            return handler.DetectQuotationConvention();
        }

        protected abstract bool Exists(string fileName);
        protected abstract Stream Open(string fileName);
    }
}
