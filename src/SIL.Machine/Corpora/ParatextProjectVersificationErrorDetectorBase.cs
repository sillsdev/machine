using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public abstract class ParatextProjectVersificationErrorDetectorBase
    {
        private readonly ParatextProjectSettings _settings;
        private readonly IParatextProjectFileHandler _paratextProjectFileHandler;

        protected ParatextProjectVersificationErrorDetectorBase(
            IParatextProjectFileHandler paratextProjectFileHandler,
            ParatextProjectSettings settings
        )
        {
            _settings = settings;
            _paratextProjectFileHandler = paratextProjectFileHandler;
        }

        public IReadOnlyList<UsfmVersificationError> GetUsfmVersificationErrors(
            UsfmVersificationErrorDetector handler = null,
            HashSet<int> books = null
        )
        {
            handler = handler ?? new UsfmVersificationErrorDetector(_settings);
            foreach (string bookId in _settings.GetAllScriptureBookIds())
            {
                string fileName = _settings.GetBookFileName(bookId);

                if (!_paratextProjectFileHandler.Exists(fileName))
                    continue;

                if (books != null && !books.Contains(Canon.BookIdToNumber(bookId)))
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
            }
            return handler.Errors;
        }
    }
}
