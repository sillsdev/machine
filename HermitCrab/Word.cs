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
		private readonly Mode _mode;
		private readonly AnnotationList<ShapeNode> _annotations; 
		private readonly Annotation<ShapeNode> _ann;

		public Word(Stratum stratum, Mode mode)
		{
			Stratum = stratum;
			_mode = mode;
			_annotations = new AnnotationList<ShapeNode>();
			_shape = new Shape(stratum.CharacterDefinitionTable.SpanFactory, _annotations);
			_ann = new Annotation<ShapeNode>(HCFeatureSystem.WordType,
				_shape.SpanFactory.Create(_shape.Begin, _shape.End), new FeatureStruct());
			_annotations.Add(_ann);
		}

		public Word(Stratum stratum, Mode mode, string word)
		{
			Stratum = stratum;
			_mode = mode;
			_annotations = new AnnotationList<ShapeNode>();
			_shape = stratum.CharacterDefinitionTable.ToShape(word, _annotations);
			_ann = new Annotation<ShapeNode>(HCFeatureSystem.WordType,
				_shape.SpanFactory.Create(_shape.Begin, _shape.End), new FeatureStruct());
			_annotations.Add(_ann);
		}

		public Word(Word word)
		{
			Stratum = word.Stratum;
			_mode = word._mode;
			_annotations = new AnnotationList<ShapeNode>();
			_shape = new Shape(word._shape.SpanFactory, _annotations);
			word.CopyTo(word._shape.SpanFactory.Create(_shape.First, _shape.Last), this);
		}

		public Stratum Stratum { get; set; }

		public Shape Shape
		{
			get { return _shape; }
		}

		public Mode Mode
		{
			get { return _mode; }
		}

		public Annotation<ShapeNode> Annotation
		{
			get { return _ann; }
		}

		public Span<ShapeNode> Span
		{
			get { return _ann.Span; }
		}

		public AnnotationList<ShapeNode> Annotations
		{
			get { return _annotations; }
		}

		public Span<ShapeNode> CopyTo(Span<ShapeNode> srcSpan, Word dest)
		{
			Span<ShapeNode> destSpan = _shape.CopyTo(srcSpan, dest._shape);
			Dictionary<ShapeNode, ShapeNode> mapping = _shape.GetNodes(srcSpan).Zip(dest._shape.GetNodes(destSpan)).ToDictionary(tuple => tuple.Item1, tuple => tuple.Item2);
			foreach (Annotation<ShapeNode> ann in _annotations.GetNodes(srcSpan))
			{
				if (!ann.Type.IsOneOf(HCFeatureSystem.SegmentType, HCFeatureSystem.BoundaryType, HCFeatureSystem.AnchorType))
				{
					dest._annotations.Add(new Annotation<ShapeNode>(ann.Type, dest._shape.SpanFactory.Create(mapping[ann.Span.Start], mapping[ann.Span.End]),
						(FeatureStruct) ann.FeatureStruct.Clone()));
				}
			}

			return destSpan;
		}

		public Word Clone()
		{
			return new Word(this);
		}

		public override string ToString()
		{
			return Stratum.CharacterDefinitionTable.ToRegexString(_shape, _mode, true);
		}
	}
}
