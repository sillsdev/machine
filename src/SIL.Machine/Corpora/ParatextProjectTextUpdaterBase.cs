using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SIL.Machine.Corpora
{
    public abstract class ParatextProjectTextUpdaterBase
    {
        private readonly ParatextProjectSettings _settings;

        protected ParatextProjectTextUpdaterBase(ParatextProjectSettings settings)
        {
            _settings = settings;
        }

        protected ParatextProjectTextUpdaterBase(ParatextProjectSettingsParserBase settingsParser)
        {
            _settings = settingsParser.Parse();
        }

        public string UpdateUsfm(
            string bookId,
            IReadOnlyList<UpdateUsfmRow> rows,
            string fullName = null,
            UpdateUsfmTextBehavior textBehavior = UpdateUsfmTextBehavior.PreferExisting,
            UpdateUsfmMarkerBehavior paragraphBehavior = UpdateUsfmMarkerBehavior.Preserve,
            UpdateUsfmMarkerBehavior embedBehavior = UpdateUsfmMarkerBehavior.Preserve,
            UpdateUsfmMarkerBehavior styleBehavior = UpdateUsfmMarkerBehavior.Strip,
            IEnumerable<string> preserveParagraphStyles = null,
            IEnumerable<IUsfmUpdateBlockHandler> updateBlockHandlers = null,
            IEnumerable<string> remarks = null,
            Func<UsfmUpdateBlockHandlerException, bool> errorHandler = null,
            bool compareSegments = false
        )
        {
            string fileName = _settings.GetBookFileName(bookId);
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
                textBehavior,
                paragraphBehavior,
                embedBehavior,
                styleBehavior,
                preserveParagraphStyles,
                updateBlockHandlers,
                remarks,
                errorHandler,
                compareSegments
            );
            try
            {
                UsfmParser.Parse(usfm, handler, _settings.Stylesheet, _settings.Versification);
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

        protected abstract bool Exists(string fileName);
        protected abstract Stream Open(string fileName);
    }
}
