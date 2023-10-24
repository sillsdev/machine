using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

        public int Count(bool includeEmpty = true)
        {
            using (var reader = new StreamReader(_fileName))
            {
                int count = 0;
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (includeEmpty)
                    {
                        count++;
                    }
                    else if (line.Length > 0)
                    {
                        int index = line.IndexOf("\t");
                        if (index >= 0)
                            line = line.Substring(index + 1);
                        if (line.Trim().Length > 0)
                            count++;
                    }
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
                    int index = line.IndexOf("\t");
                    object rowRef;
                    if (index >= 0)
                    {
                        string[] keys = line.Substring(0, index).Trim().Split('-', '_');
                        rowRef = new MultiKeyRef(Id, keys.Select(k => int.TryParse(k, out int ki) ? (object)ki : k));
                        line = line.Substring(index + 1);
                    }
                    else
                    {
                        rowRef = new MultiKeyRef(Id, lineNum);
                    }
                    yield return new AlignmentRow(Id, rowRef) { AlignedWordPairs = AlignedWordPair.Parse(line) };
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
