using System;
using System.Collections.Generic;

namespace SIL.APRE
{
	public class StringFeatureValue : SimpleFeatureValue<string>
	{
		public StringFeatureValue(IEnumerable<string> values)
			: base(values)
		{
		}

		public StringFeatureValue(string value)
			: base(value)
		{
		}

		public StringFeatureValue(StringFeatureValue fv)
			: base(fv)
		{
		}

		public override FeatureValueType Type
		{
			get { return FeatureValueType.String; }
		}

		public override void UninstantiateAll()
		{
			throw new NotImplementedException();
		}

		public override FeatureValue Clone()
		{
			return new StringFeatureValue(this);
		}
	}
}
