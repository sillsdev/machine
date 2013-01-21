using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SIL.Collections;
using SIL.Machine;
using SIL.Machine.FeatureModel;

namespace SIL.HermitCrab
{
	public class SymbolTable : IDBearerBase, IEnumerable<string>
	{
		private readonly Dictionary<string, FeatureStruct> _symbols;
		private readonly SpanFactory<ShapeNode> _spanFactory;

		public SymbolTable(SpanFactory<ShapeNode> spanFactory, string id)
			: base(id)
		{
			_spanFactory = spanFactory;
			_symbols = new Dictionary<string, FeatureStruct>();
		}

		public SpanFactory<ShapeNode> SpanFactory
		{
			get { return _spanFactory; }
		}

		/// <summary>
		/// Adds the segment definition.
		/// </summary>
		/// <param name="strRep"></param>
		/// <param name="fs"></param>
		public void Add(string strRep, FeatureStruct fs)
		{
			if (!fs.IsFrozen)
				throw new ArgumentException("The feature structure must be immutable.", "fs");
			_symbols[strRep] = fs;
		}

		public bool Remove(string strRep)
		{
			return _symbols.Remove(strRep);
		}

		public void Clear()
		{
			_symbols.Clear();
		}

		IEnumerator<string> IEnumerable<string>.GetEnumerator()
		{
			return _symbols.Keys.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _symbols.Keys.GetEnumerator();
		}

		/// <summary>
		/// Gets the segment definition for the specified string representation.
		/// </summary>
		/// <param name="strRep">The string representation.</param>
		/// <param name="fs">The symbol definition.</param>
		/// <returns></returns>
		public bool TryGetSymbolFeatureStruct(string strRep, out FeatureStruct fs)
		{
			return _symbols.TryGetValue(strRep, out fs);
		}

		public FeatureStruct GetSymbolFeatureStruct(string strRep)
		{
			return _symbols[strRep];
		}

		/// <summary>
		/// Gets all of the string representations that match the specified segment.
		/// </summary>
		/// <param name="node">The phonetic shape node.</param>
		/// <returns>The string representations.</returns>
		protected virtual IEnumerable<string> GetMatchingStrReps(ShapeNode node)
		{
			foreach (KeyValuePair<string, FeatureStruct> symbol in _symbols)
			{
				if (symbol.Value.IsUnifiable(node.Annotation.FeatureStruct))
					yield return symbol.Key;
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
					FeatureStruct fs;
					if (TryGetSymbolFeatureStruct(s, out fs))
					{
						var node = new ShapeNode(SpanFactory, fs.DeepClone());
						node.Annotation.Optional = node.Annotation.Type() == HCFeatureSystem.Boundary;
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

		public Shape ToShape(string str)
		{
			Shape shape;
			if (ToShape(str, out shape))
				return shape;

			throw new ArgumentException("The string '{0}' cannot be converted to a shape.", "str");
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
				shape = new Shape(_spanFactory, begin => new ShapeNode(_spanFactory, begin ? HCFeatureSystem.LeftSideAnchor : HCFeatureSystem.RightSideAnchor));
				shape.AddRange(nodes);
				return true;
			}

			shape = null;
			return false;
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

		public string ToString(IEnumerable<ShapeNode> nodes, bool includeBdry)
		{
			var sb = new StringBuilder();
			foreach (ShapeNode node in nodes)
			{
				if (!includeBdry && node.Annotation.Type() == HCFeatureSystem.Boundary)
					continue;

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
	}
}
