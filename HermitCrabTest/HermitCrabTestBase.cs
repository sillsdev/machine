using NUnit.Framework;
using SIL.HermitCrab;
using SIL.Machine;
using SIL.Machine.FeatureModel;

namespace HermitCrabTest
{
	[TestFixture]
	public abstract class HermitCrabTestBase
	{
		protected SpanFactory<ShapeNode> SpanFactory;
		protected SymbolDefinitionTable Table1;
		protected SymbolDefinitionTable Table2;
		protected SymbolDefinitionTable Table3;
		protected FeatureStruct LeftSideFS;
		protected FeatureStruct RightSideFS;

		protected Stratum Surface;
		protected Stratum Allophonic;
		protected Stratum Morphophonemic;
		protected Morpher Morpher;

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
						.Symbol("3")))
				.ComplexFeature("foot", foot => foot
					.SymbolicFeature("fum", fum => fum
						.Symbol("fum+", "+")
						.Symbol("fum-", "-"))
					.SymbolicFeature("bar", bar => bar
						.Symbol("bar+", "+")
						.Symbol("bar-", "-"))).Value;

			Table1 = new SymbolDefinitionTable("table1", SpanFactory);
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

			Table2 = new SymbolDefinitionTable("table2", SpanFactory);
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

			Table3 = new SymbolDefinitionTable("table3", SpanFactory);
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

			LeftSideFS = FeatureStruct.New().Symbol(HCFeatureSystem.LeftSide).Value;
			RightSideFS = FeatureStruct.New().Symbol(HCFeatureSystem.RightSide).Value;

			Morphophonemic = new Stratum("morphophonemic", SpanFactory, Table3) {Description = "Morphophonemic"};
			Allophonic = new Stratum("allophonic", SpanFactory, Table1) {Description = "Allophonic"};
			Surface = new Stratum(Stratum.SurfaceStratumID, SpanFactory, Table1) {Description = "Surface"};

			var lexicon = new Lexicon();
			var fs = FeatureStruct.New(syntacticFeatSys)
				.Symbol("N")
				.Feature("head").EqualToFeatureStruct(head => head
					.Symbol("foo+").Symbol("baz-"))
				.Feature("foot").EqualToFeatureStruct(foot => foot
					.Symbol("fum-").Symbol("bar+")).Value;
			AddEntry(lexicon, "1", "pʰit", fs, Allophonic);
			fs = FeatureStruct.New(syntacticFeatSys)
				.Symbol("N")
				.Feature("head").EqualToFeatureStruct(head => head
					.Symbol("foo+").Symbol("baz-"))
				.Feature("foot").EqualToFeatureStruct(foot => foot
					.Symbol("fum-").Symbol("bar+")).Value;
			AddEntry(lexicon, "2", "pit", fs, Allophonic);

			AddEntry(lexicon, "5", "pʰut", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);
			AddEntry(lexicon, "6", "kʰat", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);
			AddEntry(lexicon, "7", "kʰut", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);

			AddEntry(lexicon, "8", "dat", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);
			AddEntry(lexicon, "9", "dat", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Allophonic);

			AddEntry(lexicon, "10", "ga̘p", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic);
			AddEntry(lexicon, "11", "gab", FeatureStruct.New(syntacticFeatSys).Symbol("A").Value, Morphophonemic);
			AddEntry(lexicon, "12", "ga+b", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Morphophonemic);

			AddEntry(lexicon, "13", "bubabu", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);
			AddEntry(lexicon, "14", "bubabi", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);
			AddEntry(lexicon, "15", "bɯbabu", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);
			AddEntry(lexicon, "16", "bibabi", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);
			AddEntry(lexicon, "17", "bubi", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);
			AddEntry(lexicon, "18", "bibu", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);
			AddEntry(lexicon, "19", "b+ubu", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Morphophonemic);
			AddEntry(lexicon, "20", "bubababi", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);
			AddEntry(lexicon, "21", "bibababu", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);
			AddEntry(lexicon, "22", "bubabababi", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);
			AddEntry(lexicon, "23", "bibabababu", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);
			AddEntry(lexicon, "24", "bubui", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);
			AddEntry(lexicon, "25", "buibu", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);
			AddEntry(lexicon, "26", "buibui", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);
			AddEntry(lexicon, "27", "buiibuii", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);
			AddEntry(lexicon, "28", "buitibuiti", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);
			AddEntry(lexicon, "29", "iibubu", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);

			AddEntry(lexicon, "30", "bu+ib", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Morphophonemic);
			AddEntry(lexicon, "31", "buib", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Morphophonemic);

			AddEntry(lexicon, "32", "sag", "sag", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic);
			AddEntry(lexicon, "33", "sas", "sas", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic);
			AddEntry(lexicon, "34", "saz", "saz", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic);
			AddEntry(lexicon, "35", "sat", "sat", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic);
			AddEntry(lexicon, "36", "sasibo", "liberty.port", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic);
			AddEntry(lexicon, "37", "sasibut", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic);
			AddEntry(lexicon, "38", "sasibud", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic);

			AddEntry(lexicon, "39", "ab+ba", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic);
			AddEntry(lexicon, "40", "abba", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic);

			AddEntry(lexicon, "41", "pip", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Allophonic);
			AddEntry(lexicon, "42", "bubibi", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic);
			AddEntry(lexicon, "43", "bubibu", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic);

			AddEntry(lexicon, "44", "gigigi", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic);

			AddEntry(lexicon, "45", "nbinding", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic);

			AddEntry(lexicon, "46", "bupu", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);

			AddEntry(lexicon, "47", "tag", "tag", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic);
			AddEntry(lexicon, "48", "pag", "pag", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic);
			AddEntry(lexicon, "49", "ktb", "write", FeatureStruct.New(syntacticFeatSys).Symbol("V").Value, Morphophonemic);
			AddEntry(lexicon, "50", "suupu", FeatureStruct.New(syntacticFeatSys).Symbol("N").Value, Allophonic);

			fs = FeatureStruct.New(syntacticFeatSys)
				.Symbol("V")
				.Feature("head").EqualToFeatureStruct(head => head
					.Feature("num").EqualTo("pl")).Value;
			AddEntry(lexicon, "Perc0", "ssag", "ssag", fs, Morphophonemic);
			fs = FeatureStruct.New(syntacticFeatSys)
				.Symbol("V")
				.Feature("head").EqualToFeatureStruct(head => head
					.Feature("pers").EqualTo("1")
					.Feature("num").EqualTo("pl")).Value;
			AddEntry(lexicon, "Perc1", "ssag", "ssag", fs, Morphophonemic);
			fs = FeatureStruct.New(syntacticFeatSys)
				.Symbol("V")
				.Feature("head").EqualToFeatureStruct(head => head
					.Feature("pers").EqualTo("3")
					.Feature("num").EqualTo("pl")).Value;
			AddEntry(lexicon, "Perc2", "ssag", "ssag", fs, Morphophonemic);
			fs = FeatureStruct.New(syntacticFeatSys)
				.Symbol("V")
				.Feature("head").EqualToFeatureStruct(head => head
					.Feature("pers").EqualTo("2", "3")
					.Feature("num").EqualTo("pl")).Value;
			AddEntry(lexicon, "Perc3", "ssag", "ssag", fs, Morphophonemic);
			fs = FeatureStruct.New(syntacticFeatSys)
				.Symbol("V")
				.Feature("head").EqualToFeatureStruct(head => head
					.Feature("pers").EqualTo("1", "3")
					.Feature("num").EqualTo("pl")).Value;
			AddEntry(lexicon, "Perc4", "ssag", "ssag", fs, Morphophonemic);

			Morpher = new Morpher("morpher1", phoneticFeatSys, syntacticFeatSys, lexicon);
			Morpher.AddStratum(Morphophonemic);
			Morpher.AddStratum(Allophonic);
			Morpher.AddStratum(Surface);
		}

		private void AddEntry(Lexicon lexicon, string id, string word, string gloss, FeatureStruct syntacticFS, Stratum stratum)
		{
			var entry = new LexEntry(id, syntacticFS);
			if (gloss != null)
				entry.Gloss = new Gloss(gloss);
			Shape shape;
			stratum.SymbolDefinitionTable.ToShape(word, out shape);
			entry.AddAllomorph(new RootAllomorph(id, shape));
			lexicon.AddEntry(entry);
			stratum.AddEntry(entry);
		}

		private void AddEntry(Lexicon lexicon, string id, string word, FeatureStruct syntacticFS, Stratum stratum)
		{
			AddEntry(lexicon, id, word, null, syntacticFS, stratum);
		}

		private void AddSegDef(SymbolDefinitionTable table, FeatureSystem phoneticFeatSys, string strRep, params string[] symbols)
		{
			var fs = new FeatureStruct();
			foreach (string symbolID in symbols)
			{
				FeatureSymbol symbol = phoneticFeatSys.GetSymbol(symbolID);
				fs.AddValue(symbol.Feature, new SymbolicFeatureValue(symbol));
			}
			table.AddSymbolDefinition(strRep, HCFeatureSystem.SegmentType, fs);
		}

		private void AddBdryDef(SymbolDefinitionTable table, string strRep)
		{
			table.AddSymbolDefinition(strRep, HCFeatureSystem.BoundaryType, FeatureStruct.New().Feature(HCFeatureSystem.StrRep).EqualTo(strRep).Value);
		}
	}
}
