using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using SIL.Extensions;

namespace SIL.Machine.Corpora
{
    public abstract class ParatextProjectTermsCorpusBase : DictionaryTextCorpus
    {
        private static readonly List<string> PredefinedTermsListTypes = new List<string>()
        {
            "Major",
            "All",
            "SilNt",
            "Pt6"
        };
        private readonly ParatextProjectSettings _settings;

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

        public ParatextProjectTermsCorpusBase(ParatextProjectSettings settings)
        {
            _settings = settings;
        }

        protected void AddTexts(IEnumerable<string> termCategories, bool preferTermsLocalization = false)
        {
            XDocument biblicalTermsDoc;
            IDictionary<string, string> termIdToCategoryDictionary;
            if (_settings.BiblicalTermsListType == "Project")
            {
                if (Exists(_settings.BiblicalTermsFileName))
                {
                    using (Stream keyTermsFile = Open(_settings.BiblicalTermsFileName))
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
            else if (PredefinedTermsListTypes.Contains(_settings.BiblicalTermsListType))
            {
                using (
                    Stream keyTermsFile = Assembly
                        .GetExecutingAssembly()
                        .GetManifestResourceStream("SIL.Machine.Corpora." + _settings.BiblicalTermsFileName)
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
            XDocument doc;
            bool useTermsRenderingXml =
                (!preferTermsLocalization || _settings.BiblicalTermsListType != "Major")
                && Exists("TermRenderings.xml");

            if (!SupportedLanguageTermsLocalizationXmls.TryGetValue(_settings.LanguageCode, out string resourceName))
            {
                if (Exists("TermRenderings.xml"))
                {
                    useTermsRenderingXml = true;
                }
                else
                {
                    return;
                }
            }

            if (useTermsRenderingXml)
            {
                using (Stream keyTermsFile = Open("TermRenderings.xml"))
                {
                    doc = XDocument.Load(keyTermsFile);
                }
            }
            else
            {
                using (Stream keyTermsFile = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    doc = XDocument.Load(keyTermsFile);
                }
            }

            AddTexts(doc, _settings, termCategories, termIdToCategoryDictionary);
        }

        private void AddTexts(
            XDocument doc,
            ParatextProjectSettings settings,
            IEnumerable<string> termCategories,
            IDictionary<string, string> termIdToCategoryDictionary
        )
        {
            IEnumerable<XElement> termsElements = doc.Descendants().Where(n => n.Name.LocalName == "TermRendering");
            bool isTermRenderingsFile = true;
            if (termsElements.Count() == 0)
            {
                termsElements = doc.Descendants().Where(n => n.Name.LocalName == "Localization");
                isTermRenderingsFile = false;
            }

            string textId =
                $"{settings.BiblicalTermsListType}:{settings.BiblicalTermsProjectName}:{settings.BiblicalTermsFileName}";
            List<TextRow> rows = new List<TextRow>();
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
                string gloss = isTermRenderingsFile
                    ? element.Element("Renderings").Value
                    : element.Attribute("Gloss").Value;
                IReadOnlyList<string> glosses = GetGlosses(gloss);
                rows.Add(new TextRow(textId, id) { Segment = glosses });
            }
            IText text = new MemoryText(textId, rows);
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
