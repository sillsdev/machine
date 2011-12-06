using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SIL.Machine;
using SIL.Machine.FeatureModel;

namespace SIL.HermitCrab
{
	public class SymbolDefinitionTable : IDBearerBase
	{
		private readonly Dictionary<string, SymbolDefinition> _symDefs;
		private readonly SpanFactory<ShapeNode> _spanFactory;

		public SymbolDefinitionTable(string id, SpanFactory<ShapeNode> spanFactory)
			: base(id)
		{
			_spanFactory = spanFactory;
			_symDefs = new Dictionary<string, SymbolDefinition>();
		}

		public SpanFactory<ShapeNode> SpanFactory
		{
			get { return _spanFactory; }
		}

		public IEnumerable<SymbolDefinition> SymbolDefinitions
		{
			get { return _symDefs.Values; }
		}

		/// <summary>
		/// Adds the segment definition.
		/// </summary>
		/// <param name="strRep"></param>
		/// <param name="type"></param>
		/// <param name="fs"></param>
		public void AddSymbolDefinition(string strRep, string type, FeatureStruct fs)
		{
			var segDef = new SymbolDefinition(strRep, type, fs);
			// what do we do about culture?
			_symDefs[strRep.ToLowerInvariant()] = segDef;
		}

		/// <summary>
		/// Gets the segment definition for the specified string representation.
		/// </summary>
		/// <param name="strRep">The string representation.</param>
		/// <param name="symDef">The symbol definition.</param>
		/// <returns></returns>
		public bool TryGetSymbolDefinition(string strRep, out SymbolDefinition symDef)
		{
			// what do we do about culture?
			return _symDefs.TryGetValue(strRep.ToLowerInvariant(), out symDef);
		}

		public SymbolDefinition GetSymbolDefinition(string strRep)
		{
			return _symDefs[strRep.ToLowerInvariant()];
		}

		/// <summary>
		/// Gets all of the string representations that match the specified segment.
		/// </summary>
		/// <param name="node">The phonetic shape node.</param>
		/// <returns>The string representations.</returns>
		protected virtual IEnumerable<string> GetMatchingStrReps(ShapeNode node)
		{
			foreach (SymbolDefinition symDef in _symDefs.Values)
			{
				if (node.Annotation.Type == symDef.Type && symDef.FeatureStruct.IsUnifiable(node.Annotation.FeatureStruct))
					yield return symDef.StrRep;
			}
		}

		protected virtual bool GetShapeNodes(string str, out IEnumerable<ShapeNode> nodes)
		{
			var nodesList = new List<ShapeNode>();
			int i = 0;
			while (i < str.Length)
			{
				bool match = false;
				for (int j = str.Length - i; j > 0; j--)
				{
					string s = str.Substring(i, j);
					SymbolDefinition symDef;
					if (TryGetSymbolDefinition(s, out symDef))
					{
						var node = new ShapeNode(SpanFactory, symDef.Type, symDef.FeatureStruct.Clone());
						node.Annotation.Optional = symDef.Type == HCFeatureSystem.BoundaryType;
						nodesList.Add(node);
						i += j;
						match = true;
						break;
					}
				}

				if (!match)
				{
					nodes = null;
					return false;
				}
			}
			nodes = nodesList;
			return true;
		}

		/// <summary>
		/// Converts the specified string to a phonetic shape. It matches the longest possible segment
		/// first.
		/// </summary>
		/// <param name="str">The string.</param>
		/// <param name="shape"></param>
		/// <returns>The phonetic shape, <c>null</c> if the string contains invalid segments.</returns>
		public bool ToShape(string str, out Shape shape)
		{
			IEnumerable<ShapeNode> nodes;
			if (GetShapeNodes(str, out nodes))
			{
				shape = new Shape(_spanFactory, CreateBegin(), CreateEnd());
				shape.AddRange(nodes);
				return true;
			}

			shape = null;
			return false;
		}

		private ShapeNode CreateBegin()
		{
			return new ShapeNode(_spanFactory, HCFeatureSystem.AnchorType,
				FeatureStruct.New(HCFeatureSystem.Instance).Symbol(HCFeatureSystem.LeftSide).Value);
		}

		private ShapeNode CreateEnd()
		{
			return new ShapeNode(_spanFactory, HCFeatureSystem.AnchorType,
				FeatureStruct.New(HCFeatureSystem.Instance).Symbol(HCFeatureSystem.RightSide).Value);
		}

		/// <summary>
		/// Converts the specified phonetic shape to a valid regular expression string. Regular expressions
		/// formatted for display purposes are NOT guaranteed to compile.
		/// </summary>
		/// <param name="shape">The phonetic shape.</param>
		/// <param name="displayFormat">if <c>true</c> the result will be formatted for display, otherwise
		/// it will be formatted for compilation.</param>
		/// <returns>The regular expression string.</returns>
		public string ToRegexString(Shape shape, bool displayFormat)
		{
			var sb = new StringBuilder();
			if (!displayFormat)
				sb.Append("^");
			foreach (ShapeNode node in shape)
			{
				string[] strReps = GetMatchingStrReps(node).ToArray();
				int strRepCount = strReps.Length;
				if (strRepCount > 0)
				{
					if (strRepCount > 1)
						sb.Append(displayFormat ? "[" : "(");
					int i = 0;
					foreach (string strRep in strReps)
					{
						if (strRep.Length > 1)
							sb.Append("(");

						sb.Append(displayFormat ? strRep : Regex.Escape(strRep));

						if (strRep.Length > 1)
							sb.Append(")");
						if (i < strRepCount - 1 && !displayFormat)
							sb.Append("|");
						i++;
					}
					if (strReps.Length > 1)
						sb.Append(displayFormat ? "]" : ")");

					if (node.Annotation.Optional)
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
		/// <param name="includeBdry">if <c>true</c> boundary markers will be included in the
		/// string representation.</param>
		/// <returns>The string representation.</returns>
		public string ToString(Shape shape, bool includeBdry)
		{
			var sb = new StringBuilder();
			foreach (ShapeNode node in shape)
			{
				IEnumerable<string> strReps = GetMatchingStrReps(node);
				string strRep = strReps.FirstOrDefault();
				if (strRep != null)
					sb.Append(strRep);
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
		public bool IsMatch(string word, Shape shape)
		{
			string pattern = ToRegexString(shape, false);
			return Regex.IsMatch(word, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		}

		public void Reset()
		{
			_symDefs.Clear();
		}
	}
}
