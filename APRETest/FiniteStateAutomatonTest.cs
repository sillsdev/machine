using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.APRE.Fsa;

namespace SIL.APRE.Test
{
	public class TestCondition : ITransitionCondition<int, object>
	{
		private readonly HashSet<Tuple<string, bool>> _symbols;

		public TestCondition(string symbol)
		{
			_symbols = new HashSet<Tuple<string, bool>> { Tuple.Create(symbol, false) };
		}

		public TestCondition(IEnumerable<Tuple<string, bool>> symbols)
		{
			_symbols = new HashSet<Tuple<string, bool>>(symbols);
		}

		public bool IsMatch(Annotation<int> ann, ModeType mode, ref object data)
		{
			foreach (Tuple<string, bool> symbol in _symbols)
			{
				FeatureValue fv = ann.FeatureStructure.GetValue(symbol.Item1);
				if (!(symbol.Item2 ? fv == null : fv != null))
					return false;
			}
			return true;
		}

		public ITransitionCondition<int, object> Negation()
		{
			return new TestCondition(_symbols.Select(symbol => Tuple.Create(symbol.Item1, !symbol.Item2)));
		}

		public ITransitionCondition<int, object> Conjunction(ITransitionCondition<int, object> cond)
		{
			var tcond = (TestCondition) cond;
			return new TestCondition(_symbols.Union(tcond._symbols));
		}

		public bool IsSatisfiable
		{
			get { return true; }
		}

		public override bool Equals(object obj)
		{
			var tcond = obj as TestCondition;
			if (tcond == null)
				return false;
			return _symbols.SetEquals(tcond._symbols);
		}

		public override int GetHashCode()
		{
			return _symbols.Aggregate(0, (hash, symbol) => hash ^ symbol.GetHashCode());
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			bool first = true;
			foreach (Tuple<string, bool> symbol in _symbols)
			{
				if (!first)
					sb.Append(" && ");
				if (symbol.Item2)
					sb.Append("!");
				sb.Append(symbol.Item1);
				first = false;
			}
			return sb.ToString();
		}
	}

	[TestFixture]
	public class FiniteStateAutomatonTest
	{
		[Test]
		public void Determinize()
		{
			var featSys = new FeatureSystem();
			var f = new SymbolicFeature("pi1");
			f.AddPossibleSymbol(new FeatureSymbol("pi1+", "+"));
			featSys.AddFeature(f);

			f = new SymbolicFeature("pi2");
			f.AddPossibleSymbol(new FeatureSymbol("pi2+", "+"));
			featSys.AddFeature(f);

			f = new SymbolicFeature("pi3");
			f.AddPossibleSymbol(new FeatureSymbol("pi3+", "+"));

			featSys.AddFeature(f);

			var fsa = new FiniteStateAutomaton<int, object>(Direction.LeftToRight, null, null);
			State<int, object> q1 = fsa.CreateState(true);
			fsa.StartState.AddTransition(new Transition<int, object>(new TestCondition("pi1"), q1));
			State<int, object> q2 = fsa.CreateState(true);
			fsa.StartState.AddTransition(new Transition<int, object>(new TestCondition("pi1"), q2));
			fsa.StartState.AddTransition(new Transition<int, object>(new TestCondition("pi2"), q2));
			State<int, object> q3 = fsa.CreateState(true);
			fsa.StartState.AddTransition(new Transition<int, object>(new TestCondition("pi2"), q3));
			fsa.StartState.AddTransition(new Transition<int, object>(new TestCondition("pi3"), q3));
			State<int, object> q4 = fsa.CreateState(true);
			fsa.StartState.AddTransition(new Transition<int, object>(new TestCondition("pi2"), q4));
			State<int, object> q5 = fsa.CreateState(true);
			fsa.StartState.AddTransition(new Transition<int, object>(new TestCondition("pi3"), q5));
			fsa.Determinize();

			Assert.AreEqual(8, fsa.StartState.Transitions.Count());
			Assert.IsTrue(fsa.StartState.Transitions.Any(tran => tran.Condition.Equals(
				new TestCondition(new [] { Tuple.Create("pi1", false), Tuple.Create("pi2", false), Tuple.Create("pi3", false)} ))));
			Assert.IsTrue(fsa.StartState.Transitions.Any(tran => tran.Condition.Equals(
				new TestCondition(new[] { Tuple.Create("pi1", false), Tuple.Create("pi2", true), Tuple.Create("pi3", false) }))));
		}
	}
}
