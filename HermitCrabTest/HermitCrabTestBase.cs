using System.Linq;
using NUnit.Framework;
using SIL.APRE;
using SIL.APRE.FeatureModel;
using SIL.HermitCrab;

namespace HermitCrabTest
{
	[TestFixture]
	public class HermitCrabTestBase
	{
		protected SpanFactory<ShapeNode> SpanFactory;
		protected FeatureSystem FeatSys;
		protected CharacterDefinitionTable Table1;
		protected CharacterDefinitionTable Table2;
		protected CharacterDefinitionTable Table3;
		protected FeatureStruct LeftSideFS;
		protected FeatureStruct RightSideFS;

		protected Stratum Surface;
		protected Stratum Allophonic;
		protected Stratum Morphophonemic;

		[TestFixtureSetUp]
		public void FixtureSetUp()
		{
			SpanFactory = new SpanFactory<ShapeNode>((x, y) => x.CompareTo(y), (start, end) => start.GetNodes(end).Count(), true);
			FeatSys = FeatureSystem.New
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

			Table1 = new CharacterDefinitionTable("table1", "table1", SpanFactory);
			AddSegDef(Table1, "a", "cons-", "voc+", "high-", "low+", "back+", "round-", "vd+");
			AddSegDef(Table1, "i", "cons-", "voc+", "high+", "low-", "back-", "round-", "vd+");
			AddSegDef(Table1, "u", "cons-", "voc+", "high+", "low-", "back+", "round+", "vd+");
			AddSegDef(Table1, "o", "cons-", "voc+", "high-", "low-", "back+", "round+", "vd+");
			AddSegDef(Table1, "y", "cons-", "voc+", "high+", "low-", "back-", "round+", "vd+");
			AddSegDef(Table1, "ɯ", "cons-", "voc+", "high+", "low-", "back+", "round-", "vd+");
			AddSegDef(Table1, "p", "cons+", "voc-", "bilabial", "vd-", "asp-", "strident-", "cont-", "nasal-");
			AddSegDef(Table1, "t", "cons+", "voc-", "alveolar", "vd-", "asp-", "del_rel-", "strident-", "cont-", "nasal-");
			AddSegDef(Table1, "k", "cons+", "voc-", "velar", "vd-", "asp-", "strident-", "cont-", "nasal-");
			AddSegDef(Table1, "ts", "cons+", "voc-", "alveolar", "vd-", "asp-", "del_rel+", "strident+", "cont-", "nasal-");
			AddSegDef(Table1, "pʰ", "cons+", "voc-", "bilabial", "vd-", "asp+", "strident-", "cont-", "nasal-");
			AddSegDef(Table1, "tʰ", "cons+", "voc-", "alveolar", "vd-", "asp+", "del_rel-", "strident-", "cont-", "nasal-");
			AddSegDef(Table1, "kʰ", "cons+", "voc-", "velar", "vd-", "asp+", "strident-", "cont-", "nasal-");
			AddSegDef(Table1, "tsʰ", "cons+", "voc-", "alveolar", "vd-", "asp+", "del_rel+", "strident+", "cont-", "nasal-");
			AddSegDef(Table1, "b", "cons+", "voc-", "bilabial", "vd+", "cont-", "nasal-");
			AddSegDef(Table1, "d", "cons+", "voc-", "alveolar", "vd+", "strident-", "cont-", "nasal-");
			AddSegDef(Table1, "g", "cons+", "voc-", "velar", "vd+", "cont-", "nasal-");
			AddSegDef(Table1, "m", "cons+", "voc-", "bilabial", "vd+", "cont-", "nasal+");
			AddSegDef(Table1, "n", "cons+", "voc-", "alveolar", "vd+", "strident-", "cont-", "nasal+");
			AddSegDef(Table1, "ŋ", "cons+", "voc-", "velar", "vd+", "cont-", "nasal+");
			AddSegDef(Table1, "s", "cons+", "voc-", "alveolar", "vd-", "asp-", "del_rel-", "strident+", "cont+");
			AddSegDef(Table1, "z", "cons+", "voc-", "alveolar", "vd+", "asp-", "del_rel-", "strident+", "cont+");
			AddSegDef(Table1, "f", "cons+", "voc-", "labiodental", "vd-", "asp-", "strident+", "cont+");
			AddSegDef(Table1, "v", "cons+", "voc-", "labiodental", "vd+", "asp-", "strident+", "cont+");

			Table2 = new CharacterDefinitionTable("table2", "table2", SpanFactory);
			AddSegDef(Table2, "a", "cons-", "voc+", "high-", "low+", "back+", "round-", "vd+");
			AddSegDef(Table2, "i", "cons-", "voc+", "high+", "low-", "back-", "round-", "vd+");
			AddSegDef(Table2, "u", "cons-", "voc+", "high+", "low-", "back+", "round+", "vd+");
			AddSegDef(Table2, "y", "cons-", "voc+", "high+", "low-", "back-", "round+", "vd+");
			AddSegDef(Table2, "o", "cons-", "voc+", "high-", "low-", "back+", "round+", "vd+");
			AddSegDef(Table2, "p", "cons+", "voc-", "bilabial", "vd-");
			AddSegDef(Table2, "t", "cons+", "voc-", "alveolar", "vd-", "del_rel-", "strident-");
			AddSegDef(Table2, "k", "cons+", "voc-", "velar", "vd-");
			AddSegDef(Table2, "ts", "cons+", "voc-", "alveolar", "vd-", "del_rel+", "strident+");
			AddSegDef(Table2, "b", "cons+", "voc-", "bilabial", "vd+");
			AddSegDef(Table2, "d", "cons+", "voc-", "alveolar", "vd+", "strident-");
			AddSegDef(Table2, "g", "cons+", "voc-", "velar", "vd+");
			AddSegDef(Table2, "m", "cons+", "voc-", "bilabial", "vd+", "cont-", "nasal+");
			AddSegDef(Table2, "n", "cons+", "voc-", "alveolar", "vd+", "cont-", "nasal+");
			AddSegDef(Table2, "ŋ", "cons+", "voc-", "velar", "vd+", "cont-", "nasal+");
			AddSegDef(Table2, "s", "cons+", "voc-", "alveolar", "vd-", "asp-", "del_rel-", "strident+", "cont+");
			AddSegDef(Table2, "z", "cons+", "voc-", "alveolar", "vd+", "asp-", "del_rel-", "strident+", "cont+");
			AddSegDef(Table2, "f", "cons+", "voc-", "labiodental", "vd-", "asp-", "strident+", "cont+");
			AddSegDef(Table2, "v", "cons+", "voc-", "labiodental", "vd+", "asp-", "strident+", "cont+");
			Table2.AddBoundaryDefinition("+");
			Table2.AddBoundaryDefinition("#");
			Table2.AddBoundaryDefinition("!");
			Table2.AddBoundaryDefinition(".");
			Table2.AddBoundaryDefinition("$");

			Table3 = new CharacterDefinitionTable("table3", "table3", SpanFactory);
			AddSegDef(Table3, "a", "cons-", "voc+", "high-", "low+", "back+", "round-", "vd+", "ATR+", "cont+");
			AddSegDef(Table3, "a̘", "cons-", "voc+", "high-", "low+", "back+", "round-", "vd+", "ATR-", "cont+");
			AddSegDef(Table3, "i", "cons-", "voc+", "high+", "low-", "back-", "round-", "vd+", "cont+");
			AddSegDef(Table3, "u", "cons-", "voc+", "high+", "low-", "back+", "round+", "vd+", "cont+");
			AddSegDef(Table3, "y", "cons-", "voc+", "high+", "low-", "back-", "round+", "vd+", "cont+");
			AddSegDef(Table3, "ɯ", "cons-", "voc+", "high+", "low-", "back+", "round-", "vd+", "cont+");
			AddSegDef(Table3, "o", "cons-", "voc+", "high-", "low-", "back+", "round+", "vd+", "cont+");
			AddSegDef(Table3, "p", "cons+", "voc-", "bilabial", "vd-", "cont-", "nasal-");
			AddSegDef(Table3, "t", "cons+", "voc-", "alveolar", "vd-", "del_rel-", "strident-", "cont-", "nasal-");
			AddSegDef(Table3, "k", "cons+", "voc-", "velar", "vd-", "cont-", "nasal-");
			AddSegDef(Table3, "ts", "cons+", "voc-", "alveolar", "vd-", "del_rel+", "strident+", "cont-", "nasal-");
			AddSegDef(Table3, "b", "cons+", "voc-", "bilabial", "vd+", "cont-", "nasal-");
			AddSegDef(Table3, "d", "cons+", "voc-", "alveolar", "vd+", "strident-", "cont-", "nasal-");
			AddSegDef(Table3, "g", "cons+", "voc-", "velar", "vd+", "cont-", "nasal-");
			AddSegDef(Table3, "m", "cons+", "voc-", "bilabial", "vd+", "cont-", "nasal+");
			AddSegDef(Table3, "n", "cons+", "voc-", "alveolar", "vd+", "strident-", "cont-", "nasal+");
			AddSegDef(Table3, "ŋ", "cons+", "voc-", "velar", "vd+", "cont-", "nasal+");
			AddSegDef(Table3, "s", "cons+", "voc-", "alveolar", "vd-", "asp-", "del_rel-", "strident+", "cont+");
			AddSegDef(Table3, "z", "cons+", "voc-", "alveolar", "vd+", "asp-", "del_rel-", "strident+", "cont+");
			AddSegDef(Table3, "f", "cons+", "voc-", "labiodental", "vd-", "asp-", "strident+", "cont+");
			AddSegDef(Table3, "v", "cons+", "voc-", "labiodental", "vd+", "asp-", "strident+", "cont+");
			Table3.AddBoundaryDefinition("+");
			Table3.AddBoundaryDefinition("#");
			Table3.AddBoundaryDefinition("!");
			Table3.AddBoundaryDefinition(".");

			LeftSideFS = FeatureStruct.New(HCFeatureSystem.Instance).Symbol(HCFeatureSystem.LeftSide).Value;
			RightSideFS = FeatureStruct.New(HCFeatureSystem.Instance).Symbol(HCFeatureSystem.RightSide).Value;

			Surface = new Stratum(Stratum.SurfaceStratumID, "Surface") { CharacterDefinitionTable = Table1 };
			Allophonic = new Stratum("allophonic", "Allophonic") { CharacterDefinitionTable = Table1 };
			Morphophonemic = new Stratum("morphophonemic", "Morphophonemic") { CharacterDefinitionTable = Table3 };
		}

		private void AddSegDef(CharacterDefinitionTable table, string strRep, params string[] symbols)
		{
			var fs = new FeatureStruct();
			foreach (string symbolID in symbols)
			{
				FeatureSymbol symbol = FeatSys.GetSymbol(symbolID);
				fs.AddValue(symbol.Feature, new SymbolicFeatureValue(symbol));
			}
			table.AddSegmentDefinition(strRep, fs);
		}
	}
}
