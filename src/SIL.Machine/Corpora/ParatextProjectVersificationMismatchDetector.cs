using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SIL.Machine.Corpora
{
    public abstract class ParatextProjectVersificationMismatchDetector
    {
        private readonly ParatextProjectSettings _settings;
        private readonly IParatextProjectFileHandler _paratextProjectFileHandler;

        protected ParatextProjectVersificationMismatchDetector(IParatextProjectFileHandler paratextProjectFileHandler)
        {
            _settings = _paratextProjectFileHandler.GetSettings();
            _paratextProjectFileHandler = paratextProjectFileHandler;
        }

        public IReadOnlyList<UsfmVersificationMismatch> GetUsfmVersificationMismatches(
            UsfmVersificationMismatchDetector handler = null
        )
        {
            handler = handler ?? new UsfmVersificationMismatchDetector(_settings.Versification);
            foreach (string fileName in _settings.GetAllScriptureBookFileNames())
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
            return handler.Errors;
        }

        private bool Exists(string fileName) => _paratextProjectFileHandler.Exists(fileName);

        private Stream Open(string fileName) => _paratextProjectFileHandler.Open(fileName);
    }
}
