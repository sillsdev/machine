using System.Collections;
using System.Collections.Generic;
using SIL.Collections;

namespace SIL.Machine.FeatureModel
{
	public class PossibleSymbolCollection : IKeyedReadOnlyCollection<string, FeatureSymbol>
	{
		private readonly IDBearerSet<FeatureSymbol> _symbols; 

		internal PossibleSymbolCollection(IEnumerable<FeatureSymbol> symbols)
		{
			_symbols = new IDBearerSet<FeatureSymbol>(symbols);
		}

		public IEnumerator<FeatureSymbol> GetEnumerator()
		{
			return ((IEnumerable<FeatureSymbol>) _symbols).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public bool TryGetValue(string id, out FeatureSymbol value)
		{
			return _symbols.TryGetValue(id, out value);
		}

		public FeatureSymbol this[string id]
		{
			get { return _symbols[id]; }
		}

		public bool Contains(string id)
		{
			return _symbols.Contains(id);
		}

		public bool Contains(FeatureSymbol symbol)
		{
			return _symbols.Contains(symbol);
		}

		public int Count
		{
			get { return _symbols.Count; }
		}
	}
}
