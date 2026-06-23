using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public abstract class UsfmVersificationAnalyzerBase
    {
        private readonly ParatextProjectSettings _settings;
        private readonly IParatextProjectFileHandler _paratextProjectFileHandler;

        protected UsfmVersificationAnalyzerBase(
            IParatextProjectFileHandler paratextProjectFileHandler,
            ParatextProjectSettings settings
        )
        {
            _settings = settings;
            _paratextProjectFileHandler = paratextProjectFileHandler;
        }

        public UsfmVersificationAnalysis AnalyzeUsfmVersification(
            Dictionary<string, HashSet<int>> bookIdsAndChapters,
            UsfmVersificationAnalyzerHandler handler = null
        )
        {
            return AnalyzeUsfmVersification(
                bookIdsAndChapters?.ToDictionary(b => Canon.BookIdToNumber(b.Key), b => b.Value),
                handler
            );
        }

        public UsfmVersificationAnalysis AnalyzeUsfmVersification(
            Dictionary<int, HashSet<int>> bookNumsAndChapters,
            UsfmVersificationAnalyzerHandler handler = null
        )
        {
            handler = handler ?? new UsfmVersificationAnalyzerHandler(_settings, bookNumsAndChapters);
            foreach (string bookId in _settings.GetAllScriptureBookIds())
            {
                string fileName = _settings.GetBookFileName(bookId);

                if (!_paratextProjectFileHandler.Exists(fileName))
                    continue;

                if (
                    bookNumsAndChapters != null
                    && !bookNumsAndChapters.TryGetValue(Canon.BookIdToNumber(bookId), out _)
                )
                {
                    continue;
                }

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
            }
            return handler.GetAnalysis();
        }
    }
}
