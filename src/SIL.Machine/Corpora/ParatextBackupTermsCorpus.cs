using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Linq;
using System.Reflection;

namespace SIL.Machine.Corpora
{
    public class ParatextBackupTermsCorpus : DictionaryTextCorpus
    {
        private static List<string> PREDEFINED_TERMS_LIST_TYPES = new List<string>() { "Major", "All", "SilNt", "Pt6" };

        public ParatextBackupTermsCorpus(string fileName, IEnumerable<string> termCategories)
        {
            List<TextRow> rows = new List<TextRow>();
            using (var archive = ZipFile.OpenRead(fileName))
            {
                ZipArchiveEntry termsFileEntry = archive.GetEntry("TermRenderings.xml");
                if (termsFileEntry is null)
                    return;

                ZipArchiveEntry settingsEntry = archive.GetEntry("Settings.xml");
                if (settingsEntry == null)
                    settingsEntry = archive.Entries.FirstOrDefault(e => e.FullName.EndsWith(".ssf"));
                if (settingsEntry == null)
                {
                    throw new ArgumentException(
                        "The project backup does not contain a settings file.",
                        nameof(fileName)
                    );
                }
                XDocument settingsDoc;
                using (Stream stream = settingsEntry.Open())
                {
                    settingsDoc = XDocument.Load(stream);
                }
                string textId = settingsDoc.Root.Element("BiblicalTermsListSetting").Value;

                XDocument termRenderingsDoc;
                using (var keyTermsFile = termsFileEntry.Open())
                {
                    termRenderingsDoc = XDocument.Load(keyTermsFile);
                }

                ZipArchiveEntry biblicalTermsFileEntry = archive.GetEntry(textId.Split(':').Last());

                XDocument biblicalTermsDoc;
                if (PREDEFINED_TERMS_LIST_TYPES.Contains(textId.Split(':').First()))
                {
                    using (
                        var keyTermsFile = Assembly
                            .GetExecutingAssembly()
                            .GetManifestResourceStream("SIL.Machine.Corpora." + textId.Split(':').Last())
                    )
                    {
                        biblicalTermsDoc = XDocument.Load(keyTermsFile);
                    }
                }
                else
                {
                    using (var keyTermsFile = biblicalTermsFileEntry.Open())
                    {
                        biblicalTermsDoc = XDocument.Load(keyTermsFile);
                    }
                }

                IEnumerable<XElement> termsElements = termRenderingsDoc
                    .Descendants()
                    .Where(n => n.Name.LocalName == "TermRendering");
                foreach (XElement element in termsElements)
                {
                    string id = element.Attribute("Id").Value;
                    if (
                        (
                            termCategories.Count() > 0
                            && !termCategories.Contains(
                                biblicalTermsDoc
                                    .Descendants()
                                    .Where(n => (n.Name.LocalName == "Term") && (n.Attribute("Id").Value == id))
                                    .DefaultIfEmpty(new XElement("Empty"))
                                    .First()
                                    .Element("Category")
                                    ?.Value ?? ""
                            )
                        )
                    )
                        continue;
                    id = id.Replace("\n", "&#xA");
                    string rendering = element.Element("Renderings").Value;
                    IReadOnlyList<string> renderings = GetRenderings(rendering);
                    rows.Add(new TextRow(textId, id) { Segment = renderings });
                }
                IText text = new MemoryText(textId, rows);
                AddText(text);
            }
        }

        public static IReadOnlyList<string> GetRenderings(string rendering)
        {
            //If entire term rednering is surrounded in square brackets, remove them
            Regex rx = new Regex(@"^\[(.+?)\]$", RegexOptions.Compiled);
            Match match = rx.Match(rendering);
            if (match.Success)
                rendering = match.Groups[0].Value;
            rendering = rendering.Replace("?", "");
            rendering = rendering.Replace("*", "");
            rendering = rendering.Replace("/", " ");
            rendering = rendering.Trim();
            rendering = StripParens(rendering);
            rendering = StripParens(rendering, left: '[', right: ']');
            Regex rx2 = new Regex(@"\s+\d+(\.\d+)*$", RegexOptions.Compiled);
            foreach (Match m in rx2.Matches(rendering))
            {
                rendering.Replace(m.Value, "");
            }
            IEnumerable<string> glosses = Regex.Split(rendering, @"\|\|");
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
