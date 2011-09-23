using System;
using SIL.APRE.FeatureModel;

namespace SIL.APRE.Fsa
{
	public class FsaFeatureSystem : FeatureSystem
	{
		public static readonly SymbolicFeature Anchor;
		public static readonly FeatureSymbol LeftSide;
		public static readonly FeatureSymbol RightSide;

		public static readonly StringFeature Type;

		private static readonly FsaFeatureSystem FeatureSystem;

		static FsaFeatureSystem()
		{
			Anchor = new SymbolicFeature(Guid.NewGuid().ToString(), "Anchor");

			LeftSide = new FeatureSymbol(Guid.NewGuid().ToString(), "LeftSide");
			Anchor.AddPossibleSymbol(LeftSide);
			RightSide = new FeatureSymbol(Guid.NewGuid().ToString(), "RightSide");
			Anchor.AddPossibleSymbol(RightSide);

			Type = new StringFeature(Guid.NewGuid().ToString(), "Type");

			FeatureSystem = new FsaFeatureSystem();
		}

		public static FsaFeatureSystem Instance
		{
			get { return FeatureSystem; }
		}

		private FsaFeatureSystem()
		{
			base.AddFeature(Anchor);
			base.AddFeature(Type);
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
