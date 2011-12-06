using System.Collections.Generic;

namespace SIL.Machine.FeatureModel
{
	public abstract class SimpleFeatureValue : FeatureValue, ICloneable<SimpleFeatureValue>
	{
		protected SimpleFeatureValue()
		{
		}

		protected SimpleFeatureValue(string varName, bool agree)
		{
			VariableName = varName;
			Agree = agree;
		}

		protected SimpleFeatureValue(SimpleFeatureValue sfv)
		{
			VariableName = sfv.VariableName;
			Agree = sfv.Agree;
		}

		public string VariableName { get; protected set; }

		public bool Agree { get; protected set; }

		public bool IsVariable
		{
			get { return !string.IsNullOrEmpty(VariableName); }
		}

		internal override bool IsDefiniteUnifiable(FeatureValue other, bool useDefaults, VariableBindings varBindings)
		{
			SimpleFeatureValue otherSfv;
			if (!Dereference(other, out otherSfv))
				return false;

			if (!IsVariable && !otherSfv.IsVariable)
			{
				if (!Overlaps(false, otherSfv, false))
					return false;
			}
			else if (IsVariable && !otherSfv.IsVariable)
			{
				SimpleFeatureValue binding;
				if (varBindings.TryGetValue(VariableName, out binding))
				{
					if (!binding.Overlaps(!Agree, otherSfv, false))
						return false;
				}
				else
				{
					varBindings[VariableName] = otherSfv.GetVariableValue(Agree);
				}
			}
			else if (!IsVariable && otherSfv.IsVariable)
			{
				SimpleFeatureValue binding;
				if (varBindings.TryGetValue(otherSfv.VariableName, out binding))
				{
					if (!Overlaps(false, binding, !otherSfv.Agree))
						return false;
				}
				else
				{
					varBindings[otherSfv.VariableName] = GetVariableValue(otherSfv.Agree);
				}
			}
			else
			{
				if (VariableName != otherSfv.VariableName || Agree != otherSfv.Agree)
					return false;
			}

			return true;
		}

		internal override bool DestructiveUnify(FeatureValue other, bool useDefaults, bool preserveInput, IDictionary<FeatureValue, FeatureValue> copies, VariableBindings varBindings)
		{
			SimpleFeatureValue otherSfv;
			if (!Dereference(other, out otherSfv))
				return false;

			if (!IsVariable && !otherSfv.IsVariable)
			{
				if (!Overlaps(false, otherSfv, false))
					return false;
				IntersectWith(false, otherSfv, false);
			}
			else if (IsVariable && !otherSfv.IsVariable)
			{
				SimpleFeatureValue binding;
				if (varBindings.TryGetValue(VariableName, out binding))
				{
					if (!binding.Overlaps(!Agree, otherSfv, false))
						return false;
					UnionWith(false, binding, !Agree);
					IntersectWith(false, otherSfv, false);
				}
				else
				{
					UnionWith(false, otherSfv, false);
					varBindings[VariableName] = otherSfv.GetVariableValue(Agree);
				}
				VariableName = null;
			}
			else if (!IsVariable && otherSfv.IsVariable)
			{
				SimpleFeatureValue binding;
				if (varBindings.TryGetValue(otherSfv.VariableName, out binding))
				{
					if (!Overlaps(false, binding, !otherSfv.Agree))
						return false;
					IntersectWith(false, binding, !otherSfv.Agree);
				}
				else
				{
					varBindings[otherSfv.VariableName] = GetVariableValue(otherSfv.Agree);
				}
			}
			else
			{
				if (VariableName != otherSfv.VariableName || Agree != otherSfv.Agree)
					return false;
			}

			if (preserveInput)
			{
				if (copies != null)
					copies[otherSfv] = this;
			}
			else
			{
				otherSfv.Forward = this;
			}

			return true;
		}

		internal override bool Merge(FeatureValue other, VariableBindings varBindings)
		{
			SimpleFeatureValue otherSfv;
			if (!Dereference(other, out otherSfv))
				return true;

			if (!IsVariable && !otherSfv.IsVariable)
			{
				UnionWith(false, otherSfv, false);
			}
			else if (IsVariable && !otherSfv.IsVariable)
			{
				SimpleFeatureValue binding;
				if (varBindings.TryGetValue(VariableName, out binding))
				{
					UnionWith(false, binding, !Agree);
					UnionWith(false, otherSfv, false);
				}
				else
				{
					UnionWith(false, otherSfv, false);
					varBindings[VariableName] = otherSfv.GetVariableValue(Agree);
				}
				VariableName = null;
			}
			else if (!IsVariable && otherSfv.IsVariable)
			{
				SimpleFeatureValue binding;
				if (varBindings.TryGetValue(otherSfv.VariableName, out binding))
				{
					UnionWith(false, binding, !otherSfv.Agree);
				}
				else
				{
					varBindings[otherSfv.VariableName] = GetVariableValue(otherSfv.Agree);
				}
			}

			return !IsUninstantiated;
		}

		internal override bool Subtract(FeatureValue other, VariableBindings varBindings)
		{
			SimpleFeatureValue otherSfv;
			if (!Dereference(other, out otherSfv))
				return true;

			if (!IsVariable && !otherSfv.IsVariable)
			{
				ExceptWith(false, otherSfv, false);
			}
			else if (IsVariable && !otherSfv.IsVariable)
			{
				SimpleFeatureValue binding;
				if (varBindings.TryGetValue(VariableName, out binding))
				{
					UnionWith(false, binding, !Agree);
					ExceptWith(false, otherSfv, false);
					VariableName = null;
				}
			}
			else if (!IsVariable && otherSfv.IsVariable)
			{
				SimpleFeatureValue binding;
				if (varBindings.TryGetValue(otherSfv.VariableName, out binding))
					ExceptWith(false, binding, !otherSfv.Agree);
			}

			return IsSatisfiable;
		}

		internal SimpleFeatureValue GetVariableValue(bool agree)
		{
			return agree ? Clone() : Negation();
		}

		protected override bool NondestructiveUnify(FeatureValue other, bool useDefaults, IDictionary<FeatureValue, FeatureValue> copies,
			VariableBindings varBindings, out FeatureValue output)
		{
			FeatureValue copy = Clone();
			copies[this] = copy;
			copies[other] = copy;
			if (!copy.DestructiveUnify(other, useDefaults, true, copies, varBindings))
			{
				output = null;
				return false;
			}
			output = copy;
			return true;
		}

		internal override FeatureValue Clone(IDictionary<FeatureValue, FeatureValue> copies)
		{
			FeatureValue copy;
			if (copies != null)
			{
				if (copies.TryGetValue(this, out copy))
					return copy;
			}

			copy = Clone();

			if (copies != null)
				copies[this] = copy;
			return copy;
		}

		public abstract SimpleFeatureValue Clone();

		internal override bool Negation(out FeatureValue output)
		{
			output = Negation();
			return true;
		}

		public abstract SimpleFeatureValue Negation();

		protected abstract bool Overlaps(bool not, SimpleFeatureValue other, bool notOther);
		protected abstract void IntersectWith(bool not, SimpleFeatureValue other, bool notOther);
		protected abstract void UnionWith(bool not, SimpleFeatureValue other, bool notOther);
		protected abstract void ExceptWith(bool not, SimpleFeatureValue other, bool notOther);
		protected abstract bool IsSatisfiable { get; }
		protected abstract bool IsUninstantiated { get; }
	}
}
