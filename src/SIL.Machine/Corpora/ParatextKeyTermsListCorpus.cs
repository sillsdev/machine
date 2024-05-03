using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace SIL.Machine.Corpora
{
    public class ParatextKeyTermsListCorpus : DictionaryTextCorpus
    {
        private static readonly List<string> PredefinedTermsListTypes = new List<string>()
        {
            "Major",
            "All",
            "SilNt",
            "Pt6"
        };

        public ParatextKeyTermsListCorpus(string fileName, IEnumerable<string> termCategories)
        {
            // using static class check for the existence of the TermRenderings.xml file
            // load one of the two classes (ParatextBackupTermRenderingsCorpus and ParatextBiblicalTermsListCorpus)
            // if BTL check the language of the project and map to NLLB-200
            // Check the code against the available terms
            // Load the terms if they are available
            // filter by category

            List<TextRow> rows = new List<TextRow>();
            using (var archive = ZipFile.OpenRead(fileName))
            {
                ZipArchiveEntry termsFileEntry = archive.GetEntry("TermRenderings.xml");
                if (termsFileEntry is null)
                    return;

                var settingsParser = new ZipParatextProjectSettingsParser(archive);
                ParatextProjectSettings settings = settingsParser.Parse();

                XDocument termRenderingsDoc;
                using (Stream keyTermsFile = termsFileEntry.Open())
                {
                    termRenderingsDoc = XDocument.Load(keyTermsFile);
                }

                ZipArchiveEntry biblicalTermsFileEntry = archive.GetEntry(settings.BiblicalTermsFileName);

                XDocument biblicalTermsDoc;
                IDictionary<string, string> termIdToCategoryDictionary;
                if (PredefinedTermsListTypes.Contains(settings.BiblicalTermsListType))
                {
                    using (
                        Stream keyTermsFile = Assembly
                            .GetExecutingAssembly()
                            .GetManifestResourceStream("SIL.Machine.Corpora." + settings.BiblicalTermsFileName)
                    )
                    {
                        biblicalTermsDoc = XDocument.Load(keyTermsFile);
                        termIdToCategoryDictionary = GetCategoryPerId(biblicalTermsDoc);
                    }
                }
                else if (
                    settings.BiblicalTermsListType == "Project"
                    && settings.BiblicalTermsProjectName == settings.Name
                )
                {
                    using (Stream keyTermsFile = biblicalTermsFileEntry.Open())
                    {
                        biblicalTermsDoc = XDocument.Load(keyTermsFile);
                        termIdToCategoryDictionary = GetCategoryPerId(biblicalTermsDoc);
                    }
                }
                else
                {
                    termIdToCategoryDictionary = new Dictionary<string, string>();
                }

                IEnumerable<XElement> termsElements = termRenderingsDoc
                    .Descendants()
                    .Where(n => n.Name.LocalName == "TermRendering");

                string textId =
                    $"{settings.BiblicalTermsListType}:{settings.BiblicalTermsProjectName}:{settings.BiblicalTermsFileName}";
                foreach (XElement element in termsElements)
                {
                    string id = element.Attribute("Id").Value;
                    string category = "";
                    if (
                        (termCategories.Count() > 0 && !termIdToCategoryDictionary.TryGetValue(id, out category))
                        || (termCategories.Count() > 0 && !termCategories.Contains(category))
                    )
                    {
                        continue;
                    }
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
            //If entire term rendering is surrounded in square brackets, remove them
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

        private static IDictionary<string, string> GetCategoryPerId(XDocument biblicalTermsDocument)
        {
            return biblicalTermsDocument
                .Descendants()
                .Where(n => n.Name.LocalName == "Term")
                .ToDictionary(e => e.Attribute("Id").Value, e => e.Element("Category")?.Value ?? "");
        }
    }
}
