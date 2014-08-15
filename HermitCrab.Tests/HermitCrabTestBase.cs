using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SIL.Collections;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.HermitCrab.Tests
{
	[TestFixture]
	public abstract class HermitCrabTestBase
	{
		protected SpanFactory<ShapeNode> SpanFactory;
		protected TraceManager TraceManager;
		protected SymbolTable Table1;
		protected SymbolTable Table2;
		protected SymbolTable Table3;
		protected MprFeature Latinate;
		protected MprFeature Germanic;

		protected IDBearerSet<LexEntry> Entries; 

		protected Stratum Surface;
		protected Stratum Allophonic;
		protected Stratum Morphophonemic;
		protected Language Language;

		[TestFixtureSetUp]
		public void FixtureSetUp()
		{
			SpanFactory = new ShapeSpanFactory();
			TraceManager = new TraceManager();
			var phoneticFeatSys = new FeatureSystem
			                      	{
			                      		new SymbolicFeature("voc", new FeatureSymbol("voc+", "+"), new FeatureSymbol("voc-", "-")),
			                      		new SymbolicFeature("cons", new FeatureSymbol("cons+", "+"), new FeatureSymbol("cons-", "-")),
			                      		new SymbolicFeature("high", new FeatureSymbol("high+", "+"), new FeatureSymbol("high-", "-")),
			                      		new SymbolicFeature("low", new FeatureSymbol("low+", "+"), new FeatureSymbol("low-", "-")),
			                      		new SymbolicFeature("back", new FeatureSymbol("back+", "+"), new FeatureSymbol("back-", "-")),
			                      		new SymbolicFeature("round", new FeatureSymbol("round+", "+"), new FeatureSymbol("round-", "-")),
										new SymbolicFeature("vd", new FeatureSymbol("vd+", "+"), new FeatureSymbol("vd-", "-")),
			                      		new SymbolicFeature("asp", new FeatureSymbol("asp+", "+"), new FeatureSymbol("asp-", "-")),
			                      		new SymbolicFeature("del_rel", new FeatureSymbol("del_rel+", "+"), new FeatureSymbol("del_rel-", "-")),
			                      		new SymbolicFeature("ATR", new FeatureSymbol("ATR+", "+"), new FeatureSymbol("ATR-", "-")),
			                      		new SymbolicFeature("strident", new FeatureSymbol("strident+", "+"), new FeatureSymbol("strident-", "-")),
			                      		new SymbolicFeature("cont", new FeatureSymbol("cont+", "+"), new FeatureSymbol("cont-", "-")),
			                      		new SymbolicFeature("nasal", new FeatureSymbol("nasal+", "+"), new FeatureSymbol("nasal-", "-")),
			                      		new SymbolicFeature("poa", new FeatureSymbol("bilabial"), new FeatureSymbol("labiodental"), new FeatureSymbol("alveolar"), new FeatureSymbol("velar"))
			                      	};
			phoneticFeatSys.Freeze();

			var syntacticFeatSys = new FeatureSystem
			                       	{
			                       		new SymbolicFeature("pos", new FeatureSymbol("N", "Noun"), new FeatureSymbol("V", "Verb"), new FeatureSymbol("A", "Adjective")),
			                       		new SymbolicFeature("foo", new FeatureSymbol("foo+", "+"), new FeatureSymbol("foo-", "-")),
			                       		new SymbolicFeature("baz", new FeatureSymbol("baz+", "+"), new FeatureSymbol("baz-", "-")),
			                       		new SymbolicFeature("num", new FeatureSymbol("sg"), new FeatureSymbol("pl")),
			                       		new SymbolicFeature("pers", new FeatureSymbol("1"), new FeatureSymbol("2"), new FeatureSymbol("3"), new FeatureSymbol("4")),
			                       		new SymbolicFeature("tense", new FeatureSymbol("past"), new FeatureSymbol("pres")),
			                       		new SymbolicFeature("evidential", new FeatureSymbol("witnessed")),
			                       		new SymbolicFeature("aspect", new FeatureSymbol("perf"), new FeatureSymbol("impf")),
			                       		new SymbolicFeature("mood", new FeatureSymbol("active"), new FeatureSymbol("passive")),
			                       		new SymbolicFeature("fum", new FeatureSymbol("fum+", "+"), new FeatureSymbol("fum-", "-")),
			                       		new SymbolicFeature("bar", new FeatureSymbol("bar+", "+"), new FeatureSymbol("bar-", "-")),
			                       		new ComplexFeature("head"),
			                       		new ComplexFeature("foot")
			                       	};
			syntacticFeatSys.Freeze();

			Table1 = new SymbolTable(SpanFactory, "table1");
			AddSegDef(Table1, phoneticFeatSys, "a", "cons-", "voc+", "high-", "low+", "back+", "round-", "vd+");
			AddSegDef(Table1, phoneticFeatSys, "i", "cons-", "voc+", "high+", "low-", "back-", "round-", "vd+");
			AddSegDef(Table1, phoneticFeatSys, "u", "cons-", "voc+", "high+", "low-", "back+", "round+", "vd+");
			AddSegDef(Table1, phoneticFeatSys, "o", "cons-", "voc+", "high-", "low-", "back+", "round+", "vd+");
			AddSegDef(Table1, phoneticFeatSys, "y", "cons-", "voc+", "high+", "low-", "back-", "round+", "vd+");
			AddSegDef(Table1, phoneticFeatSys, "ɯ", "cons-", "voc+", "high+", "low-", "back+", "round-", "vd+");
			AddSegDef(Table1, phoneticFeatSys, "p", "cons+", "voc-", "bilabial", "vd-", "asp-", "strident-", "cont-", "nasal-");
			AddSegDef(Table1, phoneticFeatSys, "t", "cons+", "voc-", "alveolar", "vd-", "asp-", "del_rel-", "strident-", "cont-", "nasal-");
			AddSegDef(Table1, phoneticFeatSys, "k", "cons+", "voc-", "velar", "vd-", "asp-", "strident-", "cont-", "nasal-");
			AddSegDef(Table1, phoneticFeatSys, "ts", "cons+", "voc-", "alveolar", "vd-", "asp-", "del_rel+", "strident+", "cont-", "nasal-");
			AddSegDef(Table1, phoneticFeatSys, "pʰ", "cons+", "voc-", "bilabial", "vd-", "asp+", "strident-", "cont-", "nasal-");
			AddSegDef(Table1, phoneticFeatSys, "tʰ", "cons+", "voc-", "alveolar", "vd-", "asp+", "del_rel-", "strident-", "cont-", "nasal-");
			AddSegDef(Table1, phoneticFeatSys, "kʰ", "cons+", "voc-", "velar", "vd-", "asp+", "strident-", "cont-", "nasal-");
			AddSegDef(Table1, phoneticFeatSys, "tsʰ", "cons+", "voc-", "alveolar", "vd-", "asp+", "del_rel+", "strident+", "cont-", "nasal-");
			AddSegDef(Table1, phoneticFeatSys, "b", "cons+", "voc-", "bilabial", "vd+", "cont-", "nasal-");
			AddSegDef(Table1, phoneticFeatSys, "d", "cons+", "voc-", "alveolar", "vd+", "strident-", "cont-", "nasal-");
			AddSegDef(Table1, phoneticFeatSys, "g", "cons+", "voc-", "velar", "vd+", "cont-", "nasal-");
			AddSegDef(Table1, phoneticFeatSys, "m", "cons+", "voc-", "bilabial", "vd+", "cont-", "nasal+");
			AddSegDef(Table1, phoneticFeatSys, "n", "cons+", "voc-", "alveolar", "vd+", "strident-", "cont-", "nasal+");
			AddSegDef(Table1, phoneticFeatSys, "ŋ", "cons+", "voc-", "velar", "vd+", "cont-", "nasal+");
			AddSegDef(Table1, phoneticFeatSys, "s", "cons+", "voc-", "alveolar", "vd-", "asp-", "del_rel-", "strident+", "cont+");
			AddSegDef(Table1, phoneticFeatSys, "z", "cons+", "voc-", "alveolar", "vd+", "asp-", "del_rel-", "strident+", "cont+");
			AddSegDef(Table1, phoneticFeatSys, "f", "cons+", "voc-", "labiodental", "vd-", "asp-", "strident+", "cont+");
			AddSegDef(Table1, phoneticFeatSys, "v", "cons+", "voc-", "labiodental", "vd+", "asp-", "strident+", "cont+");

			Table2 = new SymbolTable(SpanFactory, "table2");
			AddSegDef(Table2, phoneticFeatSys, "a", "cons-", "voc+", "high-", "low+", "back+", "round-", "vd+");
			AddSegDef(Table2, phoneticFeatSys, "i", "cons-", "voc+", "high+", "low-", "back-", "round-", "vd+");
			AddSegDef(Table2, phoneticFeatSys, "u", "cons-", "voc+", "high+", "low-", "back+", "round+", "vd+");
			AddSegDef(Table2, phoneticFeatSys, "y", "cons-", "voc+", "high+", "low-", "back-", "round+", "vd+");
			AddSegDef(Table2, phoneticFeatSys, "o", "cons-", "voc+", "high-", "low-", "back+", "round+", "vd+");
			AddSegDef(Table2, phoneticFeatSys, "p", "cons+", "voc-", "bilabial", "vd-");
			AddSegDef(Table2, phoneticFeatSys, "t", "cons+", "voc-", "alveolar", "vd-", "del_rel-", "strident-");
			AddSegDef(Table2, phoneticFeatSys, "k", "cons+", "voc-", "velar", "vd-");
			AddSegDef(Table2, phoneticFeatSys, "ts", "cons+", "voc-", "alveolar", "vd-", "del_rel+", "strident+");
			AddSegDef(Table2, phoneticFeatSys, "b", "cons+", "voc-", "bilabial", "vd+");
			AddSegDef(Table2, phoneticFeatSys, "d", "cons+", "voc-", "alveolar", "vd+", "strident-");
			AddSegDef(Table2, phoneticFeatSys, "g", "cons+", "voc-", "velar", "vd+");
			AddSegDef(Table2, phoneticFeatSys, "m", "cons+", "voc-", "bilabial", "vd+", "cont-", "nasal+");
			AddSegDef(Table2, phoneticFeatSys, "n", "cons+", "voc-", "alveolar", "vd+", "cont-", "nasal+");
			AddSegDef(Table2, phoneticFeatSys, "ŋ", "cons+", "voc-", "velar", "vd+", "cont-", "nasal+");
			AddSegDef(Table2, phoneticFeatSys, "s", "cons+", "voc-", "alveolar", "vd-", "asp-", "del_rel-", "strident+", "cont+");
			AddSegDef(Table2, phoneticFeatSys, "z", "cons+", "voc-", "alveolar", "vd+", "asp-", "del_rel-", "strident+", "cont+");
			AddSegDef(Table2, phoneticFeatSys, "f", "cons+", "voc-", "labiodental", "vd-", "asp-", "strident+", "cont+");
			AddSegDef(Table2, phoneticFeatSys, "v", "cons+", "voc-", "labiodental", "vd+", "asp-", "strident+", "cont+");
			AddBdryDef(Table2, "+");
			AddBdryDef(Table2, "#");
			AddBdryDef(Table2, "!");
			AddBdryDef(Table2, ".");
			AddBdryDef(Table2, "$");

			Table3 = new SymbolTable(SpanFactory, "table3");
			AddSegDef(Table3, phoneticFeatSys, "a", "cons-", "voc+", "high-", "low+", "back+", "round-", "vd+", "ATR+", "cont+");
			AddSegDef(Table3, phoneticFeatSys, "a̘", "cons-", "voc+", "high-", "low+", "back+", "round-", "vd+", "ATR-", "cont+");
			AddSegDef(Table3, phoneticFeatSys, "i", "cons-", "voc+", "high+", "low-", "back-", "round-", "vd+", "cont+");
			AddSegDef(Table3, phoneticFeatSys, "u", "cons-", "voc+", "high+", "low-", "back+", "round+", "vd+", "cont+");
			AddSegDef(Table3, phoneticFeatSys, "y", "cons-", "voc+", "high+", "low-", "back-", "round+", "vd+", "cont+");
			AddSegDef(Table3, phoneticFeatSys, "ɯ", "cons-", "voc+", "high+", "low-", "back+", "round-", "vd+", "cont+");
			AddSegDef(Table3, phoneticFeatSys, "o", "cons-", "voc+", "high-", "low-", "back+", "round+", "vd+", "cont+");
			AddSegDef(Table3, phoneticFeatSys, "p", "cons+", "voc-", "bilabial", "vd-", "cont-", "nasal-");
			AddSegDef(Table3, phoneticFeatSys, "t", "cons+", "voc-", "alveolar", "vd-", "del_rel-", "strident-", "cont-", "nasal-");
			AddSegDef(Table3, phoneticFeatSys, "k", "cons+", "voc-", "velar", "vd-", "cont-", "nasal-");
			AddSegDef(Table3, phoneticFeatSys, "ts", "cons+", "voc-", "alveolar", "vd-", "del_rel+", "strident+", "cont-", "nasal-");
			AddSegDef(Table3, phoneticFeatSys, "b", "cons+", "voc-", "bilabial", "vd+", "cont-", "nasal-");
			AddSegDef(Table3, phoneticFeatSys, "d", "cons+", "voc-", "alveolar", "vd+", "strident-", "cont-", "nasal-");
			AddSegDef(Table3, phoneticFeatSys, "g", "cons+", "voc-", "velar", "vd+", "cont-", "nasal-");
			AddSegDef(Table3, phoneticFeatSys, "m", "cons+", "voc-", "bilabial", "vd+", "cont-", "nasal+");
			AddSegDef(Table3, phoneticFeatSys, "n", "cons+", "voc-", "alveolar", "vd+", "strident-", "cont-", "nasal+");
			AddSegDef(Table3, phoneticFeatSys, "ŋ", "cons+", "voc-", "velar", "vd+", "cont-", "nasal+");
			AddSegDef(Table3, phoneticFeatSys, "s", "cons+", "voc-", "alveolar", "vd-", "asp-", "del_rel-", "strident+", "cont+");
			AddSegDef(Table3, phoneticFeatSys, "z", "cons+", "voc-", "alveolar", "vd+", "asp-", "del_rel-", "strident+", "cont+");
			AddSegDef(Table3, phoneticFeatSys, "f", "cons+", "voc-", "labiodental", "vd-", "asp-", "strident+", "cont+");
			AddSegDef(Table3, phoneticFeatSys, "v", "cons+", "voc-", "labiodental", "vd+", "asp-", "strident+", "cont+");
			AddBdryDef(Table3, "+");
			AddBdryDef(Table3, "#");
			AddBdryDef(Table3, "!");
			AddBdryDef(Table3, ".");

			Latinate = new MprFeature("latinate");
			Germanic = new MprFeature("germanic");

			Morphophonemic = new Stratum("morphophonemic", Table3) { Description = "Morphophonemic", MorphologicalRuleOrder = MorphologicalRuleOrder.Unordered };
			Allophonic = new Stratum("allophonic", Table1) { Description = "Allophonic", MorphologicalRuleOrder = MorphologicalRuleOrder.Unordered };
			Surface = new Stratum("surface", Table1) { Description = "Surface", MorphologicalRuleOrder = MorphologicalRuleOrder.Unordered };

			Entries = new IDBearerSet<LexEntry>();
			var fs = FeatureStruct.New(syntacticFeatSys)
				.Symbol("N")
				.Feature("head").EqualTo(head => head
					.Symbol("foo+").Symbol("baz-"))
				.Feature("foot").EqualTo(foot => foot
					.Symbol("fum-").Symbol("bar+")).Value;
			AddEntry("1", fs, Allophonic, "pʰit");
			fs = FeatureStruct.New(syntacticFeatSys)
				.Symbol("N")
				.Feature("head").EqualTo(head => head
					.Symbol("foo+").Symbol("baz-"))
				.Feature("foot").EqualTo(foot => foot
					.Symbol("fum-").Symbol("bar+")).Value;
			AddEntry("2", fs, Allophonic, "pit");

			AddEntry("5", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "pʰut");
			AddEntry("6", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "kʰat");
			AddEntry("7", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "kʰut");

			AddEntry("8", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "dat");
			AddEntry("9", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Allophonic, "dat");

			AddEntry("10", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "ga̘p");
			AddEntry("11", FeatureStruct.New(syntacticFeatSys).Symbol("A").Value, Morphophonemic, "gab");
			AddEntry("12", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Morphophonemic, "ga+b");

			AddEntry("13", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "bubabu");
			AddEntry("14", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "bubabi");
			AddEntry("15", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "bɯbabu");
			AddEntry("16", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "bibabi");
			AddEntry("17", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "bubi");
			AddEntry("18", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "bibu");
			AddEntry("19", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Morphophonemic, "b+ubu");
			AddEntry("20", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "bubababi");
			AddEntry("21", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "bibababu");
			AddEntry("22", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "bubabababi");
			AddEntry("23", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "bibabababu");
			AddEntry("24", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "bubui");
			AddEntry("25", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "buibu");
			AddEntry("26", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "buibui");
			AddEntry("27", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "buiibuii");
			AddEntry("28", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "buitibuiti");
			AddEntry("29", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "iibubu");

			AddEntry("30", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Morphophonemic, "bu+ib");
			AddEntry("31", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Morphophonemic, "buib");

			AddEntry("32", "sag", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "sag");
			AddEntry("33", "sas", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "sas");
			AddEntry("34", "saz", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "saz");
			AddEntry("35", "sat", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "sat");
			AddEntry("36", "liberty.port", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "sasibo");
			AddEntry("37", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "sasibut");
			AddEntry("38", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "sasibud");

			AddEntry("39", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "ab+ba");
			AddEntry("40", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "abba");

			AddEntry("41", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Allophonic, "pip");
			AddEntry("42", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "bubibi");
			AddEntry("43", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "bubibu");

			AddEntry("44", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "gigigi");

			AddEntry("45", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "nbinding");

			AddEntry("46", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "bupu");

			AddEntry("47", "tag", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "tag");
			AddEntry("48", "pag", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "pag");
			AddEntry("49", "write", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "ktb");
			AddEntry("50", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic, "suupu");

			fs = FeatureStruct.New(syntacticFeatSys)
				.Symbol("V")
				.Feature("head").EqualTo(head => head
					.Feature("num").EqualTo("pl")).Value;
			AddEntry("Perc0", "ssag", fs, Morphophonemic, "ssag");
			fs = FeatureStruct.New(syntacticFeatSys)
				.Symbol("V")
				.Feature("head").EqualTo(head => head
					.Feature("pers").EqualTo("1")
					.Feature("num").EqualTo("pl")).Value;
			AddEntry("Perc1", "ssag", fs, Morphophonemic, "ssag");
			fs = FeatureStruct.New(syntacticFeatSys)
				.Symbol("V")
				.Feature("head").EqualTo(head => head
					.Feature("pers").EqualTo("3")
					.Feature("num").EqualTo("pl")).Value;
			AddEntry("Perc2", "ssag", fs, Morphophonemic, "ssag");
			fs = FeatureStruct.New(syntacticFeatSys)
				.Symbol("V")
				.Feature("head").EqualTo(head => head
					.Feature("pers").EqualTo("2", "3")
					.Feature("num").EqualTo("pl")).Value;
			AddEntry("Perc3", "ssag", fs, Morphophonemic, "ssag");
			fs = FeatureStruct.New(syntacticFeatSys)
				.Symbol("V")
				.Feature("head").EqualTo(head => head
					.Feature("pers").EqualTo("1", "3")
					.Feature("num").EqualTo("pl")).Value;
			AddEntry("Perc4", "ssag", fs, Morphophonemic, "ssag");

			var seeFamily = new LexFamily("SEE");
			seeFamily.Entries.Add(AddEntry("bl1", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "si"));
			fs = FeatureStruct.New(syntacticFeatSys)
				.Symbol("V")
				.Feature("head").EqualTo(head => head
					.Feature("tense").EqualTo("past")).Value;
			seeFamily.Entries.Add(AddEntry("bl2", fs, Morphophonemic, "sau"));
			fs = FeatureStruct.New(syntacticFeatSys)
				.Symbol("V")
				.Feature("head").EqualTo(head => head
					.Feature("tense").EqualTo("pres")).Value;
			seeFamily.Entries.Add(AddEntry("bl3", fs, Morphophonemic, "sis"));

			LexEntry entry = AddEntry("pos1", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "ba");
			entry.MprFeatures.Add(Latinate);
			entry = AddEntry("pos2", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Morphophonemic, "ba");
			entry.MprFeatures.Add(Germanic);

			AddEntry("free", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "taz", "tas");

			entry = AddEntry("disj", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "baz", "bat", "bad", "bas");
			var unroundedVowel = FeatureStruct.New(phoneticFeatSys).Symbol(HCFeatureSystem.Segment).Symbol("voc+").Symbol("round-").Value;
			var vowel = FeatureStruct.New(phoneticFeatSys).Symbol(HCFeatureSystem.Segment).Symbol("voc+").Value;
			entry.Allomorphs[0].RequiredEnvironments.Add(new AllomorphEnvironment(SpanFactory, null, Pattern<Word, ShapeNode>.New().Annotation(unroundedVowel).Value));
			entry.Allomorphs[1].RequiredEnvironments.Add(new AllomorphEnvironment(SpanFactory, null, Pattern<Word, ShapeNode>.New().Annotation(vowel).Value));
			entry.Allomorphs[2].RequiredEnvironments.Add(new AllomorphEnvironment(SpanFactory, null, Pattern<Word, ShapeNode>.New().Annotation(vowel).Value));

			entry = AddEntry("stemname", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic, "sad", "san");
			fs = FeatureStruct.New(syntacticFeatSys)
				.Symbol("V")
				.Feature("head").EqualTo(head => head
					.Feature("tense").EqualTo("past")).Value;
			entry.Allomorphs[0].StemName = new StemName("sn1", fs);

			Language = new Language("lang1")
			           	{
			           		PhoneticFeatureSystem = phoneticFeatSys,
							SyntacticFeatureSystem = syntacticFeatSys,
							Strata = { Morphophonemic, Allophonic, Surface }
			           	};
		}

		[TearDown]
		public void TestCleanup()
		{
			foreach (Stratum stratum in Language.Strata)
			{
				stratum.PhonologicalRules.Clear();
				stratum.MorphologicalRules.Clear();
				stratum.AffixTemplates.Clear();
			}
		}

		private LexEntry AddEntry(string id, string gloss, FeatureStruct syntacticFS, Stratum stratum, params string[] forms)
		{
			var entry = new LexEntry(id) { SyntacticFeatureStruct = syntacticFS, Gloss = gloss };
			for (int i = 0; i < forms.Length; i++)
			{
				Shape shape = stratum.SymbolTable.Segment(forms[i]);
				entry.Allomorphs.Add(new RootAllomorph(id + "_allo" + (i + 1), shape));
			}
			stratum.Entries.Add(entry);
			Entries.Add(entry);
			return entry;
		}

		private LexEntry AddEntry(string id, FeatureStruct syntacticFS, Stratum stratum, params string[] forms)
		{
			return AddEntry(id, null, syntacticFS, stratum, forms);
		}

		private void AddSegDef(SymbolTable table, FeatureSystem phoneticFeatSys, string strRep, params string[] symbols)
		{
			var fs = new FeatureStruct();
			foreach (string symbolID in symbols)
			{
				FeatureSymbol symbol = phoneticFeatSys.GetSymbol(symbolID);
				fs.AddValue(symbol.Feature, new SymbolicFeatureValue(symbol));
			}
			fs.AddValue(HCFeatureSystem.Type, HCFeatureSystem.Segment);
			fs.Freeze();
			table.Add(strRep, fs);
		}

		private void AddBdryDef(SymbolTable table, string strRep)
		{
			table.Add(strRep, FeatureStruct.New().Symbol(HCFeatureSystem.Boundary).Feature(HCFeatureSystem.StrRep).EqualTo(strRep).Value);
		}

		protected void AssertMorphsEqual(IEnumerable<Word> words, params string[] expected)
		{
			var actual = new HashSet<string>();
			foreach (Word word in words)
			{
				var sb = new StringBuilder();
				bool first = true;
				foreach (Allomorph morph in word.AllomorphsInMorphOrder)
				{
					if (!first)
						sb.Append(" ");
					sb.Append(morph.Morpheme.ID);
					first = false;
				}
				actual.Add(sb.ToString());
			}

			Assert.That(actual, Is.EquivalentTo(expected));
		}

		protected void AssertSyntacticFeatureStructsEqual(IEnumerable<Word> words, FeatureStruct expected)
		{
			Assert.That(words, Has.All.Property("SyntacticFeatureStruct").EqualTo(expected).Using(FreezableEqualityComparer<FeatureStruct>.Default));
		}
	}
}
