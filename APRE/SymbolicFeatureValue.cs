using System;
using System.Collections.Generic;

namespace SIL.APRE
{
	public class SymbolicFeatureValue : SimpleFeatureValue<FeatureSymbol>
	{
		public SymbolicFeatureValue(IEnumerable<FeatureSymbol> values)
			: base(values)
		{
		}

		public SymbolicFeatureValue(FeatureSymbol value)
			: base(value)
		{
		}

		public SymbolicFeatureValue(SymbolicFeatureValue fv)
			: base(fv)
		{
		}

		public override FeatureValueType Type
		{
			get { return FeatureValueType.Symbol; }
		}

		public override void UninstantiateAll()
		{
			
		}

		public override FeatureValue Clone()
		{
			return new SymbolicFeatureValue(this);
		}
	}
}
