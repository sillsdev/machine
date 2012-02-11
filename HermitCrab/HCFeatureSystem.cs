using System;
using SIL.Machine.FeatureModel;

namespace SIL.HermitCrab
{
	public class HCFeatureSystem : FeatureSystem
	{
		public static readonly SymbolicFeature Type;
		public static readonly FeatureSymbol AnchorType;
		public static readonly FeatureSymbol SegmentType;
		public static readonly FeatureSymbol BoundaryType;
		public static readonly FeatureSymbol MorphType;

		public static readonly SymbolicFeature Backtrack;
		public static readonly FeatureSymbol Searched;
		public static readonly FeatureSymbol NotSearched;

		public static readonly SymbolicFeature Anchor;
		public static readonly FeatureSymbol LeftSide;
		public static readonly FeatureSymbol RightSide;

		public static readonly StringFeature StrRep;

		public static readonly StringFeature Allomorph;

		private static readonly HCFeatureSystem FeatureSystem;

		static HCFeatureSystem()
		{
			Type = new SymbolicFeature(Guid.NewGuid().ToString()) {Description = "Type"};
			AnchorType = new FeatureSymbol(Guid.NewGuid().ToString()) {Description = "anchor"};
			Type.AddPossibleSymbol(AnchorType);
			SegmentType = new FeatureSymbol(Guid.NewGuid().ToString()) {Description = "segment"};
			Type.AddPossibleSymbol(SegmentType);
			BoundaryType = new FeatureSymbol(Guid.NewGuid().ToString()) {Description = "boundary"};
			Type.AddPossibleSymbol(BoundaryType);
			MorphType = new FeatureSymbol(Guid.NewGuid().ToString()) {Description = "morph"};
			Type.AddPossibleSymbol(MorphType);

			Backtrack = new SymbolicFeature(Guid.NewGuid().ToString()) {Description = "Backtrack"};
			Searched = new FeatureSymbol(Guid.NewGuid().ToString()) {Description = "Searched"};
			Backtrack.AddPossibleSymbol(Searched);
			NotSearched = new FeatureSymbol(Guid.NewGuid().ToString()) {Description = "NotSearched"};
			Backtrack.AddPossibleSymbol(NotSearched);

			Anchor = new SymbolicFeature(Guid.NewGuid().ToString()) {Description = "Anchor"};
			LeftSide = new FeatureSymbol(Guid.NewGuid().ToString()) {Description = "LeftSide"};
			Anchor.AddPossibleSymbol(LeftSide);
			RightSide = new FeatureSymbol(Guid.NewGuid().ToString()) {Description = "RightSide"};
			Anchor.AddPossibleSymbol(RightSide);

			StrRep = new StringFeature(Guid.NewGuid().ToString()) {Description = "StrRep"};

			Allomorph = new StringFeature(Guid.NewGuid().ToString()) {Description = "Allomorph"};

			FeatureSystem = new HCFeatureSystem();
		}

		public static HCFeatureSystem Instance
		{
			get { return FeatureSystem; }
		}

		private HCFeatureSystem()
		{
			base.AddFeature(Type);
			base.AddFeature(Backtrack);
			base.AddFeature(Anchor);
			base.AddFeature(StrRep);
			base.AddFeature(Allomorph);
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
