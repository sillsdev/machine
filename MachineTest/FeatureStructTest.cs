using System;
using System.Collections.Generic;
using NUnit.Framework;
using SIL.Collections;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Test
{
	[TestFixture]
	public class FeatureStructTest
	{
		[Test]
		public void DisjunctiveUnify()
		{
			var featSys = new FeatureSystem
							{
								new SymbolicFeature("rank") {PossibleSymbols = {"clause"}},
								new SymbolicFeature("case") {PossibleSymbols = {"nom"}},
								new SymbolicFeature("number") {PossibleSymbols = {"pl", "sing"}},
								new SymbolicFeature("person") {PossibleSymbols = {"2", "3"}},
								new StringFeature("lex"),
								new SymbolicFeature("transitivity") {PossibleSymbols = {"trans", "intrans"}},
								new SymbolicFeature("voice") {PossibleSymbols = {"passive", "active"}},
								new StringFeature("varFeat1"),
								new StringFeature("varFeat2"),
								new StringFeature("varFeat3"),
								new StringFeature("varFeat4"),
								new ComplexFeature("subj"),
								new ComplexFeature("actor"),
								new ComplexFeature("goal")
							};

			FeatureStruct grammar = FeatureStruct.New(featSys)
				.Feature("rank").EqualTo("clause")
				.Feature("subj").EqualTo(1, subj => subj
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
						.Feature("actor").EqualTo(actor => actor
							.Feature("person").EqualTo("3")))
					.Or(disj => disj
						.Feature("transitivity").EqualTo("trans")
						.Feature("goal").EqualTo(goal => goal
							.Feature("person").EqualTo("3"))
						.Feature("varFeat3").EqualToVariable("beta")))
				.And(and => and
					.With(disj => disj
						.Feature("number").EqualTo("sing")
						.Feature("subj").EqualTo(subj1 => subj1
							.Feature("number").EqualTo("sing")))
					.Or(disj => disj
						.Feature("number").EqualTo("pl")
						.Feature("subj").EqualTo(subj1 => subj1
							.Feature("number").EqualTo("pl"))
						.Feature("varFeat3").EqualTo("value3")
						.Feature("varFeat2").Not.EqualToVariable("beta"))).Value;

			FeatureStruct constituent = FeatureStruct.New(featSys)
				.Feature("subj").EqualTo(subj => subj
					.Feature("lex").EqualTo("y'all")
					.Feature("person").EqualTo("2")
					.Feature("number").EqualTo("pl"))
				.Feature("varFeat1").EqualTo("value1")
				.Feature("varFeat4").EqualTo("value4").Value;

			FeatureStruct output;
			Assert.That(grammar.Unify(constituent, out output), Is.True);
			Assert.That(output, Is.EqualTo(FeatureStruct.New(featSys)
				.Feature("rank").EqualTo("clause")
				.Feature("subj").EqualTo(1, subj => subj
					.Feature("case").EqualTo("nom")
					.Feature("lex").EqualTo("y'all")
					.Feature("person").EqualTo("2")
					.Feature("number").EqualTo("pl"))
				.Feature("number").EqualTo("pl")
				.Feature("voice").EqualTo("active")
				.Feature("actor").ReferringTo(1)
				.Feature("transitivity").EqualTo("trans")
				.Feature("goal").EqualTo(goal => goal.Feature("person").EqualTo("3"))
				.Feature("varFeat1").EqualTo("value1")
				.Feature("varFeat2").EqualTo("value2")
				.Feature("varFeat3").EqualTo("value3")
				.Feature("varFeat4").EqualTo("value4").Value).Using((IEqualityComparer<FeatureStruct>) FreezableEqualityComparer<FeatureStruct>.Instance));

			featSys = new FeatureSystem
			          	{
			          		new SymbolicFeature("feat1") {PossibleSymbols = {"feat1+", "feat1-"}},
			          		new SymbolicFeature("feat2") {PossibleSymbols = {"feat2+", "feat2-"}},
			          		new SymbolicFeature("feat3") {PossibleSymbols = {"feat3+", "feat3-"}},
			          		new SymbolicFeature("feat4") {PossibleSymbols = {"feat4+", "feat4-"}},
			          		new SymbolicFeature("feat5") {PossibleSymbols = {"feat5+", "feat5-"}},
			          		new SymbolicFeature("feat6") {PossibleSymbols = {"feat6+", "feat6-"}},
			          		new SymbolicFeature("feat7") {PossibleSymbols = {"feat7+", "feat7-"}},
			          	};

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

			Assert.That(fs1.Unify(fs2, out output), Is.True);
			Assert.That(output, Is.EqualTo(FeatureStruct.New(featSys)
				.And(disjunction => disjunction
					.With(disj => disj.Symbol("feat1+"))
					.Or(disj => disj.Symbol("feat2+")))
				.And(disjunction => disjunction
					.With(disj => disj.Symbol("feat3+"))
					.Or(disj => disj.Symbol("feat4+")))
				.And(disjunction => disjunction
					.With(disj => disj.Symbol("feat5+"))
					.Or(disj => disj.Symbol("feat6+"))).Value).Using((IEqualityComparer<FeatureStruct>) FreezableEqualityComparer<FeatureStruct>.Instance));

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
			Assert.That(fs1.Unify(fs2, out output), Is.True);
			Assert.That(output, Is.EqualTo(FeatureStruct.New(featSys)
				.Symbol(1, "feat1+")
				.Symbol("feat3+")
				.And(disjunction => disjunction
					.With(disj => disj.Symbol("feat1+"))
					.Or(disj => disj.Feature("feat1").ReferringTo(1))).Value).Using((IEqualityComparer<FeatureStruct>) FreezableEqualityComparer<FeatureStruct>.Instance));
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
			TestBinaryOperation(FreezableEqualityComparer<FeatureStruct>.Instance, resultsSelector, varResultsSelector,
				// simple
				featSys => null,
				featSys => FeatureStruct.New(featSys).Symbol("a2").Symbol("b1").Symbol("c2").Value,

				// complex
				featSys => null,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(cx1 => cx1.Symbol("a2"))
					.Feature("cx2").EqualTo(cx2 => cx2.Symbol("b1"))
					.Feature("cx3").EqualTo(cx2 => cx2.Symbol("c2"))
					.Feature("cx4").EqualTo(cx4 => cx4.Symbol("d1")).Value,

				// re-entrant
				featSys => null,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(1, cx1 => cx1.Symbol("a1"))
					.Feature("cx2").ReferringTo(1).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(cx1 => cx1.Symbol(1, "a1"))
					.Feature("cx2").EqualTo(cx2 => cx2.Feature("a").ReferringTo(1)).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(1, cx1 => cx1.Symbol("a1"))
					.Feature("cx2").ReferringTo(1).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(1, cx1 => cx1.Symbol("a2"))
					.Feature("cx2").ReferringTo(1).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(cx1 => cx1.Symbol(1, "a2"))
					.Feature("cx2").EqualTo(cx2 => cx2.Feature("a").ReferringTo(1)).Value,

				// cyclic
				featSys => null,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(1, cx1 => cx1
					.Symbol("a2").Symbol("b2").Feature("cx2").ReferringTo(1)).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(1, cx1 => cx1
					.Symbol("a2").Symbol("b1").Symbol("c1").Feature("cx2").ReferringTo(1)).Value,
				featSys => null,
				featSys => null,

				// variable
				featSys => null,
				featSys => FeatureStruct.New(featSys).Symbol("a+").Symbol("b-").Value,
				featSys => FeatureStruct.New(featSys).Symbol("a+").Symbol("b-").Value,
				featSys => FeatureStruct.New(featSys).Symbol("a+").Symbol("b-").Value,
				featSys => null);

			var featureSystem = new FeatureSystem
			                    	{
			                    		new ComplexFeature("a"),
			                    		new SymbolicFeature("b") {PossibleSymbols = {"c"}},
			                    		new ComplexFeature("d"),
			                    		new SymbolicFeature("e") {PossibleSymbols = {"f"}},
			                    		new ComplexFeature("g"),
			                    		new SymbolicFeature("h") {PossibleSymbols = {"j"}}
			                    	};

			FeatureStruct featStruct1 = FeatureStruct.New(featureSystem)
				.Feature("a").EqualTo(a => a
					.Feature("b").EqualTo("c"))
				.Feature("d").EqualTo(1, d => d
					.Feature("e").EqualTo("f"))
				.Feature("g").ReferringTo(1).Value;

			FeatureStruct featStruct2 = FeatureStruct.New(featureSystem)
				.Feature("a").EqualTo(1, a => a
					.Feature("b").EqualTo("c"))
				.Feature("d").ReferringTo(1)
				.Feature("g").EqualTo(g => g
					.Feature("h").EqualTo("j")).Value;

			FeatureStruct result;
			Assert.IsTrue(featStruct1.Unify(featStruct2, out result));

			featureSystem = new FeatureSystem
			                	{
			                		new ComplexFeature("a"),
									new SymbolicFeature("b") {PossibleSymbols = {"c"}},
									new ComplexFeature("d"),
									new SymbolicFeature("e") {PossibleSymbols = {"f"}},
									new ComplexFeature("g"),
									new SymbolicFeature("h") {PossibleSymbols = {"j"}},
									new ComplexFeature("i")
			                	};

			featStruct1 = FeatureStruct.New(featureSystem)
				.Feature("a").EqualTo(1, a => a
					.Feature("b").EqualTo("c"))
				.Feature("d").EqualTo(2, d => d
					.Feature("e").EqualTo("f"))
				.Feature("g").ReferringTo(2)
				.Feature("i").ReferringTo(1).Value;

			featStruct2 = FeatureStruct.New(featureSystem)
				.Feature("a").EqualTo(1, a => a
					.Feature("b").EqualTo("c"))
				.Feature("d").EqualTo(g => g
					.Feature("h").EqualTo("j"))
				.Feature("g").ReferringTo(1).Value;

			Assert.That(featStruct1.Unify(featStruct2, out result), Is.True);
			Assert.That(result, Is.EqualTo(FeatureStruct.New(featureSystem)
				.Feature("a").EqualTo(1, a => a
					.Feature("b").EqualTo("c")
					.Feature("e").EqualTo("f")
					.Feature("h").EqualTo("j"))
				.Feature("d").ReferringTo(1)
				.Feature("g").ReferringTo(1)
				.Feature("i").ReferringTo(1).Value).Using((IEqualityComparer<FeatureStruct>) FreezableEqualityComparer<FeatureStruct>.Instance));
		}

		[Test]
		public void IsUnifiable()
		{
			Func<FeatureStruct, FeatureStruct, bool> resultsSelector = (fs1, fs2) => fs1.IsUnifiable(fs2);
			Func<FeatureStruct, FeatureStruct, VariableBindings, bool> varResultsSelector = (fs1, fs2, varBindings) => fs1.IsUnifiable(fs2, varBindings);
			TestBinaryOperation(EqualityComparer<bool>.Default, resultsSelector, varResultsSelector,
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
			TestBinaryOperation(FreezableEqualityComparer<FeatureStruct>.Instance, resultsSelector, varResultsSelector,
				// simple
				featSys => FeatureStruct.New(featSys).Symbol("a2").Symbol("b1").Symbol("c2").Value,
				featSys => FeatureStruct.New(featSys).Symbol("a2").Symbol("b1").Symbol("c2").Value,

				// complex
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(cx1 => cx1.Symbol("a2"))
					.Feature("cx2").EqualTo(cx2 => cx2.Symbol("b1"))
					.Feature("cx3").EqualTo(cx2 => cx2.Symbol("c2"))
					.Feature("cx4").EqualTo(cx4 => cx4.Symbol("d2", "d3")).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(cx1 => cx1.Symbol("a2"))
					.Feature("cx2").EqualTo(cx2 => cx2.Symbol("b1"))
					.Feature("cx3").EqualTo(cx2 => cx2.Symbol("c2"))
					.Feature("cx4").EqualTo(cx4 => cx4.Symbol("d1", "d2")).Value,

				// re-entrant
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(1, cx1 => cx1.Symbol("a2"))
					.Feature("cx2").ReferringTo(1).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx2").EqualTo(1, cx2 => cx2.Symbol("a1", "a3"))
					.Feature("cx1").ReferringTo(1).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx2").EqualTo(cx2 => cx2.Symbol(1, "a1", "a3"))
					.Feature("cx1").EqualTo(cx1 => cx1.Feature("a").ReferringTo(1)).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(1, cx1 => cx1.Symbol("a1", "a2"))
					.Feature("cx2").ReferringTo(1).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(1, cx1 => cx1.Symbol("a2", "a3", "a4"))
					.Feature("cx2").ReferringTo(1).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx2").EqualTo(cx2 => cx2.Symbol(1, "a2", "a3", "a4"))
					.Feature("cx1").EqualTo(cx1 => cx1.Feature("a").ReferringTo(1)).Value,

				// cyclic
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(1, cx1 => cx1
					.Symbol("a2").Symbol("b1").Feature("cx2").ReferringTo(1)).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(1, cx1 => cx1
					.Symbol("a2").Symbol("b2").Feature("cx2").ReferringTo(1)).Value,
				// fs1 is not re-entrant and fs2 is re-entrant on cx2, so fs2 wins out and replaces fs1's value for cx2
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(1, cx1 => cx1
					.Symbol("a2").Symbol("b1").Feature("cx2").ReferringTo(1)).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(1, cx1 => cx1
					.Symbol("a2").Symbol("b1").Symbol("c2").Feature("cx2").ReferringTo(1)).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(1, cx1 => cx1
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
			
			TestBinaryOperation(FreezableEqualityComparer<FeatureStruct>.Instance, resultsSelector, varResultsSelector,
				// simple
				featSys => FeatureStruct.New(featSys).Symbol("a1", "a2").Value,
				featSys => FeatureStruct.New(featSys).Symbol("a1", "a2").Symbol("c2").Value,

				// complex
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(cx1 => cx1.Symbol("a1", "a2")).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(cx1 => cx1.Symbol("a1", "a2"))
					.Feature("cx4").EqualTo(cx4 => cx4.Symbol("d1", "d2")).Value,

				// re-entrant
				featSys => FeatureStruct.New(featSys).Feature("cx2").EqualTo(cx1 => cx1.Symbol("a1", "a2")).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(cx2 => cx2.Symbol("a1", "a2", "a3")).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(cx2 => cx2.Symbol("a1", "a2", "a3")).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(1, cx1 => cx1.Symbol("a1", "a2"))
					.Feature("cx2").ReferringTo(1).Value,
				featSys => FeatureStruct.New().Value,
				featSys => FeatureStruct.New().Value,

				// cyclic
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(cx1 => cx1.Symbol("a1", "a2")).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(cx1 => cx1.Symbol("a1", "a2")).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(cx1 => cx1.Symbol("a1", "a2")).Value,
				// fs1 is not re-entrant, so the result is also not re-entrant
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(cx1 => cx1.Symbol("a1", "a2")
					.Feature("cx2").EqualTo(cx2 => cx2.Symbol("c1", "c2"))).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(1, cx1 => cx1.Symbol("a1", "a2").Symbol("c1", "c2")
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

			TestBinaryOperation(FreezableEqualityComparer<FeatureStruct>.Instance, resultsSelector, varResultsSelector,
				// simple
				featSys => FeatureStruct.New(featSys).Symbol("a1").Symbol("b1").Value,
				featSys => FeatureStruct.New(featSys).Symbol("a1").Symbol("b1").Value,

				// complex
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(cx1 => cx1.Symbol("a1"))
					.Feature("cx2").EqualTo(cx2 => cx2.Symbol("b1"))
					.Feature("cx4").EqualTo(cx4 => cx4.Symbol("d1")).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(cx1 => cx1.Symbol("a1"))
					.Feature("cx2").EqualTo(cx2 => cx2.Symbol("b1")).Value,

				// re-entrant
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(1, cx1 => cx1.Symbol("a1")).Feature("cx2")
					.ReferringTo(1).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(cx1 => cx1.Symbol("a2")).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(cx1 => cx1.Symbol("a2")).Value,
				featSys => FeatureStruct.New().Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(1, cx1 => cx1.Symbol("a1"))
					.Feature("cx2").ReferringTo(1).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(cx1 => cx1.Symbol(1, "a1"))
					.Feature("cx2").EqualTo(cx2 => cx2.Feature("a").ReferringTo(1)).Value,

				// cyclic
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(cx1 => cx1.Symbol("a1").Symbol("b1")).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(1, cx1 => cx1.Symbol("a1")
					.Feature("cx2").ReferringTo(1)).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(cx1 => cx1
					.Symbol("a1").Symbol("b1").Feature("cx2").EqualTo(cx2 => cx2.Symbol("c1"))).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(cx1 => cx1
					.Symbol("a1").Symbol("b1").Feature("cx2").EqualTo(cx2 => cx2.Symbol("c1"))).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(1, cx1 => cx1
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
					.With(disj => disj.Feature("cx1").EqualTo(cx1 => cx1.Symbol("a2", "a3")))
					.Or(disj => disj.Feature("cx2").EqualTo(cx2 => cx2.Symbol("b2", "b3")))
					.Or(disj => disj.Feature("cx3").EqualTo(cx3 => cx3.Symbol("d2", "d3")))).Value,
				featSys => FeatureStruct.New(featSys).And(disjunction => disjunction
					.With(disj => disj.Feature("cx1").EqualTo(cx1 => cx1.Symbol("a3")))
					.Or(disj => disj.Feature("cx2").EqualTo(cx2 => cx2.Symbol("b2", "b3")))
					.Or(disj => disj.Feature("cx3").EqualTo(cx3 => cx3.And(cx3Disjunction => cx3Disjunction
						.With(cx3Disj => cx3Disj.Symbol("c1")).Or(cx3Disj => cx3Disj.Symbol("d2", "d3")))))).Value,

				// re-entrant
				featSys => FeatureStruct.New(featSys).And(disjunction => disjunction
					.With(disj => disj.Feature("cx1").EqualTo(1, cx1 => cx1.Symbol("a2", "a3", "a4")))
					.Or(disj => disj.Feature("cx2").ReferringTo(1))).Value,
				featSys => FeatureStruct.New(featSys).And(disjunction => disjunction
					.With(disj => disj.Feature("cx1").EqualTo(1, cx1 => cx1.Symbol("a1")))
					.Or(disj => disj.Feature("cx2").ReferringTo(1))).Value,
				featSys => FeatureStruct.New(featSys).And(disjunction => disjunction
					.With(disj => disj.Feature("cx1").EqualTo(cx1 => cx1.Symbol(1, "a2", "a4")))
					.Or(disj => disj.Feature("cx2").EqualTo(cx2 => cx2.Feature("a").ReferringTo(1)))).Value,

				// cyclic
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(1, cx1 => cx1.And(disjunction => disjunction
					.With(disj => disj.Symbol("a1", "a3"))
					.Or(disj => disj.Feature("cx2").ReferringTo(1)))).Value,
				featSys => FeatureStruct.New(featSys).Feature("cx1").EqualTo(1, cx1 => cx1.And(disjunction => disjunction
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
						.Or(disj => disj.Symbol("feat5-").Feature("cfeat2").EqualTo(cfeat2 => cfeat2.And(cfeat2Disjunction => cfeat2Disjunction
							.With(cfeat2Disj => cfeat2Disj.Symbol("feat9-"))
							.Or(cfeat2Disj => cfeat2Disj.Symbol("feat10-")))))
						.Or(disj => disj.Feature("cfeat1").EqualTo(cfeat1 => cfeat1.And(cfeat1Disjunction => cfeat1Disjunction
							.With(cfeat1Disj => cfeat1Disj.Symbol("feat7-"))
							.Or(cfeat1Disj => cfeat1Disj.Symbol("feat8-")))))
						.Or(disj => disj.Symbol("feat1-"))
						.Or(disj => disj.Symbol("feat2-"))).Value);
		}

		private void TestUnaryOperation<TResult>(Func<FeatureStruct, TResult> resultsSelector, params Func<FeatureSystem, TResult>[] expectedSelectors)
		{
			// simple
			var featSys = new FeatureSystem
							{
								new SymbolicFeature("a") {PossibleSymbols = {"a1", "a2", "a3"}},
								new SymbolicFeature("b") {PossibleSymbols = {"b1", "b2", "b3"}},
								new SymbolicFeature("c") {PossibleSymbols = {"c1", "c2", "c3"}}
							};

			FeatureStruct fs = FeatureStruct.New(featSys).Symbol("a1").Symbol("b1").Value;
			Assert.That(resultsSelector(fs), Is.EqualTo(expectedSelectors[0](featSys)).Using((IEqualityComparer<FeatureStruct>) FreezableEqualityComparer<FeatureStruct>.Instance));

			fs = FeatureStruct.New(featSys).Symbol("a1", "a2").Symbol("b1").Symbol("c2").Value;
			Assert.That(resultsSelector(fs), Is.EqualTo(expectedSelectors[1](featSys)).Using((IEqualityComparer<FeatureStruct>) FreezableEqualityComparer<FeatureStruct>.Instance));

			// complex
			featSys = new FeatureSystem
			          	{
			          		new ComplexFeature("cx1"),
			          		new SymbolicFeature("a") {PossibleSymbols = {"a1", "a2", "a3"}},
			          		new ComplexFeature("cx2"),
			          		new SymbolicFeature("b") {PossibleSymbols = {"b1", "b2", "b3"}},
			          		new ComplexFeature("cx3"),
			          		new SymbolicFeature("c") {PossibleSymbols = {"c1", "c2", "c3"}},
			          		new SymbolicFeature("d") {PossibleSymbols = {"d1", "d2", "d3"}}
			          	};

			fs = FeatureStruct.New(featSys).Feature("cx1").EqualTo(cx1 => cx1.Symbol("a1"))
				.Feature("cx2").EqualTo(cx2 => cx2.Symbol("b1"))
				.Feature("cx3").EqualTo(cx3 => cx3.Symbol("d1")).Value;
			Assert.That(resultsSelector(fs), Is.EqualTo(expectedSelectors[2](featSys)).Using((IEqualityComparer<FeatureStruct>) FreezableEqualityComparer<FeatureStruct>.Instance));

			fs = FeatureStruct.New(featSys).Feature("cx1").EqualTo(cx1 => cx1.Symbol("a1", "a2"))
				.Feature("cx2").EqualTo(cx2 => cx2.Symbol("b1"))
				.Feature("cx3").EqualTo(cx3 => cx3.Symbol("c2", "c3").Symbol("d1")).Value;
			Assert.That(resultsSelector(fs), Is.EqualTo(expectedSelectors[3](featSys)).Using((IEqualityComparer<FeatureStruct>) FreezableEqualityComparer<FeatureStruct>.Instance));

			// re-entrant
			featSys = new FeatureSystem
			          	{
			          		new ComplexFeature("cx1"),
			          		new SymbolicFeature("a") {PossibleSymbols = {"a1", "a2", "a3", "a4"}},
			          		new ComplexFeature("cx2")
			          	};

			fs = FeatureStruct.New(featSys).Feature("cx1").EqualTo(1, cx1 => cx1.Symbol("a1"))
				.Feature("cx2").ReferringTo(1).Value;
			Assert.That(resultsSelector(fs), Is.EqualTo(expectedSelectors[4](featSys)).Using((IEqualityComparer<FeatureStruct>) FreezableEqualityComparer<FeatureStruct>.Instance));

			fs = FeatureStruct.New(featSys).Feature("cx2").EqualTo(1, cx2 => cx2.Symbol("a2", "a3", "a4"))
				.Feature("cx1").ReferringTo(1).Value;
			Assert.That(resultsSelector(fs), Is.EqualTo(expectedSelectors[5](featSys)).Using((IEqualityComparer<FeatureStruct>) FreezableEqualityComparer<FeatureStruct>.Instance));

			fs = FeatureStruct.New(featSys).Feature("cx2").EqualTo(cx2 => cx2.Symbol(1, "a1", "a3"))
				.Feature("cx1").EqualTo(cx1 => cx1.Feature("a").ReferringTo(1)).Value;
			Assert.That(resultsSelector(fs), Is.EqualTo(expectedSelectors[6](featSys)).Using((IEqualityComparer<FeatureStruct>) FreezableEqualityComparer<FeatureStruct>.Instance));

			// cyclic
			featSys = new FeatureSystem
			          	{
			          		new ComplexFeature("cx1"),
			          		new SymbolicFeature("a") {PossibleSymbols = {"a1", "a2", "a3"}},
			          		new SymbolicFeature("b") {PossibleSymbols = {"b1", "b2", "b3"}},
			          		new SymbolicFeature("c") {PossibleSymbols = {"c1", "c2", "c3"}},
			          		new ComplexFeature("cx2")
			          	};

			fs = FeatureStruct.New(featSys).Feature("cx1").EqualTo(1, cx1 => cx1
				.Symbol("a2").Feature("cx2").ReferringTo(1)).Value;
			Assert.That(resultsSelector(fs), Is.EqualTo(expectedSelectors[7](featSys)).Using((IEqualityComparer<FeatureStruct>) FreezableEqualityComparer<FeatureStruct>.Instance));

			fs = FeatureStruct.New(featSys).Feature("cx1").EqualTo(1, cx1 => cx1
				.Symbol("a1", "a2").Feature("cx2").ReferringTo(1)).Value;
			Assert.That(resultsSelector(fs), Is.EqualTo(expectedSelectors[8](featSys)).Using((IEqualityComparer<FeatureStruct>) FreezableEqualityComparer<FeatureStruct>.Instance));

			// variable
			featSys = new FeatureSystem
			          	{
			          		new SymbolicFeature("a") {PossibleSymbols = {{"a+", "+"}, {"a-", "-"}}},
			          		new SymbolicFeature("b") {PossibleSymbols = {{"b+", "+"}, {"b-", "-"}}}
			          	};

			fs = FeatureStruct.New(featSys)
				.Feature("a").EqualToVariable("var1")
				.Symbol("b-").Value;

			Assert.That(resultsSelector(fs), Is.EqualTo(expectedSelectors[9](featSys)).Using((IEqualityComparer<FeatureStruct>) FreezableEqualityComparer<FeatureStruct>.Instance));

			fs = FeatureStruct.New(featSys)
				.Feature("a").Not.EqualToVariable("var1")
				.Symbol("b-").Value;

			Assert.That(resultsSelector(fs), Is.EqualTo(expectedSelectors[10](featSys)).Using((IEqualityComparer<FeatureStruct>) FreezableEqualityComparer<FeatureStruct>.Instance));

			// disjunctive
			featSys = new FeatureSystem
			          	{
			          		new SymbolicFeature("feat1") {PossibleSymbols = {"feat1+", "feat1-"}},
			          		new SymbolicFeature("feat2") {PossibleSymbols = {"feat2+", "feat2-"}},
			          		new SymbolicFeature("feat3") {PossibleSymbols = {"feat3+", "feat3-"}},
			          		new SymbolicFeature("feat4") {PossibleSymbols = {"feat4+", "feat4-"}},
			          		new SymbolicFeature("feat5") {PossibleSymbols = {"feat5+", "feat5-"}},
			          		new SymbolicFeature("feat6") {PossibleSymbols = {"feat6+", "feat6-"}},
			          		new SymbolicFeature("feat7") {PossibleSymbols = {"feat7+", "feat7-"}},
			          		new SymbolicFeature("feat8") {PossibleSymbols = {"feat8+", "feat8-"}},
			          		new SymbolicFeature("feat9") {PossibleSymbols = {"feat9+", "feat9-"}},
			          		new SymbolicFeature("feat10") {PossibleSymbols = {"feat10+", "feat10-"}},
							new ComplexFeature("cfeat1"),
							new ComplexFeature("cfeat2")
			          	};

			fs = FeatureStruct.New(featSys)
				.And(disjunction => disjunction
					.With(disj => disj.Symbol("feat1+"))
					.Or(disj => disj.Symbol("feat2+"))).Value;
			Assert.That(resultsSelector(fs), Is.EqualTo(expectedSelectors[11](featSys)).Using((IEqualityComparer<FeatureStruct>) FreezableEqualityComparer<FeatureStruct>.Instance));

			fs = FeatureStruct.New(featSys)
				.And(disjunction => disjunction
					.With(disj => disj.Symbol("feat1+").Symbol("feat2+"))
					.Or(disj => disj.Symbol("feat3+").Symbol("feat4+"))).Value;
			Assert.That(resultsSelector(fs), Is.EqualTo(expectedSelectors[12](featSys)).Using((IEqualityComparer<FeatureStruct>) FreezableEqualityComparer<FeatureStruct>.Instance));

			fs = FeatureStruct.New(featSys)
				.Symbol("feat1+")
				.Symbol("feat2+")
				.And(disjunction => disjunction
					.With(disj => disj.Symbol("feat3+"))
					.Or(disj => disj.Symbol("feat4+")))
				.And(disjunction => disjunction
					.With(disj => disj.Symbol("feat5+"))
					.Or(disj => disj.Symbol("feat6+"))).Value;
			Assert.That(resultsSelector(fs), Is.EqualTo(expectedSelectors[13](featSys)).Using((IEqualityComparer<FeatureStruct>) FreezableEqualityComparer<FeatureStruct>.Instance));

			fs = FeatureStruct.New(featSys)
				.Feature("cfeat1").EqualTo(cfeat1 => cfeat1.Symbol("feat7+").Symbol("feat8+"))
				.Symbol("feat1+")
				.Symbol("feat2+")
				.And(disjunction => disjunction
					.With(disj => disj.Symbol("feat3+"))
					.Or(disj => disj.Symbol("feat4+")))
				.And(disjunction => disjunction
					.With(disj => disj.Feature("cfeat2").EqualTo(cfeat2 => cfeat2.Symbol("feat9+").Symbol("feat10+")))
					.Or(disj => disj.Symbol("feat5+"))).Value;
			Assert.That(resultsSelector(fs), Is.EqualTo(expectedSelectors[14](featSys)).Using((IEqualityComparer<FeatureStruct>) FreezableEqualityComparer<FeatureStruct>.Instance));
		}

		private void TestBinaryOperation<TResult>(IEqualityComparer<TResult> comparer, Func<FeatureStruct, FeatureStruct, TResult> resultsSelector,
			Func<FeatureStruct, FeatureStruct, VariableBindings, TResult> varResultsSelector, params Func<FeatureSystem, TResult>[] expectedSelectors)
		{
			// simple
			var featSys = new FeatureSystem
			              	{
			              		new SymbolicFeature("a") {PossibleSymbols = {"a1", "a2", "a3"}},
			              		new SymbolicFeature("b") {PossibleSymbols = {"b1", "b2", "b3"}},
			              		new SymbolicFeature("c") {PossibleSymbols = {"c1", "c2", "c3"}}
			              	};

			FeatureStruct fs1 = FeatureStruct.NewMutable(featSys).Symbol("a1").Symbol("b1").Value;
			FeatureStruct fs2 = FeatureStruct.NewMutable(featSys).Symbol("a2").Symbol("c2").Value;
			Assert.That(resultsSelector(fs1, fs2), Is.EqualTo(expectedSelectors[0](featSys)).Using(comparer));

			fs1 = FeatureStruct.NewMutable(featSys).Symbol("a1", "a2").Symbol("b1").Symbol("c2").Value;
			fs2 = FeatureStruct.NewMutable(featSys).Symbol("a2").Symbol("c2").Value;
			Assert.That(resultsSelector(fs1, fs2), Is.EqualTo(expectedSelectors[1](featSys)).Using(comparer));

			// complex
			featSys = new FeatureSystem
			          	{
			          		new ComplexFeature("cx1"),
			          		new SymbolicFeature("a") {PossibleSymbols = {"a1", "a2", "a3"}},
			          		new ComplexFeature("cx2"),
			          		new SymbolicFeature("b") {PossibleSymbols = {"b1", "b2", "b3"}},
			          		new ComplexFeature("cx3"),
			          		new SymbolicFeature("c") {PossibleSymbols = {"c1", "c2", "c3"}},
			          		new ComplexFeature("cx4"),
			          		new SymbolicFeature("d") {PossibleSymbols = {"d1", "d2", "d3"}}
			          	};

			fs1 = FeatureStruct.NewMutable(featSys).Feature("cx1").EqualTo(cx1 => cx1.Symbol("a1"))
				.Feature("cx2").EqualTo(cx2 => cx2.Symbol("b1"))
				.Feature("cx4").EqualTo(cx4 => cx4.Symbol("d1")).Value;
			fs2 = FeatureStruct.NewMutable(featSys).Feature("cx1").EqualTo(cx1 => cx1.Symbol("a2"))
				.Feature("cx3").EqualTo(cx2 => cx2.Symbol("c2"))
				.Feature("cx4").EqualTo(cx4 => cx4.Symbol("d2", "d3")).Value;
			Assert.That(resultsSelector(fs1, fs2), Is.EqualTo(expectedSelectors[2](featSys)).Using(comparer));

			fs1 = FeatureStruct.NewMutable(featSys).Feature("cx1").EqualTo(cx1 => cx1.Symbol("a1", "a2"))
				.Feature("cx2").EqualTo(cx2 => cx2.Symbol("b1"))
				.Feature("cx4").EqualTo(cx4 => cx4.Symbol("d1")).Value;
			fs2 = FeatureStruct.NewMutable(featSys).Feature("cx1").EqualTo(cx1 => cx1.Symbol("a2"))
				.Feature("cx3").EqualTo(cx2 => cx2.Symbol("c2"))
				.Feature("cx4").EqualTo(cx4 => cx4.Symbol("d1", "d2")).Value;
			Assert.That(resultsSelector(fs1, fs2), Is.EqualTo(expectedSelectors[3](featSys)).Using(comparer));

			// re-entrant
			featSys = new FeatureSystem
			          	{
			          		new ComplexFeature("cx1"),
			          		new SymbolicFeature("a") {PossibleSymbols = {"a1", "a2", "a3", "a4"}},
			          		new ComplexFeature("cx2")
			          	};

			fs1 = FeatureStruct.NewMutable(featSys).Feature("cx1").EqualTo(1, cx1 => cx1.Symbol("a1"))
				.Feature("cx2").ReferringTo(1).Value;
			fs2 = FeatureStruct.NewMutable(featSys).Feature("cx2").EqualTo(cx2 => cx2.Symbol("a2")).Value;
			Assert.That(resultsSelector(fs1, fs2), Is.EqualTo(expectedSelectors[4](featSys)).Using(comparer));

			fs1 = FeatureStruct.NewMutable(featSys).Feature("cx1").EqualTo(cx1 => cx1.Symbol("a1", "a2")).Value;
			fs2 = FeatureStruct.NewMutable(featSys).Feature("cx2").EqualTo(1, cx2 => cx2.Symbol("a1", "a3"))
				.Feature("cx1").ReferringTo(1).Value;
			Assert.That(resultsSelector(fs1, fs2), Is.EqualTo(expectedSelectors[5](featSys)).Using(comparer));

			fs1 = FeatureStruct.NewMutable(featSys).Feature("cx1").EqualTo(cx1 => cx1.Symbol("a1", "a2")).Value;
			fs2 = FeatureStruct.NewMutable(featSys).Feature("cx2").EqualTo(cx2 => cx2.Symbol(1, "a1", "a3"))
				.Feature("cx1").EqualTo(cx1 => cx1.Feature("a").ReferringTo(1)).Value;
			Assert.That(resultsSelector(fs1, fs2), Is.EqualTo(expectedSelectors[6](featSys)).Using(comparer));

			fs1 = FeatureStruct.NewMutable(featSys).Feature("cx1").EqualTo(1, cx1 => cx1.Symbol("a1"))
				.Feature("cx2").ReferringTo(1).Value;
			fs2 = FeatureStruct.NewMutable(featSys).Feature("cx2").EqualTo(1, cx2 => cx2.Symbol("a1", "a2"))
				.Feature("cx1").ReferringTo(1).Value;
			Assert.That(resultsSelector(fs1, fs2), Is.EqualTo(expectedSelectors[7](featSys)).Using(comparer));

			fs1 = FeatureStruct.NewMutable(featSys).Feature("cx1").EqualTo(1, cx1 => cx1.Symbol("a1", "a2"))
				.Feature("cx2").ReferringTo(1).Value;
			fs2 = FeatureStruct.NewMutable(featSys).Feature("cx2").EqualTo(1, cx2 => cx2.Symbol("a2", "a3", "a4"))
				.Feature("cx1").ReferringTo(1).Value;
			Assert.That(resultsSelector(fs1, fs2), Is.EqualTo(expectedSelectors[8](featSys)).Using(comparer));

			fs1 = FeatureStruct.NewMutable(featSys).Feature("cx1").EqualTo(cx1 => cx1.Symbol(1, "a1", "a2"))
				.Feature("cx2").EqualTo(cx2 => cx2.Feature("a").ReferringTo(1)).Value;
			fs2 = FeatureStruct.NewMutable(featSys).Feature("cx2").EqualTo(cx2 => cx2.Symbol(1, "a2", "a3", "a4"))
				.Feature("cx1").EqualTo(cx1 => cx1.Feature("a").ReferringTo(1)).Value;
			Assert.That(resultsSelector(fs1, fs2), Is.EqualTo(expectedSelectors[9](featSys)).Using(comparer));

			// cyclic
			featSys = new FeatureSystem
			          	{
			          		new ComplexFeature("cx1"),
			          		new SymbolicFeature("a") {PossibleSymbols = {"a1", "a2", "a3"}},
			          		new SymbolicFeature("b") {PossibleSymbols = {"b1", "b2", "b3"}},
			          		new SymbolicFeature("c") {PossibleSymbols = {"c1", "c2", "c3"}},
			          		new ComplexFeature("cx2")
			          	};

			fs1 = FeatureStruct.NewMutable(featSys).Feature("cx1").EqualTo(cx1 => cx1
				.Symbol("a1").Symbol("b1")).Value;
			fs2 = FeatureStruct.NewMutable(featSys).Feature("cx1").EqualTo(1, cx1 => cx1
				.Symbol("a2").Feature("cx2").ReferringTo(1)).Value;
			Assert.That(resultsSelector(fs1, fs2), Is.EqualTo(expectedSelectors[10](featSys)).Using(comparer));

			fs1 = FeatureStruct.NewMutable(featSys).Feature("cx1").EqualTo(1, cx1 => cx1
				.Symbol("a1", "a2").Feature("cx2").ReferringTo(1)).Value;
			fs2 = FeatureStruct.NewMutable(featSys).Feature("cx1").EqualTo(cx1 => cx1
				.Symbol("a2").Symbol("b2")).Value;
			Assert.That(resultsSelector(fs1, fs2), Is.EqualTo(expectedSelectors[11](featSys)).Using(comparer));

			fs1 = FeatureStruct.NewMutable(featSys).Feature("cx1").EqualTo(cx1 => cx1
				.Symbol("a1", "a2").Symbol("b1").Feature("cx2").EqualTo(cx2 => cx2.Symbol("c1"))).Value;
			fs2 = FeatureStruct.NewMutable(featSys).Feature("cx1").EqualTo(1, cx1 => cx1
				.Symbol("a2").Feature("cx2").ReferringTo(1)).Value;
			Assert.That(resultsSelector(fs1, fs2), Is.EqualTo(expectedSelectors[12](featSys)).Using(comparer));

			fs1 = FeatureStruct.NewMutable(featSys).Feature("cx1").EqualTo(cx1 => cx1
				.Symbol("a1").Symbol("b1").Feature("cx2").EqualTo(cx2 => cx2.Symbol("c1", "c2"))).Value;
			fs2 = FeatureStruct.NewMutable(featSys).Feature("cx1").EqualTo(1, cx1 => cx1
				.Symbol("a2").Symbol("c2").Feature("cx2").ReferringTo(1)).Value;
			Assert.That(resultsSelector(fs1, fs2), Is.EqualTo(expectedSelectors[13](featSys)).Using(comparer));

			fs1 = FeatureStruct.NewMutable(featSys).Feature("cx1").EqualTo(1, cx1 => cx1
				.Symbol("a1").Symbol("b1").Symbol("c1").Feature("cx2").ReferringTo(1)).Value;
			fs2 = FeatureStruct.NewMutable(featSys).Feature("cx1").EqualTo(1, cx1 => cx1
				.Symbol("a2").Symbol("c2").Feature("cx2").ReferringTo(1)).Value;
			Assert.That(resultsSelector(fs1, fs2), Is.EqualTo(expectedSelectors[14](featSys)).Using(comparer));

			// variable
			featSys = new FeatureSystem
			          	{
			          		new SymbolicFeature("a") {PossibleSymbols = {{"a+", "+"}, {"a-", "-"}}},
			          		new SymbolicFeature("b") {PossibleSymbols = {{"b+", "+"}, {"b-", "-"}}}
			          	};

			fs1 = FeatureStruct.NewMutable(featSys)
				.Feature("a").EqualToVariable("var1")
				.Symbol("b-").Value;

			fs2 = FeatureStruct.NewMutable(featSys)
				.Feature("a").Not.EqualToVariable("var1")
				.Symbol("b-").Value;

			Assert.That(resultsSelector(fs1, fs2), Is.EqualTo(expectedSelectors[15](featSys)).Using(comparer));

			fs1 = FeatureStruct.NewMutable(featSys)
				.Feature("a").EqualToVariable("var1")
				.Symbol("b-").Value;

			fs2 = FeatureStruct.NewMutable(featSys)
				.Symbol("a+")
				.Symbol("b-").Value;

			Assert.That(resultsSelector(fs1, fs2), Is.EqualTo(expectedSelectors[16](featSys)).Using(comparer));

			fs1 = FeatureStruct.NewMutable(featSys)
				.Symbol("a+")
				.Symbol("b-").Value;

			fs2 = FeatureStruct.NewMutable(featSys)
				.Feature("a").Not.EqualToVariable("var1")
				.Symbol("b-").Value;

			Assert.That(resultsSelector(fs1, fs2), Is.EqualTo(expectedSelectors[17](featSys)).Using(comparer));

			fs1 = FeatureStruct.NewMutable(featSys)
				.Symbol("a+")
				.Symbol("b-").Value;

			fs2 = FeatureStruct.NewMutable(featSys)
				.Feature("a").Not.EqualToVariable("var1")
				.Symbol("b-").Value;

			var varBindings = new VariableBindings();
			varBindings["var1"] = new SymbolicFeatureValue(featSys.GetSymbol("a-"));
			Assert.That(varResultsSelector(fs1, fs2, varBindings), Is.EqualTo(expectedSelectors[18](featSys)).Using(comparer));

			fs1 = FeatureStruct.NewMutable(featSys)
				.Symbol("a+")
				.Symbol("b-").Value;

			fs2 = FeatureStruct.NewMutable(featSys)
				.Feature("a").Not.EqualToVariable("var1")
				.Symbol("b-").Value;

			varBindings = new VariableBindings();
			varBindings["var1"] = new SymbolicFeatureValue(featSys.GetSymbol("a+"));
			Assert.That(varResultsSelector(fs1, fs2, varBindings), Is.EqualTo(expectedSelectors[19](featSys)).Using(comparer));
		}
	}
}
