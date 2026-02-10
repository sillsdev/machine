using System.Collections.Generic;
using System.Linq;

namespace SIL.Machine.Corpora
{
    public class TextFileTextCorpus : DictionaryTextCorpus
    {
        public TextFileTextCorpus(params string[] filePatterns)
            : this(filePatterns, new List<TextRowContentType>()) { }

        public TextFileTextCorpus(IEnumerable<TextRowContentType> contentTypes = null, params string[] filePatterns)
            : this(filePatterns, contentTypes) { }

        public TextFileTextCorpus(IEnumerable<string> filePatterns, IEnumerable<TextRowContentType> contentTypes = null)
            : base(GetTexts(filePatterns, contentTypes)) { }

        private static IEnumerable<IText> GetTexts(
            IEnumerable<string> filePatterns,
            IEnumerable<TextRowContentType> contentTypes
        )
        {
            List<TextRowContentType> contentTypesList;
            if (contentTypes == null)
            {
                contentTypesList = new List<TextRowContentType>();
            }
            else
            {
                contentTypesList = contentTypes.ToList();
            }
            foreach ((string id, string fileName, int patternIndex) in CorporaUtils.GetFiles(filePatterns))
            {
                yield return new TextFileText(
                    id,
                    fileName,
                    patternIndex < contentTypesList.Count ? contentTypesList[patternIndex] : TextRowContentType.Segment
                );
            }
        }
    }
}
