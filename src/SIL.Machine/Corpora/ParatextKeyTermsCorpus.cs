using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Linq;

namespace SIL.Machine.Corpora
{
    public class ParatextKeyTermsCorpus : DictionaryTextCorpus
    {
        public string BiblicalTermsType { get; set; }

        public ParatextKeyTermsCorpus(string projectDir)
        {
            List<TextRow> rows = new List<TextRow>();
            using (var archive = ZipFile.OpenRead(projectDir))
            {
                ZipArchiveEntry termsFileEntry = archive.GetEntry("TermRenderings.xml");
                if (termsFileEntry is null)
                {
                    throw new ArgumentException(
                        $"The project directory does not contain a term renderings file",
                        nameof(projectDir)
                    );
                }
                ZipArchiveEntry settingsEntry = archive.GetEntry("Settings.xml");
                if (settingsEntry == null)
                    settingsEntry = archive.Entries.FirstOrDefault(e => e.FullName.EndsWith(".ssf"));
                if (settingsEntry == null)
                {
                    throw new ArgumentException(
                        "The project backup does not contain a settings file.",
                        nameof(projectDir)
                    );
                }
                XDocument settingsDoc;
                using (Stream stream = settingsEntry.Open())
                {
                    settingsDoc = XDocument.Load(stream);
                }
                BiblicalTermsType = settingsDoc.Root.Element("BiblicalTermsListSetting").Value;
                XDocument termsDoc;
                using (var keyTermsFile = termsFileEntry.Open())
                {
                    termsDoc = XDocument.Load(keyTermsFile);
                }
                IEnumerable<XElement> termsElements = termsDoc
                    .Descendants()
                    .Where(n => n.Name.LocalName == "TermRendering");
                foreach (XElement element in termsElements)
                {
                    string id = element.Attribute("Id").Value;
                    id = id.Replace("\n", "&#xA");
                    string gloss = element.Element("Renderings").Value;
                    IReadOnlyList<string> glosses = GetGlosses(gloss);
                    rows.Add(new TextRow("KeyTerms", id) { Segment = glosses });
                }
                IText text = new MemoryText("KeyTerms", rows);
                AddText(text);
            }
        }

        public static IReadOnlyList<string> GetGlosses(string gloss)
        {
            //If entire term rednering is surrounded in square brackets, remove them
            Regex rx = new Regex(@"^\[(.+?)\]$", RegexOptions.Compiled);
            Match match = rx.Match(gloss);
            if (match.Success)
                gloss = match.Groups[0].Value;
            gloss = gloss.Replace("?", "");
            gloss = gloss.Replace("*", "");
            gloss = gloss.Replace("/", " ");
            gloss = gloss.Trim();
            gloss = StripParens(gloss);
            gloss = StripParens(gloss, left: '[', right: ']');
            Regex rx2 = new Regex(@"\s+\d+(\.\d+)*$", RegexOptions.Compiled);
            foreach (Match m in rx2.Matches(gloss))
            {
                gloss.Replace(m.Value, "");
            }
            IEnumerable<string> glosses = Regex.Split(gloss, @"\|\|");
            glosses = glosses.Select(g => g.Trim()).Where(s => s != "").Distinct().ToList();
            return (IReadOnlyList<string>)glosses;
        }

        /// <summary>
        /// Strips all content between left and right parentheses "left" and "right" and returns resultant string
        /// </summary>
        /// <param name="termString">The string to be modified</param>
        /// <param name="left">The desired left parenthesis char e.g. [,{,(, etc.</param>
        /// <param name="right">The desired right parenthesis char e.g. ],},) etc.</param>
        /// <returns>String stripped of content between parentheses</returns>
        public static string StripParens(string termString, char left = '(', char right = ')')
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
