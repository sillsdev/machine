using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.Matching;

namespace SIL.Machine.Morphology.HermitCrab.MorphologicalRules
{
	/// <summary>
	/// This abstract class is extended by each type of morphological output record
	/// used on the RHS of morphological rules.
	/// </summary>
	public abstract class MorphologicalOutputAction
	{
		private readonly string _partName;

		protected MorphologicalOutputAction(string partName)
		{
			_partName = partName;
		}

		public string PartName
		{
			get { return _partName; }
		}

		public abstract void GenerateAnalysisLhs(Pattern<Word, ShapeNode> analysisLhs,
			IDictionary<string, Pattern<Word, ShapeNode>> partLookup, IDictionary<string, int> capturedParts);

		/// <summary>
		/// Applies this output record to the specified word synthesis.
		/// </summary>
		/// <param name="match">The match.</param>
		/// <param name="output">The output word synthesis.</param>
		public abstract IEnumerable<Tuple<ShapeNode, ShapeNode>> Apply(Match<Word, ShapeNode> match, Word output);

		protected IEnumerable<ShapeNode> GetSkippedOptionalNodes(Shape shape, Range<ShapeNode> range)
		{
			ShapeNode node = range.Start.Prev;
			var skippedNodes = new List<ShapeNode>();
			while (node.Annotation.Optional)
			{
				skippedNodes.Add(node);
				node = node.Prev;
			}

			if (node == shape.Begin)
				return ((IEnumerable<ShapeNode>) skippedNodes).Reverse();
			return Enumerable.Empty<ShapeNode>();
		}
	}
}
