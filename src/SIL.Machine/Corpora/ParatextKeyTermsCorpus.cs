using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Linq;

namespace SIL.Machine.Corpora
{
    public class ParatextKeyTermsCorpus : DictionaryTextCorpus
    {
        public ParatextKeyTermsCorpus(string projectDir)
        {
            List<TextRow> rows = new List<TextRow>();
            string keyTermsFileName = Path.Combine(projectDir, "ProjectBiblicalTerms.xml");
            if (!File.Exists(keyTermsFileName))
            {
                throw new ArgumentException(
                    "The project directory does not contain a key terms file.",
                    nameof(projectDir)
                );
            }
            XDocument keyTermsDoc = XDocument.Load(keyTermsFileName);
            foreach (XElement element in keyTermsDoc.Elements("Term"))
            {
                string id = element.Attributes("Id").First().Name.LocalName;
                id = id.Replace("\n", "&#xA");
                string gloss = element.Elements("Gloss").First().Value;
                Regex rx = new Regex(@"\[(.+?)\]", RegexOptions.Compiled);
                Match match = rx.Match(gloss);
                if (match.Success)
                    gloss = match.Groups[0].Value;
                gloss = gloss.Replace("?", "");
                gloss = gloss.Trim();
                gloss = StripParens(gloss);
                gloss = StripParens(gloss, left: '[', right: ']');
                Regex rx2 = new Regex(@"\s+\d+(\.\d+)*$", RegexOptions.Compiled);
                foreach (Match m in rx2.Matches(gloss))
                {
                    gloss.Replace(m.Value, "");
                }
                IEnumerable<string> glosses = gloss.Split(';', ',', '/');
                glosses = glosses.Select(g => g.Trim()).Where(s => s != "").Distinct();
                rows.Add(new TextRow(projectDir + "_KeyTerms", id) { Segment = (IReadOnlyList<string>)glosses });
            }
            IText text = new MemoryText(projectDir + "_KeyTerms", rows); //TODO id?
            AddText(text);
        }

        private static string StripParens(string termString, char left = '(', char right = ')')
        {
            int parens = 0;
            int end = -1;
            for (int i = termString.Length - 1; i >= 0; i--)
            {
                char c = termString[i];
                if (c == right)
                {
                    if (parens == 0)
                        end = i + 1;
                    parens++;
                }
                else if (c == left)
                {
                    if (parens > 0)
                    {
                        parens--;
                        if (parens == 0)
                        {
                            termString =
                                termString.Substring(0, i) + termString.Substring(end, termString.Length - end);
                        }
                    }
                }
            }
            return termString;
        }
    }
}
