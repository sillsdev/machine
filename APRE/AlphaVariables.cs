using System.Collections.Generic;
using System.Linq;

namespace SIL.APRE
{
    /// <summary>
    /// This class represents a set of alpha variables. An alpha variable is a variable for a feature value.
    /// </summary>
    public class AlphaVariables<TOffset>
    {
        private readonly IDictionary<string, IEnumerable<Feature>> _varFeatures;

        /// <summary>
		/// Initializes a new instance of the <see cref="AlphaVariables&lt;TOffset&gt;"/> class.
        /// </summary>
        /// <param name="varFeatures">The varFeats of variables and features.</param>
        public AlphaVariables(IDictionary<string, IEnumerable<Feature>> varFeatures)
        {
            _varFeatures = varFeatures;
        }

        /// <summary>
        /// Gets the variables.
        /// </summary>
        /// <value>The variables.</value>
        public IEnumerable<string> Variables
        {
            get
            {
                return _varFeatures.Keys;
            }
        }

        /// <summary>
        /// Gets the feature associated with the specified variable.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <returns>The feature.</returns>
        public IEnumerable<Feature> GetFeaturePath(string variable)
        {
            IEnumerable<Feature> path;
            if (_varFeatures.TryGetValue(variable, out path))
                return path;
            return null;
        }

    	/// <summary>
    	/// Gets a valid binding between the specified variables and the feature values
    	/// currently set on the specified segment. It adds the variable values to the varFeats of
    	/// instantiated variables.
    	/// </summary>
    	/// <param name="variables">The variables.</param>
    	/// <param name="ann">The annotation.</param>
    	/// <param name="varValues">The variable values.</param>
    	/// <returns><c>true</c> if a valid binding was found, otherwise <c>false</c></returns>
    	public bool GetBinding(IDictionary<string, bool> variables, Annotation<TOffset> ann, FeatureStructure varValues)
    	{
    		return variables.All(varPolarity => GetBinding(varPolarity.Key, varPolarity.Value, ann, varValues));
    	}

    	/// <summary>
    	/// Gets a valid binding between the specified variable and the feature values
    	/// currently set on the specified segment. It adds the variable value to the varFeats of
    	/// instantiated variables.
    	/// </summary>
    	/// <param name="variable">The variable.</param>
    	/// <param name="polarity">The variable polarity.</param>
    	/// <param name="ann">The annotation.</param>
    	/// <param name="varValues">The variable values.</param>
    	/// <returns><c>true</c> if a valid binding was found, otherwise <c>false</c></returns>
		public bool GetBinding(string variable, bool polarity, Annotation<TOffset> ann, FeatureStructure varValues)
    	{
    		IEnumerable<Feature> path = _varFeatures[variable];
			FeatureValue annValue = ann.FeatureStructure.GetValue(path);
    		FeatureValue varValue = varValues.GetValue(path);
			foreach (FeatureValue value in GetAgreeValues(path, varValue))
            {
				if (annValue == null || (polarity ? annValue.IsUnifiable(value)
					: GetDisagreeValues(path, value).Any(disagree => annValue.IsUnifiable(disagree))))
				{
					if (varValue == null)
					{
						varValues.Add(path, value);
					}
					else
					{
						varValue.Instantiate(value);
						varValues.Add(path, varValue);
					}
					return true;
				}
            }

            return false;
        }

    	/// <summary>
    	/// Gets all valid bindings between the specified variables and the feature values
    	/// currently set on the specified segment. It adds the variable values to the varFeats of
    	/// instantiated variables.
    	/// </summary>
    	/// <param name="variables">The variables.</param>
    	/// <param name="ann">The annotation.</param>
    	/// <param name="varValues">The variable values.</param>
    	/// <returns><c>true</c> if a valid binding was found, otherwise <c>false</c></returns>
		public bool GetAllBindings(IDictionary<string, bool> variables, Annotation<TOffset> ann, FeatureStructure varValues)
    	{
    		var allBindings = new Dictionary<string, FeatureValue>();
            foreach (KeyValuePair<string, bool> varPolarity in variables)
            {
				IEnumerable<Feature> path = _varFeatures[varPolarity.Key];
				FeatureValue annValue = ann.FeatureStructure.GetValue(path);
            	FeatureValue varValue = varValues.GetValue(path);
            	bool match = false;
                foreach (FeatureValue value in GetAgreeValues(path, varValue))
                {
                    if (annValue == null || (varPolarity.Value ? annValue.IsUnifiable(value)
						: GetDisagreeValues(path, value).Any(disagree => annValue.IsUnifiable(disagree))))
                    {
						if (varValue == null)
							varValue = value;
						else
							varValue.Uninstantiate(value);
                    	match = true;
                    }
                }
				if (!match)
					return false;

            	allBindings[varPolarity.Key] = varValue;
            }

			foreach (KeyValuePair<string, FeatureValue> binding in allBindings)
				varValues.Add(_varFeatures[binding.Key], binding.Value);

            return true;
        }

		public void Instantiate(Annotation<TOffset> ann, IDictionary<string, bool> variables, FeatureStructure varValues)
		{
			ann.FeatureStructure.Instantiate(GetVariableValues(variables, varValues));
		}

		public void Uninstantiate(Annotation<TOffset> ann, IDictionary<string, bool> variables, FeatureStructure varValues)
		{
			ann.FeatureStructure.Uninstantiate(GetVariableValues(variables, varValues));
		}

		private FeatureStructure GetVariableValues(IDictionary<string, bool> variables, FeatureStructure varValues)
		{
			var fs = varValues;
			bool newFs = false;
			foreach (KeyValuePair<string, bool> varPolarity in variables.Where(kvp => !kvp.Value))
			{
				if (!newFs)
				{
					fs = (FeatureStructure)varValues.Clone();
					newFs = true;
				}
				IEnumerable<Feature> path = _varFeatures[varPolarity.Key];
				fs.Add(path, GetDisagreeValues(path, varValues.GetValue(path)).First());
			}
			return fs;
		}

        /// <summary>
        /// Enumerates thru all of the possible values for the specified variable.
        /// </summary>
        /// <param name="path">The variable.</param>
        /// <param name="varValue">The variable values.</param>
        /// <returns>An enumerable of feature values.</returns>
		private static IEnumerable<FeatureValue> GetAgreeValues(IEnumerable<Feature> path, FeatureValue varValue)
        {
			Feature f = path.Last();
			switch (f.ValueType)
			{
				case FeatureValueType.String:
					// TODO: how do we handle strings?
					break;

				case FeatureValueType.Symbol:
					var symbolicValue = (SymbolicFeatureValue) varValue;
					if (symbolicValue.Values.Any())
					{
						// variable is instantiated, so only check already instantiated values
						foreach (FeatureSymbol symbol in symbolicValue.Values)
							yield return new SymbolicFeatureValue(symbol);
					}
					else
					{
						var symbolicFeat = (SymbolicFeature) f;
						foreach (FeatureSymbol symbol in symbolicFeat.PossibleSymbols)
							yield return new SymbolicFeatureValue(symbol);
					}
					break;

				case FeatureValueType.Complex:
					// this should not be allowed
					break;
			}
        }

		private static IEnumerable<FeatureValue> GetDisagreeValues(IEnumerable<Feature> path, FeatureValue varValue)
		{
			Feature f = path.Last();
			switch (f.ValueType)
			{
				case FeatureValueType.String:
					// TODO: how do we handle strings?
					break;

				case FeatureValueType.Symbol:
					var symbolicFeat = (SymbolicFeature)f;
					foreach (FeatureSymbol symbol in symbolicFeat.PossibleSymbols)
					{
						var newValue = new SymbolicFeatureValue(symbol);
						if (!varValue.Equals(newValue))
							yield return newValue;
					}
					break;

				case FeatureValueType.Complex:
					// this should not be allowed
					break;
			}
		}
    }
}
