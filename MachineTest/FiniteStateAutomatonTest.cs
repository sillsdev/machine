using System.IO;
using System.Linq;
using NUnit.Framework;
using SIL.Machine.FeatureModel;
using SIL.Machine.Fsa;

namespace SIL.Machine.Test
{
	[TestFixture]
	public class FiniteStateAutomatonTest
	{
		[Test]
		public void Determinize()
		{
			FeatureSystem featSys = FeatureSystem.New()
				.SymbolicFeature("pi1", pi1 => pi1
					.Symbol("pi1+", "+")
               		.Symbol("pi1-", "-"))
				.SymbolicFeature("pi2", pi2 => pi2
                   	.Symbol("pi2+", "+")
                   	.Symbol("pi2-", "-"))
				.SymbolicFeature("pi3", pi3 => pi3
                   	.Symbol("pi3+", "+")
                   	.Symbol("pi3-", "-")).Value;

			var fsa = new FiniteStateAutomaton<StringData, int>(Direction.LeftToRight);
			State<StringData, int> q1 = fsa.CreateAcceptingState();
			fsa.StartState.AddArc(FeatureStruct.New(featSys).Symbol("pi1+").Value, q1);
			State<StringData, int> q2 = fsa.CreateAcceptingState();
			fsa.StartState.AddArc(FeatureStruct.New(featSys).Symbol("pi1+").Value, q2);
			fsa.StartState.AddArc(FeatureStruct.New(featSys).Symbol("pi2+").Value, q2);
			State<StringData, int> q3 = fsa.CreateAcceptingState();
			fsa.StartState.AddArc(FeatureStruct.New(featSys).Symbol("pi2+").Value, q3);
			fsa.StartState.AddArc(FeatureStruct.New(featSys).Symbol("pi3+").Value, q3);
			State<StringData, int> q4 = fsa.CreateAcceptingState();
			fsa.StartState.AddArc(FeatureStruct.New(featSys).Symbol("pi2+").Value, q4);
			State<StringData, int> q5 = fsa.CreateAcceptingState();
			fsa.StartState.AddArc(FeatureStruct.New(featSys).Symbol("pi3+").Value, q5);
			fsa.Determinize(false);

			var writer = new StreamWriter("c:\\dfa.dot");
			fsa.ToGraphViz(writer);
			writer.Close();

			Assert.AreEqual(7, fsa.StartState.OutgoingArcs.Count());
			Assert.IsTrue(fsa.StartState.OutgoingArcs.Any(tran => tran.Condition.Equals(FeatureStruct.New(featSys).Symbol("pi1+").Symbol("pi2+").Symbol("pi3+").Value)));
			Assert.IsTrue(fsa.StartState.OutgoingArcs.Any(tran => tran.Condition.Equals(FeatureStruct.New(featSys).Symbol("pi1+").Symbol("pi2-").Symbol("pi3+").Value)));
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
