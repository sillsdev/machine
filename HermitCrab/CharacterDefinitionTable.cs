using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SIL.APRE;
using SIL.APRE.FeatureModel;
using SIL.APRE.Matching;

namespace SIL.HermitCrab
{
    /// <summary>
    /// This class represents a character definition table. It encapsulates the mappings of
    /// characters to phonetic segments.
    /// </summary>
    public class CharacterDefinitionTable : IDBearerBase
    {
        private readonly Dictionary<string, SegmentDefinition> _segDefs;
        private readonly Dictionary<string, BoundaryDefinition> _bdryDefs;
    	private readonly SpanFactory<PhoneticShapeNode> _spanFactory;

    	/// <summary>
    	/// Initializes a new instance of the <see cref="CharacterDefinitionTable"/> class.
    	/// </summary>
    	/// <param name="id">The ID.</param>
    	/// <param name="desc">The description.</param>
    	/// <param name="spanFactory"></param>
		public CharacterDefinitionTable(string id, string desc, SpanFactory<PhoneticShapeNode> spanFactory)
			: base(id, desc)
    	{
    		_spanFactory = spanFactory;
    		_segDefs = new Dictionary<string, SegmentDefinition>();
    		_bdryDefs = new Dictionary<string, BoundaryDefinition>();
    	}

    	public SpanFactory<PhoneticShapeNode> SpanFactory
    	{
    		get { return _spanFactory; }
    	}

    	/// <summary>
    	/// Adds the segment definition.
    	/// </summary>
    	/// <param name="strRep"></param>
    	/// <param name="fs"></param>
    	public virtual void AddSegmentDefinition(string strRep, FeatureStruct fs)
        {
            var segDef = new SegmentDefinition(strRep, this, fs);
            // what do we do about culture?
            _segDefs[strRep.ToLowerInvariant()] = segDef;
        }

        /// <summary>
        /// Adds the boundary definition.
        /// </summary>
        /// <param name="strRep">The string representation.</param>
        public void AddBoundaryDefinition(string strRep)
        {
            _bdryDefs[strRep] = new BoundaryDefinition(strRep, this);
        }

        /// <summary>
        /// Gets the segment definition for the specified string representation.
        /// </summary>
        /// <param name="strRep">The string representation.</param>
        /// <returns>The segment definition.</returns>
        public virtual SegmentDefinition GetSegmentDefinition(string strRep)
        {
            SegmentDefinition segDef;
            // what do we do about culture?
            if (_segDefs.TryGetValue(strRep.ToLowerInvariant(), out segDef))
                return segDef;
            return null;
        }

        public BoundaryDefinition GetBoundaryDefinition(string strRep)
        {
            BoundaryDefinition bdryDef;
            if (_bdryDefs.TryGetValue(strRep, out bdryDef))
                return bdryDef;
            return null;
        }

        /// <summary>
        /// Gets all of the string representations that match the specified segment.
        /// </summary>
        /// <param name="node">The phonetic shape node.</param>
        /// <param name="mode">The mode.</param>
        /// <returns>The string representations.</returns>
        public IEnumerable<SegmentDefinition> GetMatchingSegmentDefinitions(PhoneticShapeNode node, ModeType mode)
        {
            var results = new List<SegmentDefinition>();

            foreach (SegmentDefinition segDef in _segDefs.Values)
            {
				if (segDef.FeatureStruct.IsUnifiable(node.Annotation.FeatureStruct))
					results.Add(segDef);
            }

            return results;
        }

        /// <summary>
        /// Converts the specified string to a phonetic shape. It matches the longest possible segment
        /// first.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="mode">The mode.</param>
        /// <returns>The phonetic shape, <c>null</c> if the string contains invalid segments.</returns>
        public PhoneticShape ToPhoneticShape(string str, ModeType mode)
        {
            var ps = new PhoneticShape(this, mode);
            int i = 0;
            while (i < str.Length)
            {
                bool match = false;
                for (int j = str.Length - i; j > 0; j--)
                {
                    string s = str.Substring(i, j);
                    PhoneticShapeNode node = GetPhoneticShapeNode(s);
                    if (node != null)
                    {
                        ps.Add(node);
                        i += j;
                        match = true;
                        break;
                    }
                }

                if (!match)
                    return null;
            }
            return ps;
        }

        private PhoneticShapeNode GetPhoneticShapeNode(string strRep)
        {
            PhoneticShapeNode node = null;
            SegmentDefinition segDef = GetSegmentDefinition(strRep);
            if (segDef != null)
            {
				node = new PhoneticShapeNode(HCFeatureSystem.SegmentType, _spanFactory, (FeatureStruct) segDef.FeatureStruct.Clone());
            }
            else
            {
                BoundaryDefinition bdryDef = GetBoundaryDefinition(strRep);
				if (bdryDef != null)
				{
					node = new PhoneticShapeNode(HCFeatureSystem.BoundaryType, _spanFactory, (FeatureStruct) bdryDef.FeatureStruct.Clone());
					node.Annotation.Optional = true;
				}
            }
            return node;
        }

        /// <summary>
        /// Converts the specified phonetic shape to a valid regular expression string. Regular expressions
		/// formatted for display purposes are NOT guaranteed to compile.
        /// </summary>
        /// <param name="shape">The phonetic shape.</param>
        /// <param name="mode">The mode.</param>
        /// <param name="displayFormat">if <c>true</c> the result will be formatted for display, otherwise
        /// it will be formatted for compilation.</param>
        /// <returns>The regular expression string.</returns>
        public string ToRegexString(PhoneticShape shape, ModeType mode, bool displayFormat)
        {
            var sb = new StringBuilder();
			if (!displayFormat)
				sb.Append("^");
            foreach (PhoneticShapeNode node in shape)
            {
				if (node.Annotation.Type == HCFeatureSystem.SegmentType)
				{
					SegmentDefinition[] segDefs = GetMatchingSegmentDefinitions(node, mode).ToArray();
					int numSegDefs = segDefs.Length;
					if (numSegDefs > 0)
					{
						if (numSegDefs > 1)
							sb.Append(displayFormat ? "[" : "(");
						int i = 0;
						foreach (SegmentDefinition segDef in segDefs)
						{
							if (segDef.StrRep.Length > 1)
								sb.Append("(");

							sb.Append(displayFormat ? segDef.StrRep : Regex.Escape(segDef.StrRep));

							if (segDef.StrRep.Length > 1)
								sb.Append(")");
							if (i < numSegDefs - 1 && !displayFormat)
								sb.Append("|");
							i++;
						}
						if (segDefs.Length > 1)
							sb.Append(displayFormat ? "]" : ")");

						if (node.Annotation.Optional)
							sb.Append("?");
					}
				}
				else
				{
					var value = node.Annotation.FeatureStruct.GetValue<StringFeatureValue>("strRep");
					string strRep = value.Values.First();
					if (strRep.Length > 1)
						sb.Append("(");

					sb.Append(displayFormat ? strRep : Regex.Escape(strRep));

					if (strRep.Length > 1)
						sb.Append(")");
					sb.Append("?");
				}
            }
			if (!displayFormat)
        		sb.Append("$");
            return sb.ToString();
        }

        /// <summary>
        /// Generates a string representation of the specified phonetic shape.
        /// </summary>
        /// <param name="shape">The phonetic shape.</param>
        /// <param name="mode">The mode.</param>
        /// <param name="includeBdry">if <c>true</c> boundary markers will be included in the
        /// string representation.</param>
        /// <returns>The string representation.</returns>
        public string ToString(PhoneticShape shape, ModeType mode, bool includeBdry)
        {
            var sb = new StringBuilder();
            foreach (PhoneticShapeNode node in shape)
            {
				if (node.Annotation.Type == HCFeatureSystem.SegmentType)
				{
					IEnumerable<SegmentDefinition> segDefs = GetMatchingSegmentDefinitions(node, mode);
					SegmentDefinition segDef = segDefs.FirstOrDefault();
					if (segDef != null)
						sb.Append(segDef.StrRep);
				}
				else if (includeBdry)
				{
					var value = node.Annotation.FeatureStruct.GetValue<StringFeatureValue>("strRep");
					string strRep = value.Values.First();
					sb.Append(strRep);
				}
            }
            return sb.ToString();
        }

        /// <summary>
        /// Determines whether the specified word matches the specified phonetic shape.
        /// </summary>
        /// <param name="word">The word.</param>
        /// <param name="shape">The phonetic shape.</param>
        /// <returns>
        /// 	<c>true</c> if the word matches the shape, otherwise <c>false</c>.
        /// </returns>
        public virtual bool IsMatch(string word, PhoneticShape shape)
        {
            string pattern = ToRegexString(shape, ModeType.Synthesis, false);
            return Regex.IsMatch(word, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        public void Reset()
        {
            _segDefs.Clear();
            _bdryDefs.Clear();
        }
    }
}
