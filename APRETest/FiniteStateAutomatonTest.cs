using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.APRE.Fsa;

namespace SIL.APRE.Test
{
	public class TestCondition : IArcCondition<int, object>
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

		public IArcCondition<int, object> Negation()
		{
			return new TestCondition(_symbols.Select(symbol => Tuple.Create(symbol.Item1, !symbol.Item2)));
		}

		public IArcCondition<int, object> Conjunction(IArcCondition<int, object> cond)
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
			var fsa = new FiniteStateAutomaton<int, object>(Direction.LeftToRight, null, null);
			State<int, object> q1 = fsa.CreateState(true);
			fsa.StartState.AddArc(new Arc<int, object>(new TestCondition("pi1"), q1));
			State<int, object> q2 = fsa.CreateState(true);
			fsa.StartState.AddArc(new Arc<int, object>(new TestCondition("pi1"), q2));
			fsa.StartState.AddArc(new Arc<int, object>(new TestCondition("pi2"), q2));
			State<int, object> q3 = fsa.CreateState(true);
			fsa.StartState.AddArc(new Arc<int, object>(new TestCondition("pi2"), q3));
			fsa.StartState.AddArc(new Arc<int, object>(new TestCondition("pi3"), q3));
			State<int, object> q4 = fsa.CreateState(true);
			fsa.StartState.AddArc(new Arc<int, object>(new TestCondition("pi2"), q4));
			State<int, object> q5 = fsa.CreateState(true);
			fsa.StartState.AddArc(new Arc<int, object>(new TestCondition("pi3"), q5));
			fsa.Determinize();

			Assert.AreEqual(8, fsa.StartState.Arcs.Count());
			Assert.IsTrue(fsa.StartState.Arcs.Any(tran => tran.Condition.Equals(
				new TestCondition(new [] { Tuple.Create("pi1", false), Tuple.Create("pi2", false), Tuple.Create("pi3", false)} ))));
			Assert.IsTrue(fsa.StartState.Arcs.Any(tran => tran.Condition.Equals(
				new TestCondition(new[] { Tuple.Create("pi1", false), Tuple.Create("pi2", true), Tuple.Create("pi3", false) }))));
		}

		//[Test]
		//public void Intersect()
		//{
		//    var insertionFsa = new FiniteStateAutomaton<int, FeatureStructure>(Direction.LeftToRight, null, null);
		//    State<int, FeatureStructure> startState = insertionFsa.StartState;
		//    State<int, FeatureStructure> endState = insertionFsa.CreateState();
		//    startState.AddArc(new Arc<int, FeatureStructure>(new AnnotationArcCondition<int>(new[] {"s", "z"}, false), endState));
		//    startState = endState;
		//    endState = insertionFsa.CreateState();
		//    startState.AddArc(new Arc<int, FeatureStructure>(new AnnotationArcCondition<int>(new[] {"+"}, false), endState));
		//    startState = endState;
		//    endState = insertionFsa.CreateState(true);
		//    startState.AddArc(new Arc<int, FeatureStructure>(new AnnotationArcCondition<int>(new[] {"z"}, false), endState));

		//    var devoiceFsa = new FiniteStateAutomaton<int, FeatureStructure>(Direction.LeftToRight, null, null);
		//    startState = devoiceFsa.StartState;
		//    endState = devoiceFsa.CreateState();
		//    startState.AddArc(new Arc<int, FeatureStructure>(new AnnotationArcCondition<int>(new[] {"z", "a", "ɨ"}, false), endState));
		//    startState = endState;
		//    endState = insertionFsa.CreateState();
		//    startState.AddArc(new Arc<int, FeatureStructure>(new AnnotationArcCondition<int>(new[] {"+"}, false), endState));
		//    startState = endState;
		//    endState = insertionFsa.CreateState(true);
		//    startState.AddArc(new Arc<int, FeatureStructure>(new AnnotationArcCondition<int>(new[] {"z"}, false), endState));

		//    var fsa = insertionFsa.Intersect(devoiceFsa);
		//    var writer = new StreamWriter("c:\\nfa.dot");
		//    fsa.ToGraphViz(writer);
		//    writer.Close();
		//}
	}
}
