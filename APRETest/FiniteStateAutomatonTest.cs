using System.IO;
using System.Linq;
using NUnit.Framework;
using SIL.APRE.Fsa;

namespace SIL.APRE.Test
{
	[TestFixture]
	public class FiniteStateAutomatonTest
	{
		[Test]
		public void Determinize()
		{
			FeatureSystem featSys = FeatureSystem.Build()
				.SymbolicFeature("pi1", pi1 => pi1
				                               	.Symbol("pi1+", "+")
				                               	.Symbol("pi1-", "-"))
				.SymbolicFeature("pi2", pi2 => pi2
				                               	.Symbol("pi2+", "+")
				                               	.Symbol("pi2-", "-"))
				.SymbolicFeature("pi3", pi3 => pi3
				                               	.Symbol("pi3+", "+")
				                               	.Symbol("pi3-", "-"));

			var fsa = new FiniteStateAutomaton<int>(Direction.LeftToRight);
			State<int> q1 = fsa.CreateState(true);
			fsa.StartState.AddArc(new Arc<int>(new ArcCondition<int>(featSys.BuildFS().Symbol("pi1+")), q1));
			State<int> q2 = fsa.CreateState(true);
			fsa.StartState.AddArc(new Arc<int>(new ArcCondition<int>(featSys.BuildFS().Symbol("pi1+")), q2));
			fsa.StartState.AddArc(new Arc<int>(new ArcCondition<int>(featSys.BuildFS().Symbol("pi2+")), q2));
			State<int> q3 = fsa.CreateState(true);
			fsa.StartState.AddArc(new Arc<int>(new ArcCondition<int>(featSys.BuildFS().Symbol("pi2+")), q3));
			fsa.StartState.AddArc(new Arc<int>(new ArcCondition<int>(featSys.BuildFS().Symbol("pi3+")), q3));
			State<int> q4 = fsa.CreateState(true);
			fsa.StartState.AddArc(new Arc<int>(new ArcCondition<int>(featSys.BuildFS().Symbol("pi2+")), q4));
			State<int> q5 = fsa.CreateState(true);
			fsa.StartState.AddArc(new Arc<int>(new ArcCondition<int>(featSys.BuildFS().Symbol("pi3+")), q5));
			fsa.Determinize();

			var writer = new StreamWriter("c:\\dfa.dot");
			fsa.ToGraphViz(writer);
			writer.Close();

			Assert.AreEqual(7, fsa.StartState.Arcs.Count());
			Assert.IsTrue(fsa.StartState.Arcs.Any(tran => tran.Condition.FeatureStructure.Equals(featSys.BuildFS().Symbol("pi1+").Symbol("pi2+").Symbol("pi3+"))));
			Assert.IsTrue(fsa.StartState.Arcs.Any(tran => tran.Condition.FeatureStructure.Equals(featSys.BuildFS().Symbol("pi1+").Symbol("pi2-").Symbol("pi3+"))));
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
