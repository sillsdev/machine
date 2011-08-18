using System;
using System.Collections.Generic;

namespace SIL.APRE
{
	public class SymbolicFeature : Feature
	{
		private readonly IDBearerSet<FeatureSymbol> _possibleSymbols;

		public SymbolicFeature(string id, string description)
			: base(id, description)
		{
			_possibleSymbols = new IDBearerSet<FeatureSymbol>();
		}

		public SymbolicFeature(string id)
			: this(id, id)
		{
		}

		public override FeatureValueType ValueType
		{
			get { return FeatureValueType.Symbol; }
		}

		/// <summary>
		/// Gets all possible values.
		/// </summary>
		/// <value>All possible values.</value>
		public IEnumerable<FeatureSymbol> PossibleSymbols
		{
			get
			{
				return _possibleSymbols;
			}
		}

		/// <summary>
		/// Adds the value.
		/// </summary>
		/// <param name="symbol">The value.</param>
		public void AddPossibleSymbol(FeatureSymbol symbol)
		{
			symbol.Feature = this;
			_possibleSymbols.Add(symbol);
		}

		/// <summary>
		/// Removes the value associated with the specified ID.
		/// </summary>
		/// <param name="symbol"></param>
		public void RemovePossibleSymbol(FeatureSymbol symbol)
		{
			_possibleSymbols.Remove(symbol);
		}

		/// <summary>
		/// Gets the value associated with the specified ID.
		/// </summary>
		/// <param name="id">The ID.</param>
		/// <returns>The feature value.</returns>
		public FeatureSymbol GetPossibleSymbol(string id)
		{
			try
			{
				return _possibleSymbols[id];
			}
			catch (KeyNotFoundException ex)
			{
				throw new ArgumentException("The specified symbol could not be found.", "id", ex);
			}
		}

		public bool TryGetPossibleSymbol(string id, out FeatureSymbol symbol)
		{
			return _possibleSymbols.TryGetValue(id, out symbol);
		}
	}
}
