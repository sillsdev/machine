using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.Matching;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab.MorphologicalRules
{
	public class SynthesisAffixProcessAllomorphRuleSpec : IPatternRuleSpec<Word, ShapeNode>
	{
		private readonly AffixProcessAllomorph _allomorph;
		private readonly Pattern<Word, ShapeNode> _pattern;
		private readonly HashSet<MorphologicalOutputAction> _nonAllomorphActions;

		public SynthesisAffixProcessAllomorphRuleSpec(AffixProcessAllomorph allomorph)
		{
			_allomorph = allomorph;

			IList<Pattern<Word, ShapeNode>> lhs = _allomorph.Lhs;
			IList<MorphologicalOutputAction> rhs = _allomorph.Rhs;
			_nonAllomorphActions = new HashSet<MorphologicalOutputAction>();
			var redupParts = new List<List<MorphologicalOutputAction>>();
			foreach (List<MorphologicalOutputAction> partActions in rhs.Where(action => !string.IsNullOrEmpty(action.PartName))
				.GroupBy(action => action.PartName).Select(g => g.ToList()))
			{
				if (partActions.Count == 1)
				{
					if (partActions[0] is CopyFromInput)
						_nonAllomorphActions.Add(partActions[0]);
				}
				else
				{
					redupParts.Add(partActions);
				}
			}
			if (redupParts.Count > 0)
			{
				int start = -1;
				switch (_allomorph.ReduplicationHint)
				{
					case ReduplicationHint.Prefix:
						int prefixPartIndex = lhs.Count - 1;
						for (int i = rhs.Count - 1; i >= 0; i--)
						{
							MorphologicalOutputAction action = rhs[i];
							if (action.PartName == lhs[prefixPartIndex].Name || action.PartName == lhs[lhs.Count - 1].Name)
							{
								if (action.PartName == lhs[0].Name)
								{
									start = i;
									break;
								}
								if (action.PartName != lhs[prefixPartIndex].Name)
									prefixPartIndex = lhs.Count - 1;
								prefixPartIndex--;
							}
							else
							{
								prefixPartIndex = lhs.Count - 1;
							}
						}
						break;

					case ReduplicationHint.Suffix:
					case ReduplicationHint.Implicit:
						int suffixPartIndex = 0;
						for (int i = 0; i < rhs.Count; i++)
						{
							MorphologicalOutputAction action = rhs[i];
							if (action.PartName == lhs[suffixPartIndex].Name || action.PartName == lhs[0].Name)
							{
								if (action.PartName == lhs[lhs.Count - 1].Name)
								{
									start = i - (lhs.Count - 1);
									break;
								}
								if (action.PartName != lhs[suffixPartIndex].Name)
									suffixPartIndex = 0;
								suffixPartIndex++;
							}
							else
							{
								suffixPartIndex = 0;
							}
						}
						break;
				}

				foreach (List<MorphologicalOutputAction> partActions in redupParts)
				{
					for (int j = 0; j < partActions.Count; j++)
					{
						int index = rhs.IndexOf(partActions[j]);
						if ((start == -1 && j == partActions.Count - 1)
							|| (index >= start && index < start + lhs.Count))
						{
							_nonAllomorphActions.Add(partActions[j]);
						}
					}
				}
			}

			_pattern = new Pattern<Word, ShapeNode>();
			foreach (Pattern<Word, ShapeNode> part in lhs)
				_pattern.Children.Add(new Group<Word, ShapeNode>(part.Name, part.Children.CloneItems()));
		}

		public Pattern<Word, ShapeNode> Pattern
		{
			get { return _pattern; }
		}

		public bool IsApplicable(Word input)
		{
			return true;
		}

		public ShapeNode ApplyRhs(PatternRule<Word, ShapeNode> rule, Match<Word, ShapeNode> match, out Word output)
		{
			output = match.Input.Clone();
			output.Shape.Clear();
			var existingMorphNodes = new Dictionary<string, List<ShapeNode>>();
			var newMorphNodes = new List<ShapeNode>();
			foreach (MorphologicalOutputAction outputAction in _allomorph.Rhs)
			{
				foreach (Tuple<ShapeNode, ShapeNode> mapping in outputAction.Apply(match, output))
				{
					if (mapping.Item1 != null && _nonAllomorphActions.Contains(outputAction))
					{
						if (mapping.Item1.Annotation.Parent != null)
						{
							var allomorphID = (string) mapping.Item1.Annotation.Parent.FeatureStruct.GetValue(HCFeatureSystem.Allomorph);
							existingMorphNodes.GetOrCreate(allomorphID, () => new List<ShapeNode>()).Add(mapping.Item2);
						}
					}
					else
					{
						newMorphNodes.Add(mapping.Item2);
					}
				}
			}

			if (existingMorphNodes.Count > 0)
			{
				foreach (Allomorph allomorph in match.Input.AllomorphsInMorphOrder)
				{
					List<ShapeNode> nodes;
					if (existingMorphNodes.TryGetValue(allomorph.ID, out nodes))
						output.MarkMorph(nodes, allomorph);
				}
			}

			output.MarkMorph(newMorphNodes, _allomorph);
			output.MprFeatures.AddOutput(_allomorph.OutMprFeatures);

			return null;
		}
	}
}
