using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.HermitCrab.MorphologicalRules
{
	public enum MorphologicalTransformType
	{
		Head,
		NonHead,
		Affix
	}

	public class SynthesisMorphologicalTransform
	{
		private readonly IList<Pattern<Word, ShapeNode>> _lhs;
		private readonly IList<MorphologicalOutputAction> _rhs;
		private readonly Pattern<Word, ShapeNode> _pattern;
		private readonly HashSet<MorphologicalOutputAction> _nonAllomorphActions;

		public SynthesisMorphologicalTransform(MorphologicalTransformType type, IList<Pattern<Word, ShapeNode>> lhs, IList<MorphologicalOutputAction> rhs,
			ReduplicationHint redupHint = ReduplicationHint.Implicit)
		{
			_lhs = lhs;
			_rhs = rhs;
			switch (type)
			{
				case MorphologicalTransformType.Affix:
					_nonAllomorphActions = new HashSet<MorphologicalOutputAction>();
					var redupParts = new List<List<MorphologicalOutputAction>>();
					foreach (List<MorphologicalOutputAction> partActions in _rhs.Where(action => !string.IsNullOrEmpty(action.PartName))
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
						switch (redupHint)
						{
							case ReduplicationHint.Prefix:
								int prefixPartIndex = _lhs.Count - 1;
								for (int i = _rhs.Count - 1; i >= 0; i--)
								{
									MorphologicalOutputAction action = _rhs[i];
									if (action.PartName == _lhs[prefixPartIndex].Name || action.PartName == _lhs[_lhs.Count - 1].Name)
									{
										if (action.PartName == _lhs[0].Name)
										{
											start = i;
											break;
										}
										if (action.PartName != _lhs[prefixPartIndex].Name)
											prefixPartIndex = _lhs.Count - 1;
										prefixPartIndex--;
									}
									else
									{
										prefixPartIndex = _lhs.Count - 1;
									}
								}
								break;

							case ReduplicationHint.Suffix:
							case ReduplicationHint.Implicit:
								int suffixPartIndex = 0;
								for (int i = 0; i < _rhs.Count; i++)
								{
									MorphologicalOutputAction action = _rhs[i];
									if (action.PartName == _lhs[suffixPartIndex].Name || action.PartName == _lhs[0].Name)
									{
										if (action.PartName == _lhs[_lhs.Count - 1].Name)
										{
											start = i - (_lhs.Count - 1);
											break;
										}
										if (action.PartName != _lhs[suffixPartIndex].Name)
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
								int index = _rhs.IndexOf(partActions[j]);
								if ((start == -1 && j == partActions.Count - 1)
								    || (index >= start && index < start + _lhs.Count))
								{
									_nonAllomorphActions.Add(partActions[j]);
								}
							}
						}
					}
					break;

				case MorphologicalTransformType.Head:
					_nonAllomorphActions = new HashSet<MorphologicalOutputAction>(_rhs);
					break;

				case MorphologicalTransformType.NonHead:
					_nonAllomorphActions = new HashSet<MorphologicalOutputAction>();
					break;
			}

			_pattern = new Pattern<Word, ShapeNode>();
			foreach (Pattern<Word, ShapeNode> part in _lhs)
				_pattern.Children.Add(new Group<Word, ShapeNode>(part.Name, part.Children.DeepClone()));
		}

		public Pattern<Word, ShapeNode> Pattern
		{
			get { return _pattern; }
		}

		public Word Apply(SpanFactory<ShapeNode> spanFactory, Match<Word, ShapeNode> match, Allomorph allomorph)
		{
			Word output = match.Input.DeepClone();
			output.Shape.Clear();
			var existingMorphNodes = new Dictionary<string, List<ShapeNode>>();
			var newMorphNodes = new List<ShapeNode>();
			foreach (MorphologicalOutputAction outputAction in _rhs)
			{
				foreach (Tuple<ShapeNode, ShapeNode> mapping in outputAction.Apply(match, output))
				{
					if (mapping.Item1 != null && _nonAllomorphActions.Contains(outputAction))
					{
						if (mapping.Item1.Annotation.Parent != null)
						{
							var allomorphID = (string) mapping.Item1.Annotation.Parent.FeatureStruct.GetValue(HCFeatureSystem.Allomorph);
							existingMorphNodes.GetValue(allomorphID, () => new List<ShapeNode>()).Add(mapping.Item2);
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
				foreach (Annotation<ShapeNode> morph in match.Input.Morphs)
				{
					List<ShapeNode> nodes;
					if (existingMorphNodes.TryGetValue((string) morph.FeatureStruct.GetValue(HCFeatureSystem.Allomorph), out nodes))
						MarkMorph(spanFactory, output, nodes, morph.FeatureStruct.DeepClone());
				}
			}

			if (newMorphNodes.Count > 0 && allomorph != null)
			{
				FeatureStruct fs = FeatureStruct.New()
					.Symbol(HCFeatureSystem.Morph)
					.Feature(HCFeatureSystem.Allomorph).EqualTo(allomorph.ID).Value;
				MarkMorph(spanFactory, output, newMorphNodes, fs);
				output.Allomorphs.Add(allomorph);
			}

			return output;
		}

		private void MarkMorph(SpanFactory<ShapeNode> spanFactory, Word output, List<ShapeNode> nodes, FeatureStruct fs)
		{
			if (nodes.Count == 0)
				return;

			var ann = new Annotation<ShapeNode>(spanFactory.Create(nodes[0], nodes[nodes.Count - 1]), fs);
			ann.Children.AddRange(nodes.Select(n => n.Annotation));
			output.Annotations.Add(ann, false);
		}
	}
}
