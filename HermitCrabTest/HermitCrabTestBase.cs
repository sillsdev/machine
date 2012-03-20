using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SIL.Collections;
using SIL.HermitCrab;
using SIL.Machine;
using SIL.Machine.FeatureModel;

namespace HermitCrabTest
{
	[TestFixture]
	public abstract class HermitCrabTestBase
	{
		protected SpanFactory<ShapeNode> SpanFactory;
		protected SymbolTable Table1;
		protected SymbolTable Table2;
		protected SymbolTable Table3;
		protected FeatureStruct LeftSideFS;
		protected FeatureStruct RightSideFS;
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
			var phoneticFeatSys = FeatureSystem.New()
				.SymbolicFeature("voc", voc => voc
					.Symbol("voc+", "+")
					.Symbol("voc-", "-")
					.Symbol("voc?", "?").Default)
				.SymbolicFeature("cons", cons => cons
					.Symbol("cons+", "+")
					.Symbol("cons-", "-")
					.Symbol("cons?", "?").Default)
				.SymbolicFeature("high", high => high
					.Symbol("high+", "+")
					.Symbol("high-", "-")
					.Symbol("high?", "?").Default)
				.SymbolicFeature("low", low => low
					.Symbol("low+", "+")
					.Symbol("low-", "-")
					.Symbol("low?", "?"))
				.SymbolicFeature("back", back => back
					.Symbol("back+", "+")
					.Symbol("back-", "-")
					.Symbol("back?", "?").Default)
				.SymbolicFeature("round", round => round
					.Symbol("round+", "+")
					.Symbol("round-", "-")
					.Symbol("round?", "?").Default)
				.SymbolicFeature("vd", vd => vd
					.Symbol("vd+", "+")
					.Symbol("vd-", "-")
					.Symbol("vd?", "?").Default)
				.SymbolicFeature("asp", asp => asp
					.Symbol("asp+", "+")
					.Symbol("asp-", "-")
					.Symbol("asp?", "?").Default)
				.SymbolicFeature("del_rel", delrel => delrel
					.Symbol("del_rel+", "+")
					.Symbol("del_rel-", "-")
					.Symbol("del_rel?", "?").Default)
				.SymbolicFeature("ATR", atr => atr
					.Symbol("ATR+", "+")
					.Symbol("ATR-", "-")
					.Symbol("ATR?", "?").Default)
				.SymbolicFeature("strident", strident => strident
					.Symbol("strident+", "+")
					.Symbol("strident-", "-")
					.Symbol("strident?", "?").Default)
				.SymbolicFeature("cont", cont => cont
					.Symbol("cont+", "+")
					.Symbol("cont-", "-")
					.Symbol("cont?", "?").Default)
				.SymbolicFeature("nasal", nasal => nasal
					.Symbol("nasal+", "+")
					.Symbol("nasal-", "-")
					.Symbol("nasal?", "?").Default)
				.SymbolicFeature("poa", poa => poa
					.Symbol("bilabial")
					.Symbol("labiodental")
					.Symbol("alveolar")
					.Symbol("velar")
					.Symbol("poa?", "?").Default).Value;

			var syntacticFeatSys = FeatureSystem.New()
				.SymbolicFeature("pos", pos => pos
					.Symbol("N", "Noun")
					.Symbol("V", "Verb")
					.Symbol("A", "Adjective"))
				.ComplexFeature("head", head => head
					.SymbolicFeature("foo", foo => foo
						.Symbol("foo+", "+")
						.Symbol("foo-", "-"))
					.SymbolicFeature("baz", baz => baz
						.Symbol("baz+", "+")
						.Symbol("baz-", "-"))
					.SymbolicFeature("num", num => num
						.Symbol("sg")
						.Symbol("pl"))
					.SymbolicFeature("pers", pers => pers
						.Symbol("1")
						.Symbol("2")
						.Symbol("3")
						.Symbol("4"))
					.SymbolicFeature("tense", tense => tense
						.Symbol("past")
						.Symbol("pres"))
					.SymbolicFeature("evidential", evidential => evidential
						.Symbol("witnessed"))
					.SymbolicFeature("aspect", aspect => aspect
						.Symbol("perf")
						.Symbol("impf"))
					.SymbolicFeature("mood", mood => mood
						.Symbol("active")
						.Symbol("passive")))
				.ComplexFeature("foot", foot => foot
					.SymbolicFeature("fum", fum => fum
						.Symbol("fum+", "+")
						.Symbol("fum-", "-"))
					.SymbolicFeature("bar", bar => bar
						.Symbol("bar+", "+")
						.Symbol("bar-", "-"))).Value;

			Table1 = new SymbolTable("table1", SpanFactory);
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

			Table2 = new SymbolTable("table2", SpanFactory);
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

			Table3 = new SymbolTable("table3", SpanFactory);
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

			LeftSideFS = FeatureStruct.New().Symbol(HCFeatureSystem.Anchor).Symbol(HCFeatureSystem.LeftSide).Value;
			RightSideFS = FeatureStruct.New().Symbol(HCFeatureSystem.Anchor).Symbol(HCFeatureSystem.RightSide).Value;

			Latinate = new MprFeature("latinate");
			Germanic = new MprFeature("germanic");

			Morphophonemic = new Stratum("morphophonemic", Table3) {Description = "Morphophonemic"};
			Allophonic = new Stratum("allophonic", Table1) {Description = "Allophonic"};
			Surface = new Stratum("surface", Table1) {Description = "Surface"};

			Entries = new IDBearerSet<LexEntry>();
			var fs = FeatureStruct.New(syntacticFeatSys)
				.Symbol("N")
				.Feature("head").EqualToFeatureStruct(head => head
					.Symbol("foo+").Symbol("baz-"))
				.Feature("foot").EqualToFeatureStruct(foot => foot
					.Symbol("fum-").Symbol("bar+")).Value;
			AddEntry("1", "pʰit", fs, Allophonic);
			fs = FeatureStruct.New(syntacticFeatSys)
				.Symbol("N")
				.Feature("head").EqualToFeatureStruct(head => head
					.Symbol("foo+").Symbol("baz-"))
				.Feature("foot").EqualToFeatureStruct(foot => foot
					.Symbol("fum-").Symbol("bar+")).Value;
			AddEntry("2", "pit", fs, Allophonic);

			AddEntry("5", "pʰut", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);
			AddEntry("6", "kʰat", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);
			AddEntry("7", "kʰut", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);

			AddEntry("8", "dat", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);
			AddEntry("9", "dat", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Allophonic);

			AddEntry("10", "ga̘p", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic);
			AddEntry("11", "gab", FeatureStruct.New(syntacticFeatSys).Symbol("A").Value, Morphophonemic);
			AddEntry("12", "ga+b", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Morphophonemic);

			AddEntry("13", "bubabu", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);
			AddEntry("14", "bubabi", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);
			AddEntry("15", "bɯbabu", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);
			AddEntry("16", "bibabi", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);
			AddEntry("17", "bubi", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);
			AddEntry("18", "bibu", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);
			AddEntry("19", "b+ubu", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Morphophonemic);
			AddEntry("20", "bubababi", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);
			AddEntry("21", "bibababu", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);
			AddEntry("22", "bubabababi", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);
			AddEntry("23", "bibabababu", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);
			AddEntry("24", "bubui", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);
			AddEntry("25", "buibu", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);
			AddEntry("26", "buibui", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);
			AddEntry("27", "buiibuii", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);
			AddEntry("28", "buitibuiti", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);
			AddEntry("29", "iibubu", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);

			AddEntry("30", "bu+ib", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Morphophonemic);
			AddEntry("31", "buib", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Morphophonemic);

			AddEntry("32", "sag", "sag", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic);
			AddEntry("33", "sas", "sas", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic);
			AddEntry("34", "saz", "saz", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic);
			AddEntry("35", "sat", "sat", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic);
			AddEntry("36", "sasibo", "liberty.port", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic);
			AddEntry("37", "sasibut", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic);
			AddEntry("38", "sasibud", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic);

			AddEntry("39", "ab+ba", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic);
			AddEntry("40", "abba", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic);

			AddEntry("41", "pip", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Allophonic);
			AddEntry("42", "bubibi", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic);
			AddEntry("43", "bubibu", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic);

			AddEntry("44", "gigigi", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic);

			AddEntry("45", "nbinding", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic);

			AddEntry("46", "bupu", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);

			AddEntry("47", "tag", "tag", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic);
			AddEntry("48", "pag", "pag", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic);
			AddEntry("49", "ktb", "write", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic);
			AddEntry("50", "suupu", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);

			fs = FeatureStruct.New(syntacticFeatSys)
				.Symbol("V")
				.Feature("head").EqualToFeatureStruct(head => head
					.Feature("num").EqualTo("pl")).Value;
			AddEntry("Perc0", "ssag", "ssag", fs, Morphophonemic);
			fs = FeatureStruct.New(syntacticFeatSys)
				.Symbol("V")
				.Feature("head").EqualToFeatureStruct(head => head
					.Feature("pers").EqualTo("1")
					.Feature("num").EqualTo("pl")).Value;
			AddEntry("Perc1", "ssag", "ssag", fs, Morphophonemic);
			fs = FeatureStruct.New(syntacticFeatSys)
				.Symbol("V")
				.Feature("head").EqualToFeatureStruct(head => head
					.Feature("pers").EqualTo("3")
					.Feature("num").EqualTo("pl")).Value;
			AddEntry("Perc2", "ssag", "ssag", fs, Morphophonemic);
			fs = FeatureStruct.New(syntacticFeatSys)
				.Symbol("V")
				.Feature("head").EqualToFeatureStruct(head => head
					.Feature("pers").EqualTo("2", "3")
					.Feature("num").EqualTo("pl")).Value;
			AddEntry("Perc3", "ssag", "ssag", fs, Morphophonemic);
			fs = FeatureStruct.New(syntacticFeatSys)
				.Symbol("V")
				.Feature("head").EqualToFeatureStruct(head => head
					.Feature("pers").EqualTo("1", "3")
					.Feature("num").EqualTo("pl")).Value;
			AddEntry("Perc4", "ssag", "ssag", fs, Morphophonemic);

			var seeFamily = new LexFamily("SEE");
			seeFamily.Entries.Add(AddEntry("bl1", "si", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic));
			fs = FeatureStruct.New(syntacticFeatSys)
				.Symbol("V")
				.Feature("head").EqualToFeatureStruct(head => head
					.Feature("tense").EqualTo("past")).Value;
			seeFamily.Entries.Add(AddEntry("bl2", "sau", fs, Morphophonemic));
			fs = FeatureStruct.New(syntacticFeatSys)
				.Symbol("V")
				.Feature("head").EqualToFeatureStruct(head => head
					.Feature("tense").EqualTo("pres")).Value;
			seeFamily.Entries.Add(AddEntry("bl3", "sis", fs, Morphophonemic));

			LexEntry entry = AddEntry("pos1", "ba", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic);
			entry.MprFeatures.Add(Latinate);
			entry = AddEntry("pos2", "ba", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Morphophonemic);
			entry.MprFeatures.Add(Germanic);

			Language = new Language("lang1", phoneticFeatSys, syntacticFeatSys);
			Language.Strata.Add(Morphophonemic);
			Language.Strata.Add(Allophonic);
			Language.Strata.Add(Surface);
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

		private LexEntry AddEntry(string id, string word, string gloss, FeatureStruct syntacticFS, Stratum stratum)
		{
			var entry = new LexEntry(id) { SyntacticFeatureStruct = syntacticFS, Gloss = gloss };
			Shape shape;
			stratum.SymbolTable.ToShape(word, out shape);
			entry.Allomorphs.Add(new RootAllomorph(id + "_allo1", shape));
			stratum.Entries.Add(entry);
			Entries.Add(entry);
			return entry;
		}

		private LexEntry AddEntry(string id, string word, FeatureStruct syntacticFS, Stratum stratum)
		{
			return AddEntry(id, word, null, syntacticFS, stratum);
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
			table.AddSymbol(strRep, fs);
		}

		private void AddBdryDef(SymbolTable table, string strRep)
		{
			table.AddSymbol(strRep, FeatureStruct.New().Symbol(HCFeatureSystem.Boundary).Feature(HCFeatureSystem.StrRep).EqualTo(strRep).Value);
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

			Assert.AreEqual(expected.Length, actual.Count);
			foreach (string id in expected)
				Assert.IsTrue(actual.Contains(id));
		}

		protected void AssertSyntacticFeatureStructsEqual(IEnumerable<Word> words, FeatureStruct expected)
		{
			foreach (Word word in words)
				Assert.AreEqual(expected, word.SyntacticFeatureStruct);
		}
	}
}
