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
                ZipArchiveEntry keyTermsFileEntry = archive.GetEntry("ProjectBiblicalTerms.xml");
                if (keyTermsFileEntry is null)
                {
                    throw new ArgumentException(
                        $"The project directory does not contain a key terms file",
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
                XDocument keyTermsDoc;
                using (var keyTermsFile = keyTermsFileEntry.Open())
                {
                    keyTermsDoc = XDocument.Load(keyTermsFile);
                }
                IEnumerable<XElement> keyTermsElements = keyTermsDoc
                    .Descendants()
                    .Where(n => n.Name.LocalName == "Term");
                foreach (XElement element in keyTermsElements)
                {
                    if ((element.Element("Category")?.Value ?? "") != "PN")
                        continue;
                    string id = element.Attribute("Id").Value;
                    id = id.Replace("\n", "&#xA");
                    string gloss = element.Element("Gloss").Value;
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
                    glosses = glosses.Select(g => g.Trim()).Where(s => s != "").Distinct().ToList();
                    rows.Add(new TextRow("KeyTerms", id) { Segment = (IReadOnlyList<string>)glosses });
                }
                IText text = new MemoryText("KeyTerms", rows);
                AddText(text);
            }
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
