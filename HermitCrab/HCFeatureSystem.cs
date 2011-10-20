using System;
using SIL.APRE.FeatureModel;

namespace SIL.HermitCrab
{
	public class HCFeatureSystem : FeatureSystem
	{
		public static readonly SymbolicFeature Backtrack;
		public static readonly FeatureSymbol Searched;
		public static readonly FeatureSymbol NotSearched;

		public static readonly SymbolicFeature Anchor;
		public static readonly FeatureSymbol LeftSide;
		public static readonly FeatureSymbol RightSide;

		public static readonly StringFeature StrRep;

		public const string AnchorType = "anchor";
		public const string SegmentType = "segment";
		public const string BoundaryType = "boundary";
		public const string MorphType = "morph";
		public const string WordType = "word";

		private static readonly HCFeatureSystem FeatureSystem;

		static HCFeatureSystem()
		{
			Backtrack = new SymbolicFeature(Guid.NewGuid().ToString(), "Backtrack");
			Searched = new FeatureSymbol(Guid.NewGuid().ToString(), "Searched");
			Backtrack.AddPossibleSymbol(Searched);
			NotSearched = new FeatureSymbol(Guid.NewGuid().ToString(), "NotSearched");
			Backtrack.AddPossibleSymbol(NotSearched);

			Anchor = new SymbolicFeature(Guid.NewGuid().ToString(), "Anchor");
			LeftSide = new FeatureSymbol(Guid.NewGuid().ToString(), "LeftSide");
			Anchor.AddPossibleSymbol(LeftSide);
			RightSide = new FeatureSymbol(Guid.NewGuid().ToString(), "RightSide");
			Anchor.AddPossibleSymbol(RightSide);

			StrRep = new StringFeature(Guid.NewGuid().ToString(), "StrRep");

			FeatureSystem = new HCFeatureSystem();
		}

		public static HCFeatureSystem Instance
		{
			get { return FeatureSystem; }
		}

		private HCFeatureSystem()
		{
			base.AddFeature(Backtrack);
			base.AddFeature(Anchor);
			base.AddFeature(StrRep);
		}

		public override void AddFeature(Feature feature)
		{
			throw new NotSupportedException("This feature system is readonly.");
		}

		public override void Reset()
		{
			throw new NotSupportedException("This feature system is readonly.");
		}
	}
}
