using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using SIL.Extensions;

namespace SIL.Machine.Corpora
{
    public abstract class ParatextTermsCorpusBase : DictionaryTextCorpus
    {
        private static readonly List<string> PredefinedTermsListTypes = new List<string>()
        {
            "Major",
            "All",
            "SilNt",
            "Pt6"
        };

        private static readonly Dictionary<string, string> SupportedLanguageTermsLocalizationXmls = new Dictionary<
            string,
            string
        >()
        {
            { "en", "SIL.Machine.Corpora.BiblicalTermsEn.xml" },
            { "es", "SIL.Machine.Corpora.BiblicalTermsEs.xml" },
            { "fr", "SIL.Machine.Corpora.BiblicalTermsFr.xml" },
            { "id", "SIL.Machine.Corpora.BiblicalTermsId.xml" },
            { "pt", "SIL.Machine.Corpora.BiblicalTermsPt.xml" }
        };

        private static readonly Regex ContentInBracketsRegex = new Regex(@"^\[(.+?)\]$", RegexOptions.Compiled);
        private static readonly Regex NumericalInformationRegex = new Regex(@"\s+\d+(\.\d+)*$", RegexOptions.Compiled);

        protected void AddTexts(
            ParatextProjectSettings settings,
            IEnumerable<string> termCategories,
            bool useTermGlosses = true
        )
        {
            XDocument biblicalTermsDoc;
            IDictionary<string, string> termIdToCategoryDictionary;
            if (settings.BiblicalTermsListType == "Project")
            {
                if (Exists(settings.BiblicalTermsFileName))
                {
                    using (Stream keyTermsFile = Open(settings.BiblicalTermsFileName))
                    {
                        biblicalTermsDoc = XDocument.Load(keyTermsFile);
                        termIdToCategoryDictionary = GetCategoryPerId(biblicalTermsDoc);
                    }
                }
                else
                {
                    using (
                        Stream keyTermsFile = Assembly
                            .GetExecutingAssembly()
                            .GetManifestResourceStream("SIL.Machine.Corpora.BiblicalTerms.xml")
                    )
                    {
                        biblicalTermsDoc = XDocument.Load(keyTermsFile);
                        termIdToCategoryDictionary = GetCategoryPerId(biblicalTermsDoc);
                    }
                }
            }
            else if (PredefinedTermsListTypes.Contains(settings.BiblicalTermsListType))
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
            else
            {
                termIdToCategoryDictionary = new Dictionary<string, string>();
            }

            XDocument termsGlossesDoc = null;
            if (
                settings.LanguageCode != null
                && settings.BiblicalTermsListType == "Major"
                && SupportedLanguageTermsLocalizationXmls.TryGetValue(settings.LanguageCode, out string resourceName)
            )
            {
                using (Stream keyTermsFile = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    termsGlossesDoc = XDocument.Load(keyTermsFile);
                }
            }

            XDocument termRenderingsDoc = null;
            if (Exists("TermRenderings.xml"))
            {
                using (Stream keyTermsFile = Open("TermRenderings.xml"))
                {
                    termRenderingsDoc = XDocument.Load(keyTermsFile);
                }
            }

            IDictionary<string, IEnumerable<string>> termsRenderings = new Dictionary<string, IEnumerable<string>>();
            if (termRenderingsDoc != null)
            {
                termsRenderings = termRenderingsDoc
                    .Descendants()
                    .Where(n => n.Name.LocalName == "TermRendering")
                    .Select(ele => (ele.Attribute("Id").Value, ele))
                    .Where(kvp => IsInCategory(kvp.Item1, termCategories, termIdToCategoryDictionary))
                    .Select(kvp =>
                    {
                        string id = kvp.Item1.Replace("\n", "&#xA");
                        string gloss = kvp.Item2.Element("Renderings").Value;
                        IReadOnlyList<string> glosses = GetGlosses(gloss);
                        return (id, glosses);
                    })
                    .GroupBy(kvp => kvp.Item1, kvp => kvp.Item2) //Handle duplicate term ids (which do exist) e.g. שִׁלֵּמִי
                    .Select(grouping => (grouping.Key, grouping.SelectMany(g => g)))
                    .ToDictionary(kvp => kvp.Item1, kvp => kvp.Item2);
            }

            IDictionary<string, IEnumerable<string>> termsGlosses = new Dictionary<string, IEnumerable<string>>();
            if (termsGlossesDoc != null && useTermGlosses)
            {
                termsGlosses = termsGlossesDoc
                    .Descendants()
                    .Where(n => n.Name.LocalName == "Localization")
                    .Select(ele => (ele.Attribute("Id").Value, ele))
                    .Where(kvp => IsInCategory(kvp.Item1, termCategories, termIdToCategoryDictionary))
                    .Select(kvp =>
                    {
                        string id = kvp.Item1.Replace("\n", "&#xA");
                        string gloss = kvp.Item2.Attribute("Gloss").Value;
                        IReadOnlyList<string> glosses = GetGlosses(gloss);
                        return (id, glosses);
                    })
                    .GroupBy(kvp => kvp.Item1, kvp => kvp.Item2)
                    .Select(grouping => (grouping.Key, grouping.SelectMany(g => g)))
                    .ToDictionary(kvp => kvp.Item1, kvp => kvp.Item2);
            }
            if (termsGlosses.Count > 0 || termsRenderings.Count > 0)
                AddTerms(termsRenderings, termsGlosses, settings);
        }

        private static bool IsInCategory(
            string id,
            IEnumerable<string> termCategories,
            IDictionary<string, string> termIdToCategoryDictionary
        )
        {
            string category;
            return (termCategories.Count() == 0)
                || (termIdToCategoryDictionary.TryGetValue(id, out category) && termCategories.Contains(category));
        }

        private void AddTerms(
            IDictionary<string, IEnumerable<string>> termsRenderings,
            IDictionary<string, IEnumerable<string>> termsGlosses,
            ParatextProjectSettings settings
        )
        {
            string textId =
                $"{settings.BiblicalTermsListType}:{settings.BiblicalTermsProjectName}:{settings.BiblicalTermsFileName}";

            //Prefer renderings to gloss localizations
            IDictionary<string, IEnumerable<string>> glosses = termsRenderings
                .Concat(termsGlosses.Where(kvp => !termsRenderings.ContainsKey(kvp.Key)))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            IText text = new MemoryText(
                textId,
                glosses.Select(kvp => new TextRow(textId, kvp.Key) { Segment = kvp.Value.ToList() })
            );
            AddText(text);
        }

        public static IReadOnlyList<string> GetGlosses(string gloss)
        {
            //If entire term rendering is surrounded in square brackets, remove them
            Match match = ContentInBracketsRegex.Match(gloss);
            if (match.Success)
                gloss = match.Groups[0].Value;
            gloss = gloss.Replace("?", "");
            gloss = gloss.Replace("*", "");
            gloss = gloss.Replace("/", " ");
            gloss = gloss.Trim();
            gloss = StripParens(gloss);
            gloss = StripParens(gloss, left: '[', right: ']');
            gloss = gloss.Trim();
            foreach (Match m in NumericalInformationRegex.Matches(gloss))
            {
                gloss.Replace(m.Value, "");
            }
            IEnumerable<string> glosses = Regex.Split(gloss, @"\|\|");
            glosses = glosses.SelectMany(g => g.Split(new char[] { ',', ';' }));
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
                .DistinctBy(e => e.Attribute("Id").Value)
                .ToDictionary(e => e.Attribute("Id").Value, e => e.Element("Category")?.Value ?? "");
        }

        protected abstract Stream Open(string fileName);

        protected abstract bool Exists(string fileName);
    }
}
