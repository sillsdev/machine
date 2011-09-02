namespace SIL.APRE.FeatureModel
{
	public class VariableFeatureValue : SimpleFeatureValue
	{
		private readonly string _name;
		private readonly bool _agree;

		public VariableFeatureValue(string name, bool agree)
		{
			_name = name;
			_agree = agree;
		}

		public VariableFeatureValue(VariableFeatureValue vsfv)
		{
			_name = vsfv._name;
			_agree = vsfv._agree;
		}

		public string Name
		{
			get { return _name; }
		}

		public bool Agree
		{
			get { return _agree; }
		}

		public override bool Negation(out FeatureValue output)
		{
			output = new VariableFeatureValue(_name, !_agree);
			return true;
		}

		protected override bool IsValuesUnifiable(FeatureValue other, bool negate)
		{
			var otherVfv = other as VariableFeatureValue;
			if (otherVfv == null)
				return false;

			return _name == otherVfv._name && _agree == otherVfv._agree;
		}

		protected override void UnifyValues(FeatureValue other, bool negate)
		{
		}

		public override FeatureValue Clone()
		{
			return new VariableFeatureValue(this);
		}

		public override string ToString()
		{
			return (_agree ? "+" : "-") + _name;
		}
	}
}
