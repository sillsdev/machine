using System;
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
                    string[] columns = line.Split('\t');
                    object rowRef;
                    var flags = TextRowFlags.SentenceStart;
                    if (columns.Length > 1)
                    {
                        string[] keys = columns[0].Trim().Split('_');
                        rowRef = new MultiKeyRef(Id, keys.Select(k => int.TryParse(k, out int ki) ? (object)ki : k));
                        line = columns[1];
                        if (columns.Length == 3)
                        {
                            flags = TextRowFlags.None;
                            foreach (
                                string flagStr in columns[2]
                                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(f => f.Trim().ToLowerInvariant())
                            )
                            {
                                switch (flagStr)
                                {
                                    case "sentence_start":
                                    case "ss":
                                        flags |= TextRowFlags.SentenceStart;
                                        break;
                                    case "in_range":
                                    case "ir":
                                        flags |= TextRowFlags.InRange;
                                        break;
                                    case "range_start":
                                    case "rs":
                                        flags |= TextRowFlags.InRange | TextRowFlags.RangeStart;
                                        break;
                                }
                            }
                        }
                    }
                    else
                    {
                        rowRef = new MultiKeyRef(Id, lineNum);
                    }
                    yield return CreateRow(line, rowRef, flags);
                    lineNum++;
                }
            }
        }
    }
}
