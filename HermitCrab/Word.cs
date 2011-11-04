using System.Collections.Generic;
using System.Linq;
using SIL.APRE;
using SIL.APRE.FeatureModel;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This enumeration represents the morpher mode type.
	/// </summary>
	public enum Mode
	{
		/// <summary>
		/// Analysis mode (unapplication of rules)
		/// </summary>
		Analysis,
		/// <summary>
		/// Synthesis mode (application of rules)
		/// </summary>
		Synthesis
	}

	public class Word : IData<ShapeNode>, ICloneable<Word>
	{
		private readonly Shape _shape;
		private readonly FeatureStruct _syntacticFS;
		private readonly Stack<MorphologicalRule> _mrules;

		public Word(Stratum stratum, string word)
		{
			Stratum = stratum;
			if (!stratum.CharacterDefinitionTable.ToShape(word, out _shape))
			{
				var me = new MorphException(MorphErrorCode.InvalidShape,
					string.Format(HCStrings.kstidInvalidWord, word, stratum.CharacterDefinitionTable.ID));
				me.Data["shape"] = word;
				me.Data["charDefTable"] = stratum.CharacterDefinitionTable.ID;
			}
			_syntacticFS = new FeatureStruct();
			_mrules = new Stack<MorphologicalRule>();
		}

		public Word(Word word)
		{
			Stratum = word.Stratum;
			_shape = new Shape(word._shape.SpanFactory);
			word.CopyTo(word._shape.First, word._shape.Last, this);
			_syntacticFS = word._syntacticFS.Clone();
			_mrules = new Stack<MorphologicalRule>(word._mrules);
			Root = word.Root;
		}

		public Stratum Stratum { get; set; }

		public Trace CurrentTrace { get; set; }

		public LexEntry Root { get; private set; }

		public Shape Shape
		{
			get { return _shape; }
		}

		public FeatureStruct SyntacticFeatureStruct
		{
			get { return _syntacticFS; }
		}

		public Span<ShapeNode> Span
		{
			get { return _shape.Span; }
		}

		public AnnotationList<ShapeNode> Annotations
		{
			get { return _shape.Annotations; }
		}

		public Span<ShapeNode> CopyTo(ShapeNode srcStart, ShapeNode srcEnd, Word dest)
		{
			return CopyTo(_shape.SpanFactory.Create(srcStart, srcEnd), dest);
		}

		public Span<ShapeNode> CopyTo(Span<ShapeNode> srcSpan, Word dest)
		{
			Span<ShapeNode> destSpan = _shape.CopyTo(srcSpan, dest._shape);
			Dictionary<ShapeNode, ShapeNode> mapping = _shape.GetNodes(srcSpan).Zip(dest._shape.GetNodes(destSpan)).ToDictionary(tuple => tuple.Item1, tuple => tuple.Item2);
			foreach (Annotation<ShapeNode> ann in _shape.Annotations.GetNodes(srcSpan))
			{
				if (ann.Type == HCFeatureSystem.MorphType)
				{
					dest._shape.Annotations.Add(new Annotation<ShapeNode>(ann.Type, dest._shape.SpanFactory.Create(mapping[ann.Span.Start], mapping[ann.Span.End]),
						ann.FeatureStruct.Clone()));
				}
			}

			return destSpan;
		}

		public void SetRootAllomorph(RootAllomorph rootAllomorph)
		{
			Root = (LexEntry) rootAllomorph.Morpheme;
			_syntacticFS.Clear();
			_syntacticFS.Merge(Root.SyntacticFeatureStruct);
			_shape.Annotations.Add(HCFeatureSystem.MorphType, _shape.First, _shape.Last,
				FeatureStruct.New(HCFeatureSystem.Instance).Feature(HCFeatureSystem.Allomorph).EqualTo(rootAllomorph.ID).Value);
		}

		public Word Clone()
		{
			return new Word(this);
		}

		public override string ToString()
		{
			return Stratum.CharacterDefinitionTable.ToRegexString(Shape, Mode.Analysis, true);
		}
	}
}
