using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public abstract class ParatextProjectVersificationConverterBase
    {
        private readonly ParatextProjectSettings _settings;
        private readonly IParatextProjectFileHandler _paratextProjectFileHandler;

        protected ParatextProjectVersificationConverterBase(
            IParatextProjectFileHandler paratextProjectFileHandler,
            ParatextProjectSettings settings
        )
        {
            _settings = settings;
            _paratextProjectFileHandler = paratextProjectFileHandler;
        }

        public string UpdateUsfm(string bookId, ScrVers targetVersification)
        {
            string fileName = _settings.GetBookFileName(bookId);
            if (!Exists(fileName))
                return null;

            string usfm;
            using (var reader = new StreamReader(Open(fileName)))
            {
                usfm = reader.ReadToEnd();
            }

            var handler = new ConvertUsfmVersificationHandler(targetVersification);
            try
            {
                var tokenizer = new UsfmTokenizer(_settings.Stylesheet);
                IReadOnlyList<UsfmToken> tokens = tokenizer.Tokenize(usfm);
                UsfmParser.Parse(tokens, handler, _settings.Stylesheet, _settings.Versification);
                return handler.GetUsfm(_settings.Stylesheet);
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                sb.Append($"An error occurred while parsing the usfm for '{bookId}`");
                if (!string.IsNullOrEmpty(_settings.Name))
                    sb.Append($" in project '{_settings.Name}'");
                sb.Append($". Error: '{ex.Message}'");
                throw new InvalidOperationException(sb.ToString(), ex);
            }
        }

        private bool Exists(string fileName) => _paratextProjectFileHandler.Exists(fileName);

        private Stream Open(string fileName) => _paratextProjectFileHandler.Open(fileName);
    }
}
