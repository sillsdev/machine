using System.Collections.Generic;

namespace SIL.APRE.FeatureModel
{
	public abstract class SimpleFeatureValue : FeatureValue
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
				FeatureValue binding;
				if (varBindings.TryGetValue(VariableName, out binding))
				{

					var bindingSfv = (SimpleFeatureValue) binding;
					if (!bindingSfv.Overlaps(!Agree, otherSfv, false))
						return false;
				}
				else
				{
					if (Agree)
						binding = otherSfv.Clone();
					else
						otherSfv.Negation(out binding);
					varBindings[VariableName] = binding;
				}
			}
			else if (!IsVariable && otherSfv.IsVariable)
			{
				FeatureValue binding;
				if (varBindings.TryGetValue(otherSfv.VariableName, out binding))
				{
					var bindingSfv = (SimpleFeatureValue) binding;
					if (!Overlaps(false, bindingSfv, !otherSfv.Agree))
						return false;
				}
				else
				{
					if (otherSfv.Agree)
						binding = Clone();
					else
						Negation(out binding);
					varBindings[otherSfv.VariableName] = binding;
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
				FeatureValue binding;
				if (varBindings.TryGetValue(VariableName, out binding))
				{
					var bindingSfv = (SimpleFeatureValue) binding;
					if (!bindingSfv.Overlaps(!Agree, otherSfv, false))
						return false;
					UnionWith(false, bindingSfv, !Agree);
					IntersectWith(false, otherSfv, false);
				}
				else
				{
					UnionWith(false, otherSfv, false);
					if (Agree)
						binding = otherSfv.Clone();
					else
						otherSfv.Negation(out binding);
					varBindings[VariableName] = binding;
				}
				VariableName = null;
			}
			else if (!IsVariable && otherSfv.IsVariable)
			{
				FeatureValue binding;
				if (varBindings.TryGetValue(otherSfv.VariableName, out binding))
				{
					var bindingSfv = (SimpleFeatureValue) binding;
					if (!Overlaps(false, bindingSfv, !otherSfv.Agree))
						return false;
					IntersectWith(false, bindingSfv, !otherSfv.Agree);
				}
				else
				{
					if (otherSfv.Agree)
						binding = Clone();
					else
						Negation(out binding);
					varBindings[otherSfv.VariableName] = binding;
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
				FeatureValue binding;
				if (varBindings.TryGetValue(VariableName, out binding))
				{
					var bindingSfv = (SimpleFeatureValue) binding;
					UnionWith(false, bindingSfv, !Agree);
					UnionWith(false, otherSfv, false);
				}
				else
				{
					UnionWith(false, otherSfv, false);
					if (Agree)
						binding = otherSfv.Clone();
					else
						otherSfv.Negation(out binding);
					varBindings[VariableName] = binding;
				}
				VariableName = null;
			}
			else if (!IsVariable && otherSfv.IsVariable)
			{
				FeatureValue binding;
				if (varBindings.TryGetValue(otherSfv.VariableName, out binding))
				{
					var bindingSfv = (SimpleFeatureValue) binding;
					UnionWith(false, bindingSfv, !otherSfv.Agree);
				}
				else
				{
					if (otherSfv.Agree)
						binding = Clone();
					else
						Negation(out binding);
					varBindings[otherSfv.VariableName] = binding;
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
				FeatureValue binding;
				if (varBindings.TryGetValue(VariableName, out binding))
				{
					var bindingSfv = (SimpleFeatureValue) binding;
					UnionWith(false, bindingSfv, !Agree);
					ExceptWith(false, otherSfv, false);
					VariableName = null;
				}
			}
			else if (!IsVariable && otherSfv.IsVariable)
			{
				FeatureValue binding;
				if (varBindings.TryGetValue(otherSfv.VariableName, out binding))
				{
					var bindingSfv = (SimpleFeatureValue) binding;
					ExceptWith(false, bindingSfv, !otherSfv.Agree);
				}
			}

			return IsSatisfiable;
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
			if (copies.TryGetValue(this, out copy))
				return copy;

			copy = Clone();
			copies[this] = copy;
			return copy;
		}

		protected abstract bool Overlaps(bool not, SimpleFeatureValue other, bool notOther);
		protected abstract void IntersectWith(bool not, SimpleFeatureValue other, bool notOther);
		protected abstract void UnionWith(bool not, SimpleFeatureValue other, bool notOther);
		protected abstract void ExceptWith(bool not, SimpleFeatureValue other, bool notOther);
		protected abstract bool IsSatisfiable { get; }
		protected abstract bool IsUninstantiated { get; }
	}
}
