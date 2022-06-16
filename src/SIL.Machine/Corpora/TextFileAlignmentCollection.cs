using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace SIL.Machine.Corpora
{
    public class TextFileAlignmentCollection : IAlignmentCollection
    {
        private readonly string _fileName;

        public TextFileAlignmentCollection(string id, string fileName)
        {
            Id = id;
            _fileName = fileName;
        }

        public string Id { get; }

        public string SortKey => Id;

        public bool MissingRowsAllowed => false;

        public int Count(bool includeEmpty = true)
        {
            using (var reader = new StreamReader(_fileName))
            {
                int count = 0;
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (includeEmpty || line.Trim().Length > 0)
                        count++;
                }
                return count;
            }
        }

        public IEnumerator<AlignmentRow> GetEnumerator()
        {
            return GetRows().GetEnumerator();
        }

        public IEnumerable<AlignmentRow> GetRows()
        {
            using (var reader = new StreamReader(_fileName))
            {
                int lineNum = 1;
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var textFileRef = new TextFileRef(Id, lineNum);
                    yield return new AlignmentRow(Id, textFileRef) { AlignedWordPairs = AlignedWordPair.Parse(line) };
                    lineNum++;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
