using System.Collections.Generic;
using System.IO;

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
                    yield return CreateRow(line, new TextFileRef(Id, lineNum));
                    lineNum++;
                }
            }
        }
    }
}
