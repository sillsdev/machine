using System;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Morphology.HermitCrab
{
	public class HCFeatureSystem : FeatureSystem
	{
		public static readonly SymbolicFeature Type;
		public static readonly FeatureSymbol Anchor;
		public static readonly FeatureSymbol Segment;
		public static readonly FeatureSymbol Boundary;
		public static readonly FeatureSymbol Morph;

		public static readonly SymbolicFeature Modified;
		public static readonly FeatureSymbol Dirty;
		public static readonly FeatureSymbol Clean;

		public static readonly SymbolicFeature AnchorType;
		public static readonly FeatureSymbol LeftSide;
		public static readonly FeatureSymbol RightSide;

		public static readonly SymbolicFeature Deletion;
		public static readonly FeatureSymbol Deleted;
		public static readonly FeatureSymbol NotDeleted;

		public static readonly StringFeature StrRep;

		public static readonly StringFeature Allomorph;
		public static readonly StringFeature MorphID;

		public static readonly HCFeatureSystem Instance;

		public static readonly FeatureStruct LeftSideAnchor;
		public static readonly FeatureStruct RightSideAnchor;

		static HCFeatureSystem()
		{
			Anchor = new FeatureSymbol(Guid.NewGuid().ToString()) { Description = "anchor" };
			Segment = new FeatureSymbol(Guid.NewGuid().ToString()) { Description = "segment" };
			Boundary = new FeatureSymbol(Guid.NewGuid().ToString()) { Description = "boundary" };
			Morph = new FeatureSymbol(Guid.NewGuid().ToString()) { Description = "morph" };

			Type = new SymbolicFeature(Guid.NewGuid().ToString(), Anchor, Segment, Boundary, Morph) { Description = "Type" };

			Dirty = new FeatureSymbol(Guid.NewGuid().ToString()) { Description = "Dirty" };
			Clean = new FeatureSymbol(Guid.NewGuid().ToString()) { Description = "Clean" };

			Modified = new SymbolicFeature(Guid.NewGuid().ToString(), Dirty, Clean)
			{
				Description = "Modified",
				DefaultValue = new SymbolicFeatureValue(Clean)
			};

			Deleted = new FeatureSymbol(Guid.NewGuid().ToString()) { Description = "Deleted" };
			NotDeleted = new FeatureSymbol(Guid.NewGuid().ToString()) { Description = "NotDeleted" };

			Deletion = new SymbolicFeature(Guid.NewGuid().ToString(), Deleted, NotDeleted)
			{
				Description = "Deletion",
				DefaultValue = new SymbolicFeatureValue(NotDeleted)
			};

			LeftSide = new FeatureSymbol(Guid.NewGuid().ToString()) { Description = "LeftSide" };
			RightSide = new FeatureSymbol(Guid.NewGuid().ToString()) { Description = "RightSide" };

			AnchorType = new SymbolicFeature(Guid.NewGuid().ToString(), LeftSide, RightSide) { Description = "AnchorType" };

			StrRep = new StringFeature(Guid.NewGuid().ToString()) { Description = "StrRep" };

			Allomorph = new StringFeature(Guid.NewGuid().ToString()) { Description = "Allomorph" };
			MorphID = new StringFeature(Guid.NewGuid().ToString()) { Description = "ID" };

			Instance = new HCFeatureSystem();

			LeftSideAnchor = FeatureStruct.New().Symbol(Anchor).Symbol(LeftSide).Value;
			RightSideAnchor = FeatureStruct.New().Symbol(Anchor).Symbol(RightSide).Value;
		}

		private HCFeatureSystem()
		{
			Add(Type);
			Add(Modified);
			Add(Deletion);
			Add(AnchorType);
			Add(StrRep);
			Add(Allomorph);
			Freeze();
		}
	}
}
