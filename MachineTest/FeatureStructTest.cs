using System;
using NUnit.Framework;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Test
{
	[TestFixture]
	public class FeatureStructTest
	{
		[Test]
		public void DisjunctiveUnify()
		{
			FeatureSystem featSys = FeatureSystem.New()
				.SymbolicFeature("rank", rank => rank
					.Symbol("clause"))
				.SymbolicFeature("case", casef => casef
					.Symbol("nom"))
				.SymbolicFeature("number", number => number
					.Symbol("pl")
					.Symbol("sing"))
				.SymbolicFeature("person", person => person
					.Symbol("2")
					.Symbol("3"))
				.StringFeature("lex")
				.SymbolicFeature("transitivity", transitivity => transitivity
					.Symbol("trans")
					.Symbol("intrans"))
				.SymbolicFeature("voice", voice => voice
					.Symbol("passive")
					.Symbol("active"))
				.StringFeature("varFeat1")
				.StringFeature("varFeat2")
				.StringFeature("varFeat3")
				.StringFeature("varFeat4")
				.ComplexFeature("subj")
				.ComplexFeature("actor")
				.ComplexFeature("goal").Value;

			FeatureStruct grammar = FeatureStruct.New(featSys)
				.Feature("rank").EqualTo("clause")
				.Feature("subj").EqualToFeatureStruct(1, subj => subj
					.Feature("case").EqualTo("nom"))
				.Feature("varFeat1").EqualToVariable("alpha")
				.Feature("varFeat2").EqualTo("value2")
				.And(and => and
					.With(disj => disj
						.Feature("voice").EqualTo("passive")
						.Feature("transitivity").EqualTo("trans")
						.Feature("goal").ReferringTo(1))
					.Or(disj => disj
						.Feature("voice").EqualTo("active")
						.Feature("actor").ReferringTo(1)
						.Feature("varFeat4").EqualToVariable("gamma")))
				.And(and => and
					.With(disj => disj
						.Feature("transitivity").EqualTo("intrans")
						.Feature("actor").EqualToFeatureStruct(actor => actor
							.Feature("person").EqualTo("3")))
					.Or(disj => disj
						.Feature("transitivity").EqualTo("trans")
						.Feature("goal").EqualToFeatureStruct(goal => goal
							.Feature("person").EqualTo("3"))
						.Feature("varFeat3").EqualToVariable("beta")))
				.And(and => and
					.With(disj => disj
						.Feature("number").EqualTo("sing")
						.Feature("subj").EqualToFeatureStruct(subj1 => subj1
							.Feature("number").EqualTo("sing")))
					.Or(disj => disj
						.Feature("number").EqualTo("pl")
						.Feature("subj").EqualToFeatureStruct(subj1 => subj1
							.Feature("number").EqualTo("pl"))
						.Feature("varFeat3").EqualTo("value3")
						.Feature("varFeat2").Not.EqualToVariable("beta"))).Value;

			FeatureStruct constituent = FeatureStruct.New(featSys)
				.Feature("subj").EqualToFeatureStruct(subj => subj
					.Feature("lex").EqualTo("y'all")
					.Feature("person").EqualTo("2")
					.Feature("number").EqualTo("pl"))
				.Feature("varFeat1").EqualTo("value1")
				.Feature("varFeat4").EqualTo("value4").Value;

			FeatureStruct output;
			Assert.IsTrue(grammar.Unify(constituent, out output));
			Assert.AreEqual(FeatureStruct.New(featSys)
				.Feature("rank").EqualTo("clause")
				.Feature("subj").EqualToFeatureStruct(1, subj => subj
					.Feature("case").EqualTo("nom")
					.Feature("lex").EqualTo("y'all")
					.Feature("person").EqualTo("2")
					.Feature("number").EqualTo("pl"))
				.Feature("number").EqualTo("pl")
				.Feature("voice").EqualTo("active")
				.Feature("actor").ReferringTo(1)
				.Feature("transitivity").EqualTo("trans")
				.Feature("goal").EqualToFeatureStruct(goal => goal.Feature("person").EqualTo("3"))
				.Feature("varFeat1").EqualTo("value1")
				.Feature("varFeat2").EqualTo("value2")
				.Feature("varFeat3").EqualTo("value3")
				.Feature("varFeat4").EqualTo("value4").Value, output);

			featSys = FeatureSystem.New()
				.SymbolicFeature("feat1", feat1 => feat1
					.Symbol("feat1+")
					.Symbol("feat1-"))
				.SymbolicFeature("feat2", feat2 => feat2
					.Symbol("feat2+")
					.Symbol("feat2-"))
				.SymbolicFeature("feat3", feat3 => feat3
					.Symbol("feat3+")
					.Symbol("feat3-"))
				.SymbolicFeature("feat4", feat4 => feat4
					.Symbol("feat4+")
					.Symbol("feat4-"))
				.SymbolicFeature("feat5", feat5 => feat5
					.Symbol("feat5+")
					.Symbol("feat5-"))
				.SymbolicFeature("feat6", feat6 => feat6
					.Symbol("feat6+")
					.Symbol("feat6-"))
				.SymbolicFeature("feat7", feat7 => feat7
					.Symbol("feat7+")
					.Symbol("feat7-")).Value;

			FeatureStruct fs1 = FeatureStruct.New(featSys)
				.And(disjunction => disjunction
					.With(disj => disj.Symbol("feat1+"))
					.Or(disj => disj.Symbol("feat2+"))).Value;

			FeatureStruct fs2 = FeatureStruct.New(featSys)
				.And(disjunction => disjunction
					.With(disj => disj.Symbol("feat3+"))
					.Or(disj => disj.Symbol("feat4+")))
				.And(disjunction => disjunction
					.With(disj => disj.Symbol("feat5+"))
					.Or(disj => disj.Symbol("feat6+"))).Value;

			Assert.IsTrue(fs1.Unify(fs2, out output));
			Assert.AreEqual(FeatureStruct.New(featSys)
				.And(disjunction => disjunction
					.With(disj => disj.Symbol("feat1+"))
					.Or(disj => disj.Symbol("feat2+")))
				.And(disjunction => disjunction
					.With(disj => disj.Symbol("feat3+"))
					.Or(disj => disj.Symbol("feat4+")))
				.And(disjunction => disjunction
					.With(disj => disj.Symbol("feat5+"))
					.Or(disj => disj.Symbol("feat6+"))).Value, output);

			fs1 = FeatureStruct.New(featSys)
				.Symbol(1, "feat1+")
				.And(disjunction => disjunction
					.With(disj => disj.Feature("feat1").ReferringTo(1))
					.Or(disj => disj.Symbol("feat1+"))
					.Or(disj => disj.Feature("feat1").ReferringTo(1))).Value;

			fs2 = FeatureStruct.New(featSys)
				.Symbol(1, "feat1+")
				.And(disjunction => disjunction
					.With(disj => disj.Symbol("feat1+"))
					.Or(disj => disj.Feature("feat1").ReferringTo(1)))
				.And(disjunction => disjunction
					.With(disj => disj.Symbol("feat3+"))
					.Or(disj => disj.Symbol("feat1-"))).Value;
			Assert.IsTrue(fs1.Unify(fs2, out output));
			Assert.AreEqual(FeatureStruct.New(featSys)
				.Symbol(1, "feat1+")
				.Symbol("feat3+")
				.And(disjunction => disjunction
					.With(disj => disj.Symbol("feat1+"))
					.Or(disj => disj.Feature("feat1").ReferringTo(1))).Value, output);
		}

		[Test]
		public void Unify()
		{
			Func<FeatureStruct, FeatureStruct, FeatureStruct> resultsSelector = (fs1, fs2) =>
																					{
																						FeatureStruct res;
																						return fs1.Unify(fs2, out res) ? res : null;
																					};
			Func<FeatureStruct, FeatureStruct, VariableBindings, FeatureStruct> varResultsSelector = (fs1, fs2, varBindings) =>
																					{
																						FeatureStruct res;
																						return fs1.Unify(fs2, varBindings, out res) ? res : null;
																					};
			TestBinaryOperation(resultsSelector, varResultsSelector,
				// simple
				featSys => null,
				featSys => FeatureStruct.New(featSys).Symbol("a2").Symbol("b1").Symbol("c2").Value,

				// complex
				featSys => null,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(cx1 => cx1.Symbol("a2"))
					.Feature("cx2").EqualToFeatureStruct(cx2 => cx2.Symbol("b1"))
					.Feature("cx3").EqualToFeatureStruct(cx2 => cx2.Symbol("c2"))
					.Feature("cx4").EqualToFeatureStruct(cx4 => cx4.Symbol("d1")).Value,

				// re-entrant
				featSys => null,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(1, cx1 => cx1.Symbol("a1"))
					.Feature("cx2").ReferringTo(1).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(cx1 => cx1.Symbol(1, "a1"))
					.Feature("cx2").EqualToFeatureStruct(cx2 => cx2.Feature("a").ReferringTo(1)).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(1, cx1 => cx1.Symbol("a1"))
					.Feature("cx2").ReferringTo(1).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(1, cx1 => cx1.Symbol("a2"))
					.Feature("cx2").ReferringTo(1).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(cx1 => cx1.Symbol(1, "a2"))
					.Feature("cx2").EqualToFeatureStruct(cx2 => cx2.Feature("a").ReferringTo(1)).Value,

				// cyclic
				featSys => null,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(1, cx1 => cx1
					.Symbol("a2").Symbol("b2").Feature("cx2").ReferringTo(1)).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(1, cx1 => cx1
					.Symbol("a2").Symbol("b1").Symbol("c1").Feature("cx2").ReferringTo(1)).Value,
				featSys => null,
				featSys => null,

				// variable
				featSys => null,
				featSys => FeatureStruct.New(featSys).Symbol("a+").Symbol("b-").Value,
				featSys => FeatureStruct.New(featSys).Symbol("a+").Symbol("b-").Value,
				featSys => FeatureStruct.New(featSys).Symbol("a+").Symbol("b-").Value,
				featSys => null);

			FeatureSystem featureSystem = FeatureSystem.New()
				.ComplexFeature("a")
				.SymbolicFeature("b", b => b
					.Symbol("c"))
				.ComplexFeature("d")
				.SymbolicFeature("e", e => e
					.Symbol("f"))
				.ComplexFeature("g")
				.SymbolicFeature("h", h => h
					.Symbol("j")).Value;

			FeatureStruct featStruct1 = FeatureStruct.New(featureSystem)
				.Feature("a").EqualToFeatureStruct(a => a
					.Feature("b").EqualTo("c"))
				.Feature("d").EqualToFeatureStruct(1, d => d
					.Feature("e").EqualTo("f"))
				.Feature("g").ReferringTo(1).Value;

			FeatureStruct featStruct2 = FeatureStruct.New(featureSystem)
				.Feature("a").EqualToFeatureStruct(1, a => a
					.Feature("b").EqualTo("c"))
				.Feature("d").ReferringTo(1)
				.Feature("g").EqualToFeatureStruct(g => g
					.Feature("h").EqualTo("j")).Value;

			FeatureStruct result;
			Assert.IsTrue(featStruct1.Unify(featStruct2, out result));

			featureSystem = FeatureSystem.New()
				.ComplexFeature("a")
				.SymbolicFeature("b", b => b
					.Symbol("c"))
				.ComplexFeature("d")
				.SymbolicFeature("e", e => e
					.Symbol("f"))
				.ComplexFeature("g")
				.SymbolicFeature("h", h => h
					.Symbol("j"))
				.ComplexFeature("i").Value;

			featStruct1 = FeatureStruct.New(featureSystem)
				.Feature("a").EqualToFeatureStruct(1, a => a
					.Feature("b").EqualTo("c"))
				.Feature("d").EqualToFeatureStruct(2, d => d
					.Feature("e").EqualTo("f"))
				.Feature("g").ReferringTo(2)
				.Feature("i").ReferringTo(1).Value;

			featStruct2 = FeatureStruct.New(featureSystem)
				.Feature("a").EqualToFeatureStruct(1, a => a
					.Feature("b").EqualTo("c"))
				.Feature("d").EqualToFeatureStruct(g => g
					.Feature("h").EqualTo("j"))
				.Feature("g").ReferringTo(1).Value;

			Assert.IsTrue(featStruct1.Unify(featStruct2, out result));
			Assert.AreEqual(FeatureStruct.New(featureSystem)
				.Feature("a").EqualToFeatureStruct(1, a => a
					.Feature("b").EqualTo("c")
					.Feature("e").EqualTo("f")
					.Feature("h").EqualTo("j"))
				.Feature("d").ReferringTo(1)
				.Feature("g").ReferringTo(1)
				.Feature("i").ReferringTo(1).Value, result);
		}

		[Test]
		public void IsUnifiable()
		{
			Func<FeatureStruct, FeatureStruct, bool> resultsSelector = (fs1, fs2) => fs1.IsUnifiable(fs2);
			Func<FeatureStruct, FeatureStruct, VariableBindings, bool> varResultsSelector = (fs1, fs2, varBindings) => fs1.IsUnifiable(fs2, varBindings);
			TestBinaryOperation(resultsSelector, varResultsSelector,
				// simple
				featSys => false,
				featSys => true,

				// complex
				featSys => false,
				featSys => true,

				// re-entrant
				featSys => false,
				featSys => true,
				featSys => true,
				featSys => true,
				featSys => true,
				featSys => true,

				// cyclic
				featSys => false,
				featSys => true,
				featSys => true,
				featSys => false,
				featSys => false,

				// variable
				featSys => false,
				featSys => true,
				featSys => true,
				featSys => true,
				featSys => false);
		}

		[Test]
		public void PriorityUnion()
		{
			Func<FeatureStruct, FeatureStruct, FeatureStruct> resultsSelector = (fs1, fs2) =>
			                                                                    	{
			                                                                    		fs1.PriorityUnion(fs2);
			                                                                    		return fs1;
			                                                                    	};
			Func<FeatureStruct, FeatureStruct, VariableBindings, FeatureStruct> varResultsSelector = (fs1, fs2, varBindings) =>
			                                                                    	{
			                                                                    		fs1.PriorityUnion(fs2, varBindings);
			                                                                    		return fs1;
			                                                                    	};
			TestBinaryOperation(resultsSelector, varResultsSelector,
				// simple
				featSys => FeatureStruct.New(featSys).Symbol("a2").Symbol("b1").Symbol("c2").Value,
				featSys => FeatureStruct.New(featSys).Symbol("a2").Symbol("b1").Symbol("c2").Value,

				// complex
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(cx1 => cx1.Symbol("a2"))
					.Feature("cx2").EqualToFeatureStruct(cx2 => cx2.Symbol("b1"))
					.Feature("cx3").EqualToFeatureStruct(cx2 => cx2.Symbol("c2"))
					.Feature("cx4").EqualToFeatureStruct(cx4 => cx4.Symbol("d2", "d3")).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(cx1 => cx1.Symbol("a2"))
					.Feature("cx2").EqualToFeatureStruct(cx2 => cx2.Symbol("b1"))
					.Feature("cx3").EqualToFeatureStruct(cx2 => cx2.Symbol("c2"))
					.Feature("cx4").EqualToFeatureStruct(cx4 => cx4.Symbol("d1", "d2")).Value,

				// re-entrant
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(1, cx1 => cx1.Symbol("a2"))
					.Feature("cx2").ReferringTo(1).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx2").EqualToFeatureStruct(1, cx2 => cx2.Symbol("a1", "a3"))
					.Feature("cx1").ReferringTo(1).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx2").EqualToFeatureStruct(cx2 => cx2.Symbol(1, "a1", "a3"))
					.Feature("cx1").EqualToFeatureStruct(cx1 => cx1.Feature("a").ReferringTo(1)).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(1, cx1 => cx1.Symbol("a1", "a2"))
					.Feature("cx2").ReferringTo(1).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(1, cx1 => cx1.Symbol("a2", "a3", "a4"))
					.Feature("cx2").ReferringTo(1).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx2").EqualToFeatureStruct(cx2 => cx2.Symbol(1, "a2", "a3", "a4"))
					.Feature("cx1").EqualToFeatureStruct(cx1 => cx1.Feature("a").ReferringTo(1)).Value,

				// cyclic
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(1, cx1 => cx1
					.Symbol("a2").Symbol("b1").Feature("cx2").ReferringTo(1)).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(1, cx1 => cx1
					.Symbol("a2").Symbol("b2").Feature("cx2").ReferringTo(1)).Value,
				// fs1 is not re-entrant and fs2 is re-entrant on cx2, so fs2 wins out and replaces fs1's value for cx2
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(1, cx1 => cx1
					.Symbol("a2").Symbol("b1").Feature("cx2").ReferringTo(1)).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(1, cx1 => cx1
					.Symbol("a2").Symbol("b1").Symbol("c2").Feature("cx2").ReferringTo(1)).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(1, cx1 => cx1
					.Symbol("a2").Symbol("b1").Symbol("c2").Feature("cx2").ReferringTo(1)).Value,

				// variable
				featSys => FeatureStruct.New(featSys).Feature("a").Not.EqualToVariable("var1").Symbol("b-").Value,
				featSys => FeatureStruct.New(featSys).Symbol("a+").Symbol("b-").Value,
				featSys => FeatureStruct.New(featSys).Feature("a").Not.EqualToVariable("var1").Symbol("b-").Value,
				featSys => FeatureStruct.New(featSys).Symbol("a+").Symbol("b-").Value,
				featSys => FeatureStruct.New(featSys).Symbol("a-").Symbol("b-").Value);
		}

		[Test]
		public void Union()
		{
			Func<FeatureStruct, FeatureStruct, FeatureStruct> resultsSelector = (fs1, fs2) =>
																					{
																						fs1.Union(fs2);
																						return fs1;
																					};
			Func<FeatureStruct, FeatureStruct, VariableBindings, FeatureStruct> varResultsSelector = (fs1, fs2, varBindings) =>
																					{
																						fs1.Union(fs2, varBindings);
																						return fs1;
																					};
			
			TestBinaryOperation(resultsSelector, varResultsSelector,
				// simple
				featSys => FeatureStruct.New(featSys).Symbol("a1", "a2").Value,
				featSys => FeatureStruct.New(featSys).Symbol("a1", "a2").Symbol("c2").Value,

				// complex
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(cx1 => cx1.Symbol("a1", "a2")).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(cx1 => cx1.Symbol("a1", "a2"))
					.Feature("cx4").EqualToFeatureStruct(cx4 => cx4.Symbol("d1", "d2")).Value,

				// re-entrant
				featSys => FeatureStruct.New(featSys).Feature("cx2").EqualToFeatureStruct(cx1 => cx1.Symbol("a1", "a2")).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(cx2 => cx2.Symbol("a1", "a2", "a3")).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(cx2 => cx2.Symbol("a1", "a2", "a3")).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(1, cx1 => cx1.Symbol("a1", "a2"))
					.Feature("cx2").ReferringTo(1).Value,
				featSys => FeatureStruct.New().Value,
				featSys => FeatureStruct.New().Value,

				// cyclic
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(cx1 => cx1.Symbol("a1", "a2")).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(cx1 => cx1.Symbol("a1", "a2")).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(cx1 => cx1.Symbol("a1", "a2")).Value,
				// fs1 is not re-entrant, so the result is also not re-entrant
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(cx1 => cx1.Symbol("a1", "a2")
					.Feature("cx2").EqualToFeatureStruct(cx2 => cx2.Symbol("c1", "c2"))).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(1, cx1 => cx1.Symbol("a1", "a2").Symbol("c1", "c2")
					.Feature("cx2").ReferringTo(1)).Value,

				// variable
				featSys => FeatureStruct.New(featSys).Symbol("b-").Value,
				// it is unclear what to do when performing a union of a variable and a value, so I just have the value win out
				featSys => FeatureStruct.New(featSys).Symbol("a+").Symbol("b-").Value,
				featSys => FeatureStruct.New(featSys).Symbol("a+").Symbol("b-").Value,
				featSys => FeatureStruct.New(featSys).Symbol("a+").Symbol("b-").Value,
				featSys => FeatureStruct.New(featSys).Symbol("b-").Value);
		}

		[Test]
		public void Subtract()
		{
			Func<FeatureStruct, FeatureStruct, FeatureStruct> resultsSelector = (fs1, fs2) =>
																					{
																						fs1.Subtract(fs2);
																						return fs1;
																					};
			Func<FeatureStruct, FeatureStruct, VariableBindings, FeatureStruct> varResultsSelector = (fs1, fs2, varBindings) =>
																					{
																						fs1.Subtract(fs2, varBindings);
																						return fs1;
																					};

			TestBinaryOperation(resultsSelector, varResultsSelector,
				// simple
				featSys => FeatureStruct.New(featSys).Symbol("a1").Symbol("b1").Value,
				featSys => FeatureStruct.New(featSys).Symbol("a1").Symbol("b1").Value,

				// complex
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(cx1 => cx1.Symbol("a1"))
					.Feature("cx2").EqualToFeatureStruct(cx2 => cx2.Symbol("b1"))
					.Feature("cx4").EqualToFeatureStruct(cx4 => cx4.Symbol("d1")).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(cx1 => cx1.Symbol("a1"))
					.Feature("cx2").EqualToFeatureStruct(cx2 => cx2.Symbol("b1")).Value,

				// re-entrant
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(1, cx1 => cx1.Symbol("a1")).Feature("cx2")
					.ReferringTo(1).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(cx1 => cx1.Symbol("a2")).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(cx1 => cx1.Symbol("a2")).Value,
				featSys => FeatureStruct.New().Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(1, cx1 => cx1.Symbol("a1"))
					.Feature("cx2").ReferringTo(1).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(cx1 => cx1.Symbol(1, "a1"))
					.Feature("cx2").EqualToFeatureStruct(cx2 => cx2.Feature("a").ReferringTo(1)).Value,

				// cyclic
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(cx1 => cx1.Symbol("a1").Symbol("b1")).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(1, cx1 => cx1.Symbol("a1")
					.Feature("cx2").ReferringTo(1)).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(cx1 => cx1
					.Symbol("a1").Symbol("b1").Feature("cx2").EqualToFeatureStruct(cx2 => cx2.Symbol("c1"))).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(cx1 => cx1
					.Symbol("a1").Symbol("b1").Feature("cx2").EqualToFeatureStruct(cx2 => cx2.Symbol("c1"))).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(1, cx1 => cx1
					.Symbol("a1").Symbol("b1").Symbol("c1").Feature("cx2").ReferringTo(1)).Value,

				// variable
				featSys => FeatureStruct.New(featSys).Feature("a").EqualToVariable("var1").Value,
				// it is unclear what to do when subtracting a variable and a value, so I preserve the existing value
				featSys => FeatureStruct.New(featSys).Feature("a").EqualToVariable("var1").Value,
				featSys => FeatureStruct.New(featSys).Symbol("a+").Value,
				featSys => FeatureStruct.New().Value,
				featSys => FeatureStruct.New(featSys).Symbol("a+").Value);
		}

		[Test]
		public void Negation()
		{
			Func<FeatureStruct, FeatureStruct> resultsSelector = fs =>
			                                                     	{
			                                                     		FeatureStruct res;
			                                                     		return fs.Negation(out res) ? res : null;
			                                                     	};
			TestUnaryOperation(resultsSelector,
				// simple
				featSys => FeatureStruct.New(featSys).And(disjunction => disjunction
					.With(disj => disj.Symbol("a2", "a3")).Or(disj => disj.Symbol("b2", "b3"))).Value,
				featSys => FeatureStruct.New(featSys).And(disjunction => disjunction
					.With(disj => disj.Symbol("a3")).Or(disj => disj.Symbol("b2", "b3")).Or(disj => disj.Symbol("c1", "c3"))).Value,

				// complex
				featSys => FeatureStruct.New(featSys).And(disjunction => disjunction
					.With(disj => disj.Feature("cx1").EqualToFeatureStruct(cx1 => cx1.Symbol("a2", "a3")))
					.Or(disj => disj.Feature("cx2").EqualToFeatureStruct(cx2 => cx2.Symbol("b2", "b3")))
					.Or(disj => disj.Feature("cx3").EqualToFeatureStruct(cx3 => cx3.Symbol("d2", "d3")))).Value,
				featSys => FeatureStruct.New(featSys).And(disjunction => disjunction
					.With(disj => disj.Feature("cx1").EqualToFeatureStruct(cx1 => cx1.Symbol("a3")))
					.Or(disj => disj.Feature("cx2").EqualToFeatureStruct(cx2 => cx2.Symbol("b2", "b3")))
					.Or(disj => disj.Feature("cx3").EqualToFeatureStruct(cx3 => cx3.And(cx3Disjunction => cx3Disjunction
						.With(cx3Disj => cx3Disj.Symbol("c1")).Or(cx3Disj => cx3Disj.Symbol("d2", "d3")))))).Value,

				// re-entrant
				featSys => FeatureStruct.New(featSys).And(disjunction => disjunction
					.With(disj => disj.Feature("cx1").EqualToFeatureStruct(1, cx1 => cx1.Symbol("a2", "a3", "a4")))
					.Or(disj => disj.Feature("cx2").ReferringTo(1))).Value,
				featSys => FeatureStruct.New(featSys).And(disjunction => disjunction
					.With(disj => disj.Feature("cx1").EqualToFeatureStruct(1, cx1 => cx1.Symbol("a1")))
					.Or(disj => disj.Feature("cx2").ReferringTo(1))).Value,
				featSys => FeatureStruct.New(featSys).And(disjunction => disjunction
					.With(disj => disj.Feature("cx1").EqualToFeatureStruct(cx1 => cx1.Symbol(1, "a2", "a4")))
					.Or(disj => disj.Feature("cx2").EqualToFeatureStruct(cx2 => cx2.Feature("a").ReferringTo(1)))).Value,

				// cyclic
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(1, cx1 => cx1.And(disjunction => disjunction
					.With(disj => disj.Symbol("a1", "a3"))
					.Or(disj => disj.Feature("cx2").ReferringTo(1)))).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(1, cx1 => cx1.And(disjunction => disjunction
					.With(disj => disj.Symbol("a3"))
					.Or(disj => disj.Feature("cx2").ReferringTo(1)))).Value,

				// variable
				featSys => FeatureStruct.New(featSys).And(disjunction => disjunction
					.With(disj => disj.Feature("a").Not.EqualToVariable("var1")).Or(disj => disj.Symbol("b+"))).Value,
				featSys => FeatureStruct.New(featSys).And(disjunction => disjunction
					.With(disj => disj.Feature("a").EqualToVariable("var1")).Or(disj => disj.Symbol("b+"))).Value,

				// disjunctive
				featSys => FeatureStruct.New(featSys).Symbol("feat1-").Symbol("feat2-").Value,
				featSys => FeatureStruct.New(featSys)
					.And(disjunction => disjunction
						.With(disj => disj.Symbol("feat1-"))
						.Or(disj => disj.Symbol("feat2-")))
					.And(disjunction => disjunction
						.With(disj => disj.Symbol("feat3-"))
						.Or(disj => disj.Symbol("feat4-"))).Value,
				featSys => FeatureStruct.New(featSys)
					.And(disjunction => disjunction
						.With(disj => disj.Symbol("feat1-"))
						.Or(disj => disj.Symbol("feat2-"))
						.Or(disj => disj.Symbol("feat3-").Symbol("feat4-"))
						.Or(disj => disj.Symbol("feat5-").Symbol("feat6-"))).Value,
				featSys => FeatureStruct.New(featSys)
					.And(disjunction => disjunction
						.With(disj => disj.Symbol("feat3-").Symbol("feat4-"))
						.Or(disj => disj.Symbol("feat5-").Feature("cfeat2").EqualToFeatureStruct(cfeat2 => cfeat2.And(cfeat2Disjunction => cfeat2Disjunction
							.With(cfeat2Disj => cfeat2Disj.Symbol("feat9-"))
							.Or(cfeat2Disj => cfeat2Disj.Symbol("feat10-")))))
						.Or(disj => disj.Feature("cfeat1").EqualToFeatureStruct(cfeat1 => cfeat1.And(cfeat1Disjunction => cfeat1Disjunction
							.With(cfeat1Disj => cfeat1Disj.Symbol("feat7-"))
							.Or(cfeat1Disj => cfeat1Disj.Symbol("feat8-")))))
						.Or(disj => disj.Symbol("feat1-"))
						.Or(disj => disj.Symbol("feat2-"))).Value);
		}

		private void TestUnaryOperation<TResult>(Func<FeatureStruct, TResult> resultsSelector, params Func<FeatureSystem, TResult>[] expectedSelectors)
		{
			// simple
			FeatureSystem featSys = FeatureSystem.New()
				.SymbolicFeature("a", a => a
					.Symbol("a1")
					.Symbol("a2")
					.Symbol("a3"))
				.SymbolicFeature("b", b => b
					.Symbol("b1")
					.Symbol("b2")
					.Symbol("b3"))
				.SymbolicFeature("c", c => c
					.Symbol("c1")
					.Symbol("c2")
					.Symbol("c3")).Value;

			FeatureStruct fs = FeatureStruct.New(featSys).Symbol("a1").Symbol("b1").Value;
			Assert.AreEqual(expectedSelectors[0](featSys), resultsSelector(fs));

			fs = FeatureStruct.New(featSys).Symbol("a1", "a2").Symbol("b1").Symbol("c2").Value;
			Assert.AreEqual(expectedSelectors[1](featSys), resultsSelector(fs));

			// complex
			featSys = FeatureSystem.New()
				.ComplexFeature("cx1")
				.SymbolicFeature("a", a => a
					.Symbol("a1")
					.Symbol("a2")
					.Symbol("a3"))
				.ComplexFeature("cx2")
				.SymbolicFeature("b", b => b
					.Symbol("b1")
					.Symbol("b2")
					.Symbol("b3"))
				.ComplexFeature("cx3")
				.SymbolicFeature("c", c => c
					.Symbol("c1")
					.Symbol("c2")
					.Symbol("c3"))
				.SymbolicFeature("d", d => d
					.Symbol("d1")
					.Symbol("d2")
					.Symbol("d3")).Value;

			fs = FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(cx1 => cx1.Symbol("a1"))
				.Feature("cx2").EqualToFeatureStruct(cx2 => cx2.Symbol("b1"))
				.Feature("cx3").EqualToFeatureStruct(cx3 => cx3.Symbol("d1")).Value;
			Assert.AreEqual(expectedSelectors[2](featSys), resultsSelector(fs));

			fs = FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(cx1 => cx1.Symbol("a1", "a2"))
				.Feature("cx2").EqualToFeatureStruct(cx2 => cx2.Symbol("b1"))
				.Feature("cx3").EqualToFeatureStruct(cx3 => cx3.Symbol("c2", "c3").Symbol("d1")).Value;
			Assert.AreEqual(expectedSelectors[3](featSys), resultsSelector(fs));

			// re-entrant
			featSys = FeatureSystem.New()
				.ComplexFeature("cx1")
				.SymbolicFeature("a", a => a
					.Symbol("a1")
					.Symbol("a2")
					.Symbol("a3")
					.Symbol("a4"))
				.ComplexFeature("cx2").Value;

			fs = FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(1, cx1 => cx1.Symbol("a1"))
				.Feature("cx2").ReferringTo(1).Value;
			Assert.AreEqual(expectedSelectors[4](featSys), resultsSelector(fs));

			fs = FeatureStruct.New(featSys).Feature("cx2").EqualToFeatureStruct(1, cx2 => cx2.Symbol("a2", "a3", "a4"))
				.Feature("cx1").ReferringTo(1).Value;
			Assert.AreEqual(expectedSelectors[5](featSys), resultsSelector(fs));

			fs = FeatureStruct.New(featSys).Feature("cx2").EqualToFeatureStruct(cx2 => cx2.Symbol(1, "a1", "a3"))
				.Feature("cx1").EqualToFeatureStruct(cx1 => cx1.Feature("a").ReferringTo(1)).Value;
			Assert.AreEqual(expectedSelectors[6](featSys), resultsSelector(fs));

			// cyclic
			featSys = FeatureSystem.New()
				.ComplexFeature("cx1")
				.SymbolicFeature("a", a => a
					.Symbol("a1")
					.Symbol("a2")
					.Symbol("a3"))
				.SymbolicFeature("b", b => b
					.Symbol("b1")
					.Symbol("b2")
					.Symbol("b3"))
				.SymbolicFeature("c", b => b
					.Symbol("c1")
					.Symbol("c2")
					.Symbol("c3"))
				.ComplexFeature("cx2").Value;

			fs = FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(1, cx1 => cx1
				.Symbol("a2").Feature("cx2").ReferringTo(1)).Value;
			Assert.AreEqual(expectedSelectors[7](featSys), resultsSelector(fs));

			fs = FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(1, cx1 => cx1
				.Symbol("a1", "a2").Feature("cx2").ReferringTo(1)).Value;
			Assert.AreEqual(expectedSelectors[8](featSys), resultsSelector(fs));

			// variable
			featSys = FeatureSystem.New()
				.SymbolicFeature("a", a => a
					.Symbol("a+", "+")
					.Symbol("a-", "-"))
				.SymbolicFeature("b", b => b
					.Symbol("b+", "+")
					.Symbol("b-", "-")).Value;

			fs = FeatureStruct.New(featSys)
				.Feature("a").EqualToVariable("var1")
				.Symbol("b-").Value;

			Assert.AreEqual(expectedSelectors[9](featSys), resultsSelector(fs));

			fs = FeatureStruct.New(featSys)
				.Feature("a").Not.EqualToVariable("var1")
				.Symbol("b-").Value;

			Assert.AreEqual(expectedSelectors[10](featSys), resultsSelector(fs));

			// disjunctive
			featSys = FeatureSystem.New()
				.SymbolicFeature("feat1", feat1 => feat1
					.Symbol("feat1+")
					.Symbol("feat1-"))
				.SymbolicFeature("feat2", feat2 => feat2
					.Symbol("feat2+")
					.Symbol("feat2-"))
				.SymbolicFeature("feat3", feat3 => feat3
					.Symbol("feat3+")
					.Symbol("feat3-"))
				.SymbolicFeature("feat4", feat4 => feat4
					.Symbol("feat4+")
					.Symbol("feat4-"))
				.SymbolicFeature("feat5", feat5 => feat5
					.Symbol("feat5+")
					.Symbol("feat5-"))
				.SymbolicFeature("feat6", feat6 => feat6
					.Symbol("feat6+")
					.Symbol("feat6-"))
				.ComplexFeature("cfeat1")
				.SymbolicFeature("feat7", feat7 => feat7
					.Symbol("feat7+")
					.Symbol("feat7-"))
				.SymbolicFeature("feat8", feat8 => feat8
					.Symbol("feat8+")
					.Symbol("feat8-"))
				.ComplexFeature("cfeat2")
				.SymbolicFeature("feat9", feat9 => feat9
					.Symbol("feat9+")
					.Symbol("feat9-"))
				.SymbolicFeature("feat10", feat10 => feat10
					.Symbol("feat10+")
					.Symbol("feat10-")).Value;

			fs = FeatureStruct.New(featSys)
				.And(disjunction => disjunction
					.With(disj => disj.Symbol("feat1+"))
					.Or(disj => disj.Symbol("feat2+"))).Value;
			Assert.AreEqual(expectedSelectors[11](featSys), resultsSelector(fs));

			fs = FeatureStruct.New(featSys)
				.And(disjunction => disjunction
					.With(disj => disj.Symbol("feat1+").Symbol("feat2+"))
					.Or(disj => disj.Symbol("feat3+").Symbol("feat4+"))).Value;
			Assert.AreEqual(expectedSelectors[12](featSys), resultsSelector(fs));

			fs = FeatureStruct.New(featSys)
				.Symbol("feat1+")
				.Symbol("feat2+")
				.And(disjunction => disjunction
					.With(disj => disj.Symbol("feat3+"))
					.Or(disj => disj.Symbol("feat4+")))
				.And(disjunction => disjunction
					.With(disj => disj.Symbol("feat5+"))
					.Or(disj => disj.Symbol("feat6+"))).Value;
			Assert.AreEqual(expectedSelectors[13](featSys), resultsSelector(fs));

			fs = FeatureStruct.New(featSys)
				.Feature("cfeat1").EqualToFeatureStruct(cfeat1 => cfeat1.Symbol("feat7+").Symbol("feat8+"))
				.Symbol("feat1+")
				.Symbol("feat2+")
				.And(disjunction => disjunction
					.With(disj => disj.Symbol("feat3+"))
					.Or(disj => disj.Symbol("feat4+")))
				.And(disjunction => disjunction
					.With(disj => disj.Feature("cfeat2").EqualToFeatureStruct(cfeat2 => cfeat2.Symbol("feat9+").Symbol("feat10+")))
					.Or(disj => disj.Symbol("feat5+"))).Value;
			Assert.AreEqual(expectedSelectors[14](featSys), resultsSelector(fs));
		}

		private void TestBinaryOperation<TResult>(Func<FeatureStruct, FeatureStruct, TResult> resultsSelector, Func<FeatureStruct, FeatureStruct, VariableBindings, TResult> varResultsSelector,
			params Func<FeatureSystem, TResult>[] expectedSelectors)
		{
			// simple
			FeatureSystem featSys = FeatureSystem.New()
				.SymbolicFeature("a", a => a
					.Symbol("a1")
					.Symbol("a2")
					.Symbol("a3"))
				.SymbolicFeature("b", b => b
					.Symbol("b1")
					.Symbol("b2")
					.Symbol("b3"))
				.SymbolicFeature("c", c => c
					.Symbol("c1")
					.Symbol("c2")
					.Symbol("c3")).Value;

			FeatureStruct fs1 = FeatureStruct.New(featSys).Symbol("a1").Symbol("b1").Value;
			FeatureStruct fs2 = FeatureStruct.New(featSys).Symbol("a2").Symbol("c2").Value;
			Assert.AreEqual(expectedSelectors[0](featSys), resultsSelector(fs1, fs2));

			fs1 = FeatureStruct.New(featSys).Symbol("a1", "a2").Symbol("b1").Symbol("c2").Value;
			fs2 = FeatureStruct.New(featSys).Symbol("a2").Symbol("c2").Value;
			Assert.AreEqual(expectedSelectors[1](featSys), resultsSelector(fs1, fs2));

			// complex
			featSys = FeatureSystem.New()
				.ComplexFeature("cx1")
				.SymbolicFeature("a", a => a
					.Symbol("a1")
					.Symbol("a2")
					.Symbol("a3"))
				.ComplexFeature("cx2")
				.SymbolicFeature("b", b => b
					.Symbol("b1")
					.Symbol("b2")
					.Symbol("b3"))
				.ComplexFeature("cx3")
				.SymbolicFeature("c", c => c
					.Symbol("c1")
					.Symbol("c2")
					.Symbol("c3"))
				.ComplexFeature("cx4")
				.SymbolicFeature("d", d => d
					.Symbol("d1")
					.Symbol("d2")
					.Symbol("d3")).Value;

			fs1 = FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(cx1 => cx1.Symbol("a1"))
				.Feature("cx2").EqualToFeatureStruct(cx2 => cx2.Symbol("b1"))
				.Feature("cx4").EqualToFeatureStruct(cx4 => cx4.Symbol("d1")).Value;
			fs2 = FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(cx1 => cx1.Symbol("a2"))
				.Feature("cx3").EqualToFeatureStruct(cx2 => cx2.Symbol("c2"))
				.Feature("cx4").EqualToFeatureStruct(cx4 => cx4.Symbol("d2", "d3")).Value;
			Assert.AreEqual(expectedSelectors[2](featSys), resultsSelector(fs1, fs2));

			fs1 = FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(cx1 => cx1.Symbol("a1", "a2"))
				.Feature("cx2").EqualToFeatureStruct(cx2 => cx2.Symbol("b1"))
				.Feature("cx4").EqualToFeatureStruct(cx4 => cx4.Symbol("d1")).Value;
			fs2 = FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(cx1 => cx1.Symbol("a2"))
				.Feature("cx3").EqualToFeatureStruct(cx2 => cx2.Symbol("c2"))
				.Feature("cx4").EqualToFeatureStruct(cx4 => cx4.Symbol("d1", "d2")).Value;
			Assert.AreEqual(expectedSelectors[3](featSys), resultsSelector(fs1, fs2));

			// re-entrant
			featSys = FeatureSystem.New()
				.ComplexFeature("cx1")
				.SymbolicFeature("a", a => a
					.Symbol("a1")
					.Symbol("a2")
					.Symbol("a3")
					.Symbol("a4"))
				.ComplexFeature("cx2").Value;

			fs1 = FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(1, cx1 => cx1.Symbol("a1"))
				.Feature("cx2").ReferringTo(1).Value;
			fs2 = FeatureStruct.New(featSys).Feature("cx2").EqualToFeatureStruct(cx2 => cx2.Symbol("a2")).Value;
			Assert.AreEqual(expectedSelectors[4](featSys), resultsSelector(fs1, fs2));

			fs1 = FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(cx1 => cx1.Symbol("a1", "a2")).Value;
			fs2 = FeatureStruct.New(featSys).Feature("cx2").EqualToFeatureStruct(1, cx2 => cx2.Symbol("a1", "a3"))
				.Feature("cx1").ReferringTo(1).Value;
			Assert.AreEqual(expectedSelectors[5](featSys), resultsSelector(fs1, fs2));

			fs1 = FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(cx1 => cx1.Symbol("a1", "a2")).Value;
			fs2 = FeatureStruct.New(featSys).Feature("cx2").EqualToFeatureStruct(cx2 => cx2.Symbol(1, "a1", "a3"))
				.Feature("cx1").EqualToFeatureStruct(cx1 => cx1.Feature("a").ReferringTo(1)).Value;
			Assert.AreEqual(expectedSelectors[6](featSys), resultsSelector(fs1, fs2));

			fs1 = FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(1, cx1 => cx1.Symbol("a1"))
				.Feature("cx2").ReferringTo(1).Value;
			fs2 = FeatureStruct.New(featSys).Feature("cx2").EqualToFeatureStruct(1, cx2 => cx2.Symbol("a1", "a2"))
				.Feature("cx1").ReferringTo(1).Value;
			Assert.AreEqual(expectedSelectors[7](featSys), resultsSelector(fs1, fs2));

			fs1 = FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(1, cx1 => cx1.Symbol("a1", "a2"))
				.Feature("cx2").ReferringTo(1).Value;
			fs2 = FeatureStruct.New(featSys).Feature("cx2").EqualToFeatureStruct(1, cx2 => cx2.Symbol("a2", "a3", "a4"))
				.Feature("cx1").ReferringTo(1).Value;
			Assert.AreEqual(expectedSelectors[8](featSys), resultsSelector(fs1, fs2));

			fs1 = FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(cx1 => cx1.Symbol(1, "a1", "a2"))
				.Feature("cx2").EqualToFeatureStruct(cx2 => cx2.Feature("a").ReferringTo(1)).Value;
			fs2 = FeatureStruct.New(featSys).Feature("cx2").EqualToFeatureStruct(cx2 => cx2.Symbol(1, "a2", "a3", "a4"))
				.Feature("cx1").EqualToFeatureStruct(cx1 => cx1.Feature("a").ReferringTo(1)).Value;
			Assert.AreEqual(expectedSelectors[9](featSys), resultsSelector(fs1, fs2));

			// cyclic
			featSys = FeatureSystem.New()
				.ComplexFeature("cx1")
				.SymbolicFeature("a", a => a
					.Symbol("a1")
					.Symbol("a2")
					.Symbol("a3"))
				.SymbolicFeature("b", b => b
					.Symbol("b1")
					.Symbol("b2")
					.Symbol("b3"))
				.SymbolicFeature("c", b => b
					.Symbol("c1")
					.Symbol("c2")
					.Symbol("c3"))
				.ComplexFeature("cx2").Value;

			fs1 = FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(cx1 => cx1
				.Symbol("a1").Symbol("b1")).Value;
			fs2 = FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(1, cx1 => cx1
				.Symbol("a2").Feature("cx2").ReferringTo(1)).Value;
			Assert.AreEqual(expectedSelectors[10](featSys), resultsSelector(fs1, fs2));

			fs1 = FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(1, cx1 => cx1
				.Symbol("a1", "a2").Feature("cx2").ReferringTo(1)).Value;
			fs2 = FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(cx1 => cx1
				.Symbol("a2").Symbol("b2")).Value;
			Assert.AreEqual(expectedSelectors[11](featSys), resultsSelector(fs1, fs2));

			fs1 = FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(cx1 => cx1
				.Symbol("a1", "a2").Symbol("b1").Feature("cx2").EqualToFeatureStruct(cx2 => cx2.Symbol("c1"))).Value;
			fs2 = FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(1, cx1 => cx1
				.Symbol("a2").Feature("cx2").ReferringTo(1)).Value;
			Assert.AreEqual(expectedSelectors[12](featSys), resultsSelector(fs1, fs2));

			fs1 = FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(cx1 => cx1
				.Symbol("a1").Symbol("b1").Feature("cx2").EqualToFeatureStruct(cx2 => cx2.Symbol("c1", "c2"))).Value;
			fs2 = FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(1, cx1 => cx1
				.Symbol("a2").Symbol("c2").Feature("cx2").ReferringTo(1)).Value;
			Assert.AreEqual(expectedSelectors[13](featSys), resultsSelector(fs1, fs2));

			fs1 = FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(1, cx1 => cx1
				.Symbol("a1").Symbol("b1").Symbol("c1").Feature("cx2").ReferringTo(1)).Value;
			fs2 = FeatureStruct.New(featSys).Feature("cx1").EqualToFeatureStruct(1, cx1 => cx1
				.Symbol("a2").Symbol("c2").Feature("cx2").ReferringTo(1)).Value;
			Assert.AreEqual(expectedSelectors[14](featSys), resultsSelector(fs1, fs2));

			// variable
			featSys = FeatureSystem.New()
				.SymbolicFeature("a", a => a
					.Symbol("a+", "+")
					.Symbol("a-", "-"))
				.SymbolicFeature("b", b => b
					.Symbol("b+", "+")
					.Symbol("b-", "-")).Value;

			fs1 = FeatureStruct.New(featSys)
				.Feature("a").EqualToVariable("var1")
				.Symbol("b-").Value;

			fs2 = FeatureStruct.New(featSys)
				.Feature("a").Not.EqualToVariable("var1")
				.Symbol("b-").Value;

			Assert.AreEqual(expectedSelectors[15](featSys), resultsSelector(fs1, fs2));

			fs1 = FeatureStruct.New(featSys)
				.Feature("a").EqualToVariable("var1")
				.Symbol("b-").Value;

			fs2 = FeatureStruct.New(featSys)
				.Symbol("a+")
				.Symbol("b-").Value;

			Assert.AreEqual(expectedSelectors[16](featSys), resultsSelector(fs1, fs2));

			fs1 = FeatureStruct.New(featSys)
				.Symbol("a+")
				.Symbol("b-").Value;

			fs2 = FeatureStruct.New(featSys)
				.Feature("a").Not.EqualToVariable("var1")
				.Symbol("b-").Value;

			Assert.AreEqual(expectedSelectors[17](featSys), resultsSelector(fs1, fs2));

			fs1 = FeatureStruct.New(featSys)
				.Symbol("a+")
				.Symbol("b-").Value;

			fs2 = FeatureStruct.New(featSys)
				.Feature("a").Not.EqualToVariable("var1")
				.Symbol("b-").Value;

			var varBindings = new VariableBindings();
			varBindings["var1"] = new SymbolicFeatureValue(featSys.GetSymbol("a-"));
			Assert.AreEqual(expectedSelectors[18](featSys), varResultsSelector(fs1, fs2, varBindings));

			fs1 = FeatureStruct.New(featSys)
				.Symbol("a+")
				.Symbol("b-").Value;

			fs2 = FeatureStruct.New(featSys)
				.Feature("a").Not.EqualToVariable("var1")
				.Symbol("b-").Value;

			varBindings = new VariableBindings();
			varBindings["var1"] = new SymbolicFeatureValue(featSys.GetSymbol("a+"));
			Assert.AreEqual(expectedSelectors[19](featSys), varResultsSelector(fs1, fs2, varBindings));
		}
	}
}
