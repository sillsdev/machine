using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SIL.Machine.Corpora
{
    public class TextFileText : TextBase
    {
        public TextFileText(string id, string fileName) : base(id, id)
        {
            FileName = fileName;
        }

        public string FileName { get; }

        public override bool MissingRowsAllowed => false;

        public override int Count(bool includeEmpty = true)
        {
            using (var reader = new StreamReader(FileName))
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

        public override IEnumerable<TextRow> GetRows()
        {
            using (var reader = new StreamReader(FileName))
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
                    yield return CreateRow(line, rowRef);
                    lineNum++;
                }
            }
        }
    }
}
