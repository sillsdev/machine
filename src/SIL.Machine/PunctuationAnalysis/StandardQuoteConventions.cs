using System.Collections.Generic;

namespace SIL.Machine.PunctuationAnalysis
{
    public class QuoteConventions
    {
        public static readonly QuoteConventionSet Standard = new QuoteConventionSet(
            new List<QuoteConvention>
            {
                new QuoteConvention(
                    "standard_english",
                    new List<SingleLevelQuoteConvention>
                    {
                        new SingleLevelQuoteConvention("\u201c", "\u201d"),
                        new SingleLevelQuoteConvention("\u2018", "\u2019"),
                        new SingleLevelQuoteConvention("\u201c", "\u201d"),
                        new SingleLevelQuoteConvention("\u2018", "\u2019"),
                    }
                ),
                new QuoteConvention(
                    "typewriter_english",
                    new List<SingleLevelQuoteConvention>
                    {
                        new SingleLevelQuoteConvention("\"", "\""),
                        new SingleLevelQuoteConvention("'", "'"),
                        new SingleLevelQuoteConvention("\"", "\""),
                        new SingleLevelQuoteConvention("'", "'"),
                    }
                ),
                new QuoteConvention(
                    "british_english",
                    new List<SingleLevelQuoteConvention>
                    {
                        new SingleLevelQuoteConvention("\u2018", "\u2019"),
                        new SingleLevelQuoteConvention("\u201c", "\u201d"),
                        new SingleLevelQuoteConvention("\u2018", "\u2019"),
                        new SingleLevelQuoteConvention("\u201c", "\u201d"),
                    }
                ),
                new QuoteConvention(
                    "british_typewriter_english",
                    new List<SingleLevelQuoteConvention>
                    {
                        new SingleLevelQuoteConvention("'", "'"),
                        new SingleLevelQuoteConvention("\"", "\""),
                        new SingleLevelQuoteConvention("'", "'"),
                        new SingleLevelQuoteConvention("\"", "\""),
                    }
                ),
                new QuoteConvention(
                    "hybrid_typewriter_english",
                    new List<SingleLevelQuoteConvention>
                    {
                        new SingleLevelQuoteConvention("\u201c", "\u201d"),
                        new SingleLevelQuoteConvention("'", "'"),
                        new SingleLevelQuoteConvention("\"", "\""),
                    }
                ),
                new QuoteConvention(
                    "standard_french",
                    new List<SingleLevelQuoteConvention>
                    {
                        new SingleLevelQuoteConvention("\u00ab", "\u00bb"),
                        new SingleLevelQuoteConvention("\u2039", "\u203a"),
                        new SingleLevelQuoteConvention("\u00ab", "\u00bb"),
                        new SingleLevelQuoteConvention("\u2039", "\u203a"),
                    }
                ),
                new QuoteConvention(
                    "typewriter_french",
                    new List<SingleLevelQuoteConvention>
                    {
                        new SingleLevelQuoteConvention("<<", ">>"),
                        new SingleLevelQuoteConvention("<", ">"),
                        new SingleLevelQuoteConvention("<<", ">>"),
                        new SingleLevelQuoteConvention("<", ">"),
                    }
                ),
                new QuoteConvention(
                    "french_variant",
                    new List<SingleLevelQuoteConvention>
                    {
                        new SingleLevelQuoteConvention("\u00ab", "\u00bb"),
                        new SingleLevelQuoteConvention("\u2039", "\u203a"),
                        new SingleLevelQuoteConvention("\u201c", "\u201d"),
                        new SingleLevelQuoteConvention("\u2018", "\u2019"),
                    }
                ),
                new QuoteConvention(
                    "western_european",
                    new List<SingleLevelQuoteConvention>
                    {
                        new SingleLevelQuoteConvention("\u00ab", "\u00bb"),
                        new SingleLevelQuoteConvention("\u201c", "\u201d"),
                        new SingleLevelQuoteConvention("\u2018", "\u2019"),
                    }
                ),
                new QuoteConvention(
                    "british_inspired_western_european",
                    new List<SingleLevelQuoteConvention>
                    {
                        new SingleLevelQuoteConvention("\u00ab", "\u00bb"),
                        new SingleLevelQuoteConvention("\u2018", "\u2019"),
                        new SingleLevelQuoteConvention("\u201c", "\u201d"),
                    }
                ),
                new QuoteConvention(
                    "typewriter_western_european",
                    new List<SingleLevelQuoteConvention>
                    {
                        new SingleLevelQuoteConvention("<<", ">>"),
                        new SingleLevelQuoteConvention("\"", "\""),
                        new SingleLevelQuoteConvention("'", "'"),
                    }
                ),
                new QuoteConvention(
                    "typewriter_western_european_variant",
                    new List<SingleLevelQuoteConvention>
                    {
                        new SingleLevelQuoteConvention("\"", "\""),
                        new SingleLevelQuoteConvention("<", ">"),
                        new SingleLevelQuoteConvention("'", "'"),
                    }
                ),
                new QuoteConvention(
                    "hybrid_typewriter_western_european",
                    new List<SingleLevelQuoteConvention>
                    {
                        new SingleLevelQuoteConvention("\u00ab", "\u00bb"),
                        new SingleLevelQuoteConvention("\"", "\""),
                        new SingleLevelQuoteConvention("'", "'"),
                    }
                ),
                new QuoteConvention(
                    "hybrid_british_typewriter_western_european",
                    new List<SingleLevelQuoteConvention>
                    {
                        new SingleLevelQuoteConvention("\u00ab", "\u00bb"),
                        new SingleLevelQuoteConvention("'", "'"),
                        new SingleLevelQuoteConvention("\"", "\""),
                    }
                ),
                new QuoteConvention(
                    "central_european",
                    new List<SingleLevelQuoteConvention>
                    {
                        new SingleLevelQuoteConvention("\u201e", "\u201c"),
                        new SingleLevelQuoteConvention("\u201a", "\u2018"),
                        new SingleLevelQuoteConvention("\u201e", "\u201c"),
                        new SingleLevelQuoteConvention("\u201a", "\u2018"),
                    }
                ),
                new QuoteConvention(
                    "central_european_guillemets",
                    new List<SingleLevelQuoteConvention>
                    {
                        new SingleLevelQuoteConvention("\u00bb", "\u00ab"),
                        new SingleLevelQuoteConvention("\u203a", "\u2039"),
                        new SingleLevelQuoteConvention("\u00bb", "\u00ab"),
                        new SingleLevelQuoteConvention("\u203a", "\u2039"),
                    }
                ),
                new QuoteConvention(
                    "standard_swedish",
                    new List<SingleLevelQuoteConvention>
                    {
                        new SingleLevelQuoteConvention("\u201d", "\u201d"),
                        new SingleLevelQuoteConvention("\u2019", "\u2019"),
                        new SingleLevelQuoteConvention("\u201d", "\u201d"),
                        new SingleLevelQuoteConvention("\u2019", "\u2019"),
                    }
                ),
                new QuoteConvention(
                    "standard_finnish",
                    new List<SingleLevelQuoteConvention>
                    {
                        new SingleLevelQuoteConvention("\u00bb", "\u00bb"),
                        new SingleLevelQuoteConvention("\u2019", "\u2019"),
                    }
                ),
                new QuoteConvention(
                    "eastern_european",
                    new List<SingleLevelQuoteConvention>
                    {
                        new SingleLevelQuoteConvention("\u201e", "\u201d"),
                        new SingleLevelQuoteConvention("\u201a", "\u2019"),
                        new SingleLevelQuoteConvention("\u201e", "\u201d"),
                        new SingleLevelQuoteConvention("\u201a", "\u2019"),
                    }
                ),
                new QuoteConvention(
                    "standard_russian",
                    new List<SingleLevelQuoteConvention>
                    {
                        new SingleLevelQuoteConvention("\u00ab", "\u00bb"),
                        new SingleLevelQuoteConvention("\u201e", "\u201c"),
                        new SingleLevelQuoteConvention("\u201a", "\u2018"),
                    }
                ),
                new QuoteConvention(
                    "standard_arabic",
                    new List<SingleLevelQuoteConvention>
                    {
                        new SingleLevelQuoteConvention("\u201d", "\u201c"),
                        new SingleLevelQuoteConvention("\u2019", "\u2018"),
                        new SingleLevelQuoteConvention("\u201d", "\u201c"),
                        new SingleLevelQuoteConvention("\u2019", "\u2018"),
                    }
                ),
                new QuoteConvention(
                    "non-standard_arabic",
                    new List<SingleLevelQuoteConvention>
                    {
                        new SingleLevelQuoteConvention("\u00ab", "\u00bb"),
                        new SingleLevelQuoteConvention("\u2019", "\u2018"),
                    }
                ),
            }
        );
    }
}
