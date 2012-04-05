using SIL.Collections;

namespace SIL.Machine.FeatureModel
{
	public class PossibleSymbolCollection : IDBearerSet<FeatureSymbol>
	{
		private readonly SymbolicFeature _feature;

		internal PossibleSymbolCollection(SymbolicFeature feature)
		{
			_feature = feature;
		}

		public bool Add(string symbolID)
		{
			return Add(symbolID, false);
		}

		public bool Add(string symbolID, bool defaultValue)
		{
			return Add(new FeatureSymbol(symbolID), defaultValue);
		}

		public bool Add(string symbolID, string desc)
		{
			return Add(symbolID, desc, false);
		}

		public bool Add(string symbolID, string desc, bool defaultValue)
		{
			return Add(new FeatureSymbol(symbolID) { Description = desc }, defaultValue);
		}

		public bool Add(FeatureSymbol item, bool defaultValue)
		{
			if (Add(item))
			{
				if (defaultValue)
					_feature.DefaultValue = new SymbolicFeatureValue(item);
				return true;
			}
			return false;
		}

		public override bool Add(FeatureSymbol item)
		{
			if (base.Add(item))
			{
				item.Feature = _feature;
				return true;
			}

			return false;
		}

		public override bool Remove(string id)
		{
			FeatureSymbol symbol;
			if (TryGetValue(id, out symbol))
			{
				base.Remove(id);
				symbol.Feature = null;
				return true;
			}

			return false;
		}
	}
}
