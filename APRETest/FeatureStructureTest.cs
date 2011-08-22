using System.Collections.Generic;
using NUnit.Framework;
using SIL.APRE.FeatureModel;

namespace SIL.APRE.Test
{
	[TestFixture]
	public class FeatureStructureTest
	{
		[Test]
		public void DisjunctiveUnify()
		{
			FeatureSystem featSys = FeatureSystem.Build()
				.SymbolicFeature("rank", rank => rank
					.Symbol("clause"))
				.ComplexFeature("subj", subj => subj
					.SymbolicFeature("case", casef => casef
						.Symbol("nom"))
					.SymbolicFeature("number", number => number
						.Symbol("pl")
						.Symbol("sing"))
					.SymbolicFeature("person", person => person
						.Symbol("2")
						.Symbol("3"))
					.StringFeature("lex"))
				.SymbolicFeature("transitivity", transitivity => transitivity
					.Symbol("trans")
					.Symbol("intrans"))
				.SymbolicFeature("voice", voice => voice
					.Symbol("passive")
					.Symbol("active"))
				.ComplexFeature("goal", goal => goal
					.ExtantFeature("case")
					.ExtantFeature("number")
					.ExtantFeature("person")
					.ExtantFeature("lex"))
				.ComplexFeature("actor", actor => actor
					.ExtantFeature("case")
					.ExtantFeature("number")
					.ExtantFeature("person")
					.ExtantFeature("lex"));

			FeatureStructure grammar = featSys.BuildFS()
				.Symbol("rank", "clause")
				.FeatureStructure("subj", subj => subj
					.Symbol("case", "nom"))
				.Or(or => or
					.FeatureStructure(disj => disj
						.Symbol("voice", "passive")
						.Symbol("transitivity", "trans")
						.Pointer("goal", "subj"))
					.FeatureStructure(disj => disj
						.Symbol("voice", "active")
						.Pointer("actor", "subj")))
				.Or(or => or
					.FeatureStructure(disj => disj
						.Symbol("transitivity", "intrans")
						.FeatureStructure("actor", actor => actor
							.Symbol("person", "3")))
					.FeatureStructure(disj => disj
						.Symbol("transitivity", "trans")
						.FeatureStructure("goal", goal => goal
							.Symbol("person", "3"))))
				.Or(or => or
					.FeatureStructure(disj => disj
						.Symbol("number", "sing")
						.FeatureStructure("subj", subj1 => subj1
							.Symbol("number", "sing")))
					.FeatureStructure(disj => disj
						.Symbol("number", "pl")
						.FeatureStructure("subj", subj1 => subj1
							.Symbol("number", "pl"))));

			FeatureStructure constituent = featSys.BuildFS()
				.FeatureStructure("subj", subj => subj
					.String("lex", "y'all")
					.Symbol("person", "2")
					.Symbol("number", "pl"));

			FeatureStructure output;
			grammar.Unify(constituent, false, false, out output);
		}

		[Test]
		public void Unify1()
		{
			FeatureSystem featSys = FeatureSystem.Build()
				.ComplexFeature("a", a => a
					.SymbolicFeature("b", b => b
						.Symbol("c"))
					.SymbolicFeature("e", e => e
						.Symbol("f"))
					.SymbolicFeature("h", h => h
						.Symbol("j")))
				.ComplexFeature("d", d => d
					.ExtantFeature("b")
					.ExtantFeature("e")
					.ExtantFeature("h"))
				.ComplexFeature("g", g => g
					.ExtantFeature("b")
					.ExtantFeature("e")
					.ExtantFeature("h"));

			FeatureStructure fs1 = featSys.BuildFS()
				.FeatureStructure("a", a => a
					.Symbol("b", "c"))
				.FeatureStructure("d", d => d
					.Symbol("e", "f"))
				.Pointer("g", "d");

			FeatureStructure fs2 = featSys.BuildFS()
				.FeatureStructure("a", a => a
					.Symbol("b", "c"))
				.Pointer("d", "a")
				.FeatureStructure("g", g => g
					.Symbol("h", "j"));

			FeatureStructure result;
			Assert.IsTrue(fs1.Unify(fs2, false, true, out result));
		}

		[Test]
		public void Unify2()
		{
			FeatureSystem featSys = FeatureSystem.Build()
				.ComplexFeature("a", a => a
					.SymbolicFeature("b", b => b
						.Symbol("c"))
					.SymbolicFeature("e", e => e
						.Symbol("f"))
					.SymbolicFeature("h", h => h
						.Symbol("j")))
				.ComplexFeature("d", d => d
					.ExtantFeature("b")
					.ExtantFeature("e")
					.ExtantFeature("h"))
				.ComplexFeature("g", g => g
					.ExtantFeature("b")
					.ExtantFeature("e")
					.ExtantFeature("h"))
				.ComplexFeature("i", i => i
					.ExtantFeature("b"));

			FeatureStructure fs1 = featSys.BuildFS()
				.FeatureStructure("a", a => a
					.Symbol("b", "c"))
				.FeatureStructure("d", d => d
					.Symbol("e", "f"))
				.Pointer("g", "d")
				.Pointer("i", "a", "b");

			FeatureStructure fs2 = featSys.BuildFS()
				.FeatureStructure("a", a => a
					.Symbol("b", "c"))
				.FeatureStructure("d", g => g
					.Symbol("h", "j"))
				.Pointer("g", "a");

			FeatureStructure result;
			Assert.IsTrue(fs1.Unify(fs2, false, true, out result));
		}

		[Test]
		public void UnifyVariables()
		{
			FeatureSystem featSys = FeatureSystem.Build()
				.SymbolicFeature("a", a => a
					.Symbol("a+", "+")
					.Symbol("a-", "-"))
				.SymbolicFeature("b", b => b
					.Symbol("b+", "+")
					.Symbol("b-", "-"));

			FeatureStructure fs1 = featSys.BuildFS()
				.Variable("a", "var1")
				.Symbol("b-");

			FeatureStructure fs2 = featSys.BuildFS()
				.Not().Variable("a", "var1")
				.Symbol("b-");

			FeatureStructure result;
			Assert.IsFalse(fs1.Unify(fs2, false, true, out result));

			fs1 = featSys.BuildFS()
				.Variable("a", "var1")
				.Symbol("b-");

			fs2 = featSys.BuildFS()
				.Symbol("a+")
				.Symbol("b-");

			Assert.IsTrue(fs1.Unify(fs2, false, true, out result));

			fs1 = featSys.BuildFS()
				.Symbol("a+")
				.Symbol("b-");

			fs2 = featSys.BuildFS()
				.Not().Variable("a", "var1")
				.Symbol("b-");

			Assert.IsTrue(fs1.Unify(fs2, false, true, out result));

			fs1 = featSys.BuildFS()
				.Symbol("a+")
				.Symbol("b-");

			fs2 = featSys.BuildFS()
				.Not().Variable("a", "var1")
				.Symbol("b-");

			var varBindings = new Dictionary<string, FeatureValue> {{"var1", new SymbolicFeatureValue(featSys.GetSymbol("a-"))}};
			Assert.IsTrue(fs1.Unify(fs2, false, true, varBindings, out result));

			varBindings = new Dictionary<string, FeatureValue> {{"var1", new SymbolicFeatureValue(featSys.GetSymbol("a+"))}};
			Assert.IsFalse(fs1.Unify(fs2, false, true, varBindings, out result));
		}
	}
}
