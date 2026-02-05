using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;

namespace SIL.Machine.Corpora
{
    public class ParatextBackupTermsCorpus : DictionaryTextCorpus
    {
        public ParatextBackupTermsCorpus(
            string fileName,
            IEnumerable<string> termCategories,
            bool useTermGlosses = true,
            IDictionary<string, HashSet<int>> chapters = null
        )
        {
            using (var archive = ZipFile.OpenRead(fileName))
            {
                IEnumerable<KeyTerm> keyTerms = new ZipParatextProjectTermsParser(archive)
                    .Parse(termCategories, useTermGlosses, chapters)
                    .OrderBy(g => g.Id);

                ParatextProjectSettings settings = ZipParatextProjectSettingsParser.Parse(archive);

                string textId =
                    $"{settings.BiblicalTermsListType}:{settings.BiblicalTermsProjectName}:{settings.BiblicalTermsFileName}";

                IText text = new MemoryText(
                    textId,
                    keyTerms.SelectMany(keyTerm =>
                        keyTerm.Renderings.Select(gloss => new TextRow(textId, keyTerm.Id, TextRowContentType.Word)
                        {
                            Segment = new string[] { gloss },
                        })
                    )
                );
                AddText(text);
            }
        }
    }
}
