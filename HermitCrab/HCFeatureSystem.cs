using System;
using SIL.APRE.FeatureModel;

namespace SIL.HermitCrab
{
	public class HCFeatureSystem : FeatureSystem
	{
		public static readonly SymbolicFeature Backtrack;
		public static readonly FeatureSymbol Searched;
		public static readonly FeatureSymbol NotSearched;

		private static readonly HCFeatureSystem FeatureSystem;

		static HCFeatureSystem()
		{
			Backtrack = new SymbolicFeature(Guid.NewGuid().ToString(), "Backtrack");
			Searched = new FeatureSymbol(Guid.NewGuid().ToString(), "Searched");
			Backtrack.AddPossibleSymbol(Searched);
			NotSearched = new FeatureSymbol(Guid.NewGuid().ToString(), "NotSearched");
			Backtrack.AddPossibleSymbol(NotSearched);

			FeatureSystem = new HCFeatureSystem();
		}

		public static HCFeatureSystem Instance
		{
			get { return FeatureSystem; }
		}

		private HCFeatureSystem()
		{
			base.AddFeature(Backtrack);
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
