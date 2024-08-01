using System.Collections.Generic;
using System.IO;

namespace SIL.Machine.Corpora
{
    public abstract class ParatextProjectTextUpdaterBase
    {
        private readonly ParatextProjectSettingsParserBase _settingsParser;

        protected ParatextProjectTextUpdaterBase(ParatextProjectSettingsParserBase settingsParser)
        {
            _settingsParser = settingsParser;
        }

        public string UpdateUsfm(
            string bookId,
            IReadOnlyList<(IReadOnlyList<ScriptureRef>, string)> rows,
            string fullName = null,
            bool stripAllText = false,
            bool preferExistingText = true
        )
        {
            ParatextProjectSettings settings = _settingsParser.Parse();

            string fileName = settings.GetBookFileName(bookId);
            if (!Exists(fileName))
                return null;

            string usfm;
            using (var reader = new StreamReader(Open(fileName)))
            {
                usfm = reader.ReadToEnd();
            }

            var handler = new UpdateUsfmParserHandler(
                rows,
                fullName is null ? null : $"- {fullName}",
                stripAllText,
                preferExistingText: preferExistingText
            );
            UsfmParser.Parse(usfm, handler, settings.Stylesheet, settings.Versification);
            return handler.GetUsfm(settings.Stylesheet);
        }

        protected abstract bool Exists(string fileName);
        protected abstract Stream Open(string fileName);
    }
}
