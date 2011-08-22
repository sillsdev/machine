using System.Collections.Generic;

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

		public override FeatureValueType Type
		{
			get { return FeatureValueType.Variable; }
		}

		internal override bool Negation(out FeatureValue output)
		{
			if (Forward != null)
				return Forward.Negation(out output);

			output = new VariableFeatureValue(_name, !_agree);
			return true;
		}

		internal override bool IsUnifiable(FeatureValue other, bool useDefaults, IDictionary<string, FeatureValue> varBindings)
		{
			if (Forward != null)
				return Forward.IsUnifiable(other, useDefaults, varBindings);

			other = GetValue(other);
			if (other.Type == FeatureValueType.Variable)
			{
				var vfv = (VariableFeatureValue) other;
				return _name == vfv._name && _agree == vfv._agree;
			}

			if (!UnifyBindings(other, useDefaults, varBindings))
				return false;

			return true;
		}

		internal override bool DestructiveUnify(FeatureValue other, bool useDefaults, bool preserveInput,
			IDictionary<FeatureValue, FeatureValue> copies, IDictionary<string, FeatureValue> varBindings)
		{
			if (Forward != null)
				return Forward.DestructiveUnify(other, useDefaults, preserveInput, copies, varBindings);

			other = GetValue(other);
			if (!IsUnifiable(other, useDefaults, varBindings))
				return false;

			if (preserveInput)
			{
				if (copies != null)
					copies[other] = this;
			}
			else
			{
				other.Forward = this;
			}

			return true;
		}

		private bool UnifyBindings(FeatureValue other, bool useDefaults, IDictionary<string, FeatureValue> varBindings)
		{
			FeatureValue binding;
			if (varBindings.TryGetValue(_name, out binding))
			{
				FeatureValue value;
				if (_agree)
					value = other;
				else
					other.Negation(out value);
				if (!binding.DestructiveUnify(value, useDefaults, true, null, varBindings))
					return false;
			}
			else
			{
				if (_agree)
					binding = other.Clone();
				else
					other.Negation(out binding);
				varBindings[_name] = binding;
			}

			return true;
		}

		public override FeatureValue Clone()
		{
			if (Forward != null)
				return Forward.Clone();

			return new VariableFeatureValue(this);
		}

		public override string ToString()
		{
			if (Forward != null)
				return Forward.ToString();

			return (_agree ? "+" : "-") + _name;
		}
	}
}
