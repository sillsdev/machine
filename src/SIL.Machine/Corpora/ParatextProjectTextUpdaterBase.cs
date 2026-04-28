using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SIL.Machine.Corpora
{
    public abstract class ParatextProjectTextUpdaterBase
    {
        private readonly ParatextProjectSettings _settings;
        private readonly IParatextProjectFileHandler _paratextProjectFileHandler;

        protected ParatextProjectTextUpdaterBase(
            IParatextProjectFileHandler paratextProjectFileHandler,
            ParatextProjectSettings settings
        )
        {
            _settings = settings;
            _paratextProjectFileHandler = paratextProjectFileHandler;
        }

        public string UpdateUsfm(
            string bookId,
            IReadOnlyList<UpdateUsfmRow> rows,
            IReadOnlyList<int> chapters = null,
            string fullName = null,
            UpdateUsfmTextBehavior textBehavior = UpdateUsfmTextBehavior.PreferExisting,
            UpdateUsfmMarkerBehavior paragraphBehavior = UpdateUsfmMarkerBehavior.Preserve,
            UpdateUsfmMarkerBehavior embedBehavior = UpdateUsfmMarkerBehavior.Preserve,
            UpdateUsfmMarkerBehavior styleBehavior = UpdateUsfmMarkerBehavior.Strip,
            IEnumerable<string> preserveParagraphStyles = null,
            IEnumerable<IUsfmUpdateBlockHandler> updateBlockHandlers = null,
            IEnumerable<(int, string)> remarks = null,
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
                var tokenizer = new UsfmTokenizer(_settings.Stylesheet);
                IReadOnlyList<UsfmToken> tokens = tokenizer.Tokenize(usfm);
                tokens = FilterTokensByChapter(tokens, chapters);
                var parser = new UsfmParser(tokens, handler, _settings.Stylesheet, _settings.Versification);
                parser.ProcessTokens();
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

        /// <summary>
        /// Filters tokens by the specified chapters.
        /// </summary>
        /// <param name="tokens">The tokens.</param>
        /// <param name="chapters">The chapters. If null, all tokens are returned.</param>
        /// <returns>The filtered tokens.</returns>
        /// <remarks>This is marked internal so test classes can use it.</remarks>
        internal static IReadOnlyList<UsfmToken> FilterTokensByChapter(
            IReadOnlyList<UsfmToken> tokens,
            IReadOnlyList<int> chapters = null
        )
        {
            if (chapters is null)
                return tokens;

            var tokensWithinChapters = new List<UsfmToken>();
            bool inChapter = false;
            bool inIdMarker = false;

            for (int index = 0; index < tokens.Count; index++)
            {
                UsfmToken token = tokens[index];
                if (index == 0 && token.Marker == "id")
                {
                    inIdMarker = true;
                    if (chapters.Contains(1))
                        inChapter = true;
                }
                else if (inIdMarker && token.Marker != null && token.Marker != "id")
                {
                    inIdMarker = false;
                }
                else if (token.Type == UsfmTokenType.Chapter)
                {
                    inChapter =
                        !string.IsNullOrEmpty(token.Data)
                        && int.TryParse(token.Data, out int chapter)
                        && chapters.Contains(chapter);
                }

                if (inIdMarker || inChapter)
                    tokensWithinChapters.Add(token);
            }

            return tokensWithinChapters;
        }

        private bool Exists(string fileName) => _paratextProjectFileHandler.Exists(fileName);

        private Stream Open(string fileName) => _paratextProjectFileHandler.Open(fileName);
    }
}
