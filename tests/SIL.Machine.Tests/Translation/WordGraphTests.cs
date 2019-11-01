using NUnit.Framework;
using SIL.Machine.Annotations;

namespace SIL.Machine.Translation
{
	[TestFixture]
	public class WordGraphTests
	{
		[Test]
		public void Optimize_RedundantArc()
		{
			var arcs = new WordGraphArc[]
			{
				new WordGraphArc(0, 1, -11.1167f, new[] { "In", "the" },
					new WordAlignmentMatrix(2, 2) { [0, 0] = true, [1, 1] = true }, Range<int>.Create(0, 2), false),
				new WordGraphArc(0, 2, -13.7804f, new[] { "In" },
					new WordAlignmentMatrix(1, 1) { [0, 0] = true }, Range<int>.Create(0, 1), false),
				new WordGraphArc(1, 3, -12.9695f, new[] { "beginning" },
					new WordAlignmentMatrix(1, 1) { [0, 0] = true }, Range<int>.Create(2, 3), false),
				new WordGraphArc(2, 3, -7.68319f, new[] { "the", "beginning" },
					new WordAlignmentMatrix(2, 2) { [0, 0] = true, [1, 1] = true }, Range<int>.Create(1, 3), false),
				new WordGraphArc(2, 1, -14.4373f, new[] { "the" },
					new WordAlignmentMatrix(1, 1) { [0, 0] = true }, Range<int>.Create(1, 2), false),
				new WordGraphArc(3, 4, -19.3042f, new[] { "his", "Word" },
					new WordAlignmentMatrix(2, 2) { [0, 0] = true, [1, 1] = true }, Range<int>.Create(3, 5), false),
				new WordGraphArc(3, 5, -8.49148f, new[] { "the", "Word" },
					new WordAlignmentMatrix(2, 2) { [0, 0] = true, [1, 1] = true }, Range<int>.Create(3, 5), false)
			};

			var wordGraph = new WordGraph(arcs, new[] { 4, 5 }, -191.0998f);
			WordGraph optimizedWordGraph = wordGraph.Optimize();

			Assert.That(optimizedWordGraph.StateCount, Is.EqualTo(5));
			Assert.That(optimizedWordGraph.Arcs.Count, Is.EqualTo(4));
			Assert.That(optimizedWordGraph.Arcs[0].Words, Is.EqualTo(new[] { "In" }));
			Assert.That(optimizedWordGraph.Arcs[1].Words, Is.EqualTo(new[] { "the", "beginning" }));
			Assert.That(optimizedWordGraph.Arcs[2].Words, Is.EqualTo(new[] { "his", "Word" }));
			Assert.That(optimizedWordGraph.Arcs[3].Words, Is.EqualTo(new[] { "the", "Word" }));
			Assert.That(optimizedWordGraph.FinalStates, Is.EquivalentTo(new[] { 3, 4 }));
			Assert.That(optimizedWordGraph.InitialStateScore, Is.EqualTo(-191.0998f));
		}

		[Test]
		public void Optimize_PartialPhraseOverlap()
		{
			var arcs = new WordGraphArc[]
			{
				new WordGraphArc(0, 1, -23.5761f, new[] { "In", "your" },
					new WordAlignmentMatrix(2, 2) { [0, 0] = true, [1, 1] = true }, Range<int>.Create(0, 2), false),
				new WordGraphArc(0, 2, -13.7804f, new[] { "In" },
					new WordAlignmentMatrix(1, 1) { [0, 0] = true }, Range<int>.Create(0, 1), false),
				new WordGraphArc(2, 3, -7.68319f, new[] { "the", "beginning" },
					new WordAlignmentMatrix(2, 2) { [0, 0] = true, [1, 1] = true }, Range<int>.Create(1, 3), false),
				new WordGraphArc(3, 4, -19.3042f, new[] { "his", "Word" },
					new WordAlignmentMatrix(2, 2) { [0, 0] = true, [1, 1] = true }, Range<int>.Create(3, 5), false),
				new WordGraphArc(3, 5, -8.49148f, new[] { "the", "Word" },
					new WordAlignmentMatrix(2, 2) { [0, 0] = true, [1, 1] = true }, Range<int>.Create(3, 5), false),
				new WordGraphArc(1, 6, -15.2926f, new[] { "beginning" },
					new WordAlignmentMatrix(1, 1) { [0, 0] = true }, Range<int>.Create(2, 3), false),
				new WordGraphArc(6, 4, -19.3042f, new[] { "his", "Word" },
					new WordAlignmentMatrix(2, 2) { [0, 0] = true, [1, 1] = true }, Range<int>.Create(3, 5), false),
				new WordGraphArc(6, 5, -8.49148f, new[] { "the", "Word" },
					new WordAlignmentMatrix(2, 2) { [0, 0] = true, [1, 1] = true }, Range<int>.Create(3, 5), false)
			};

			var wordGraph = new WordGraph(arcs, new[] { 4, 5 }, -191.0998f);
			WordGraph optimizedWordGraph = wordGraph.Optimize();

			Assert.That(optimizedWordGraph.StateCount, Is.EqualTo(7));
			Assert.That(optimizedWordGraph.Arcs.Count, Is.EqualTo(8));
			Assert.That(optimizedWordGraph.Arcs[0].Words, Is.EqualTo(new[] { "In", "your" }));
			Assert.That(optimizedWordGraph.Arcs[1].Words, Is.EqualTo(new[] { "beginning" }));
			Assert.That(optimizedWordGraph.Arcs[2].Words, Is.EqualTo(new[] { "In" }));
			Assert.That(optimizedWordGraph.Arcs[3].Words, Is.EqualTo(new[] { "the", "beginning" }));
			Assert.That(optimizedWordGraph.Arcs[4].Words, Is.EqualTo(new[] { "his", "Word" }));
			Assert.That(optimizedWordGraph.Arcs[5].Words, Is.EqualTo(new[] { "the", "Word" }));
			Assert.That(optimizedWordGraph.Arcs[6].Words, Is.EqualTo(new[] { "his", "Word" }));
			Assert.That(optimizedWordGraph.Arcs[7].Words, Is.EqualTo(new[] { "the", "Word" }));
			Assert.That(optimizedWordGraph.FinalStates, Is.EquivalentTo(new[] { 5, 6 }));
			Assert.That(optimizedWordGraph.InitialStateScore, Is.EqualTo(-191.0998f));
		}

		[Test]
		public void Optimize_RedundantArcWithFinalState()
		{
			var arcs = new WordGraphArc[]
			{
				new WordGraphArc(0, 1, -11.1167f, new[] { "In", "the" },
					new WordAlignmentMatrix(2, 2) { [0, 0] = true, [1, 1] = true }, Range<int>.Create(0, 2), false),
				new WordGraphArc(0, 2, -13.7804f, new[] { "In" },
					new WordAlignmentMatrix(1, 1) { [0, 0] = true }, Range<int>.Create(0, 1), false),
				new WordGraphArc(1, 3, -12.9695f, new[] { "beginning" },
					new WordAlignmentMatrix(1, 1) { [0, 0] = true }, Range<int>.Create(2, 3), false),
				new WordGraphArc(2, 3, -7.68319f, new[] { "the", "beginning" },
					new WordAlignmentMatrix(2, 2) { [0, 0] = true, [1, 1] = true }, Range<int>.Create(1, 3), false),
				new WordGraphArc(2, 1, -14.4373f, new[] { "the" },
					new WordAlignmentMatrix(1, 1) { [0, 0] = true }, Range<int>.Create(1, 2), false),
				new WordGraphArc(3, 4, -19.3042f, new[] { "his", "Word" },
					new WordAlignmentMatrix(2, 2) { [0, 0] = true, [1, 1] = true }, Range<int>.Create(3, 5), false),
				new WordGraphArc(3, 5, -8.49148f, new[] { "the", "Word" },
					new WordAlignmentMatrix(2, 2) { [0, 0] = true, [1, 1] = true }, Range<int>.Create(3, 5), false)
			};

			var wordGraph = new WordGraph(arcs, new[] { 1, 4, 5 }, -191.0998f);
			WordGraph optimizedWordGraph = wordGraph.Optimize();

			Assert.That(optimizedWordGraph.StateCount, Is.EqualTo(6));
			Assert.That(optimizedWordGraph.Arcs.Count, Is.EqualTo(5));
			Assert.That(optimizedWordGraph.Arcs[0].Words, Is.EqualTo(new[] { "In", "the" }));
			Assert.That(optimizedWordGraph.Arcs[1].Words, Is.EqualTo(new[] { "In" }));
			Assert.That(optimizedWordGraph.Arcs[2].Words, Is.EqualTo(new[] { "the", "beginning" }));
			Assert.That(optimizedWordGraph.Arcs[3].Words, Is.EqualTo(new[] { "his", "Word" }));
			Assert.That(optimizedWordGraph.Arcs[4].Words, Is.EqualTo(new[] { "the", "Word" }));
			Assert.That(optimizedWordGraph.FinalStates, Is.EquivalentTo(new[] { 1, 4, 5 }));
			Assert.That(optimizedWordGraph.InitialStateScore, Is.EqualTo(-191.0998f));
		}

		[Test]
		public void Optimize_RedundantArcWithNonRedundantBranch()
		{
			var arcs = new WordGraphArc[]
			{
				new WordGraphArc(0, 1, -11.1167f, new[] { "In", "the" },
					new WordAlignmentMatrix(2, 2) { [0, 0] = true, [1, 1] = true }, Range<int>.Create(0, 2), false),
				new WordGraphArc(0, 2, -13.7804f, new[] { "In" },
					new WordAlignmentMatrix(1, 1) { [0, 0] = true }, Range<int>.Create(0, 1), false),
				new WordGraphArc(1, 3, -12.9695f, new[] { "beginning" },
					new WordAlignmentMatrix(1, 1) { [0, 0] = true }, Range<int>.Create(2, 3), false),
				new WordGraphArc(2, 3, -7.68319f, new[] { "the", "beginning" },
					new WordAlignmentMatrix(2, 2) { [0, 0] = true, [1, 1] = true }, Range<int>.Create(1, 3), false),
				new WordGraphArc(2, 1, -14.4373f, new[] { "the" },
					new WordAlignmentMatrix(1, 1) { [0, 0] = true }, Range<int>.Create(1, 2), false),
				new WordGraphArc(3, 4, -19.3042f, new[] { "his", "Word" },
					new WordAlignmentMatrix(2, 2) { [0, 0] = true, [1, 1] = true }, Range<int>.Create(3, 5), false),
				new WordGraphArc(3, 5, -8.49148f, new[] { "the", "Word" },
					new WordAlignmentMatrix(2, 2) { [0, 0] = true, [1, 1] = true }, Range<int>.Create(3, 5), false),
				new WordGraphArc(1, 6, -25.9695f, new[] { "end" },
					new WordAlignmentMatrix(1, 1) { [0, 0] = true }, Range<int>.Create(2, 3), false),
				new WordGraphArc(6, 4, -19.3042f, new[] { "his", "Word" },
					new WordAlignmentMatrix(2, 2) { [0, 0] = true, [1, 1] = true }, Range<int>.Create(3, 5), false),
				new WordGraphArc(6, 5, -8.49148f, new[] { "the", "Word" },
					new WordAlignmentMatrix(2, 2) { [0, 0] = true, [1, 1] = true }, Range<int>.Create(3, 5), false)
			};

			var wordGraph = new WordGraph(arcs, new[] { 4, 5 }, -191.0998f);
			WordGraph optimizedWordGraph = wordGraph.Optimize();

			Assert.That(optimizedWordGraph.StateCount, Is.EqualTo(7));
			Assert.That(optimizedWordGraph.Arcs.Count, Is.EqualTo(8));
			Assert.That(optimizedWordGraph.Arcs[0].Words, Is.EqualTo(new[] { "In" }));
			Assert.That(optimizedWordGraph.Arcs[1].Words, Is.EqualTo(new[] { "the", "beginning" }));
			Assert.That(optimizedWordGraph.Arcs[2].Words, Is.EqualTo(new[] { "In", "the" }));
			Assert.That(optimizedWordGraph.Arcs[3].Words, Is.EqualTo(new[] { "end" }));
			Assert.That(optimizedWordGraph.Arcs[4].Words, Is.EqualTo(new[] { "his", "Word" }));
			Assert.That(optimizedWordGraph.Arcs[5].Words, Is.EqualTo(new[] { "the", "Word" }));
			Assert.That(optimizedWordGraph.Arcs[6].Words, Is.EqualTo(new[] { "his", "Word" }));
			Assert.That(optimizedWordGraph.Arcs[7].Words, Is.EqualTo(new[] { "the", "Word" }));
			Assert.That(optimizedWordGraph.FinalStates, Is.EquivalentTo(new[] { 5, 6 }));
			Assert.That(optimizedWordGraph.InitialStateScore, Is.EqualTo(-191.0998f));
		}
	}
}
