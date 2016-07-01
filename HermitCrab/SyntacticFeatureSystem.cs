using System;
using System.Collections.Generic;
using SIL.Machine.FeatureModel;

namespace SIL.HermitCrab
{
	public enum SyntacticFeatureType
	{
		Head,
		Foot
	}

	public class SyntacticFeatureSystem : FeatureSystem
	{
		public static readonly string PartOfSpeechID = Guid.NewGuid().ToString();
		public static readonly string HeadID = Guid.NewGuid().ToString();
		public static readonly string FootID = Guid.NewGuid().ToString();

		private readonly HashSet<Feature> _headFeatures;
		private readonly HashSet<Feature> _footFeatures;

		public SyntacticFeatureSystem()
		{
			_headFeatures = new HashSet<Feature>();
			_footFeatures = new HashSet<Feature>();
		}

		public SymbolicFeature PartOfSpeechFeature
		{
			get
			{
				SymbolicFeature feature;
				if (TryGetFeature(PartOfSpeechID, out feature))
					return feature;
				return null;
			}
		}

		public ComplexFeature HeadFeature
		{
			get
			{
				ComplexFeature feature;
				if (TryGetFeature(HeadID, out feature))
					return feature;
				return null;
			}
		}

		public ComplexFeature FootFeature
		{
			get
			{
				ComplexFeature feature;
				if (TryGetFeature(FootID, out feature))
					return feature;
				return null;
			}
		}

		public IEnumerable<Feature> HeadFeatures
		{
			get { return _headFeatures; }
		}

		public IEnumerable<Feature> FootFeatures
		{
			get { return _footFeatures; }
		}

		public SymbolicFeature AddPartsOfSpeech(params FeatureSymbol[] pos)
		{
			return AddPartsOfSpeech((IEnumerable<FeatureSymbol>) pos);
		}

		public SymbolicFeature AddPartsOfSpeech(IEnumerable<FeatureSymbol> pos)
		{
			var posFeature = new SymbolicFeature(PartOfSpeechID, pos) {Description = "POS"};
			base.Add(posFeature);
			return posFeature;
		}

		public ComplexFeature AddHeadFeature()
		{
			var headFeature = new ComplexFeature(HeadID) {Description = "Head"};
			base.Add(headFeature);
			return headFeature;
		}

		public ComplexFeature AddFootFeature()
		{
			var footFeature = new ComplexFeature(FootID) {Description = "Foot"};
			base.Add(footFeature);
			return footFeature;
		}

		public void Add(Feature feature, SyntacticFeatureType type)
		{
			CheckFrozen();

			base.Add(feature);
			switch (type)
			{
				case SyntacticFeatureType.Head:
					_headFeatures.Add(feature);
					break;
				case SyntacticFeatureType.Foot:
					_footFeatures.Add(feature);
					break;
			}
		}

		public override void Add(Feature feature)
		{
			CheckFrozen();

			base.Add(feature);
			// default to head feature
			_headFeatures.Add(feature);
		}

		public override void Clear()
		{
			CheckFrozen();

			base.Clear();
			_headFeatures.Clear();
			_footFeatures.Clear();
		}

		public override bool Remove(Feature feature)
		{
			CheckFrozen();

			if (base.Remove(feature))
			{
				_headFeatures.Remove(feature);
				_footFeatures.Remove(feature);
				return true;
			}

			return false;
		}
	}
}
