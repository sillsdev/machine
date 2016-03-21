using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;

namespace SIL.HermitCrab
{
	public class SymbolTable : IEnumerable<string>
	{
		private readonly Dictionary<string, FeatureStruct> _symbols;
		private readonly SpanFactory<ShapeNode> _spanFactory;

		public SymbolTable(SpanFactory<ShapeNode> spanFactory)
		{
			_spanFactory = spanFactory;
			_symbols = new Dictionary<string, FeatureStruct>();
		}

		public string Name { get; set; }

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

		public bool Contains(string strRep)
		{
			return _symbols.ContainsKey(strRep);
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
		public IEnumerable<string> GetMatchingStrReps(ShapeNode node)
		{
			foreach (KeyValuePair<string, FeatureStruct> symbol in _symbols)
			{
				if (symbol.Value.IsUnifiable(node.Annotation.FeatureStruct))
					yield return symbol.Key;
			}
		}

		private bool GetShapeNodes(string str, out IEnumerable<ShapeNode> nodes, out int errorPos)
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
					errorPos = i;
					return false;
				}
			}
			nodes = nodesList;
			errorPos = -1;
			return true;
		}

		public Shape Segment(string str)
		{
			IEnumerable<ShapeNode> nodes;
			int errorPos;
			if (GetShapeNodes(str, out nodes, out errorPos))
			{
				var shape = new Shape(_spanFactory, begin => new ShapeNode(_spanFactory, begin ? HCFeatureSystem.LeftSideAnchor : HCFeatureSystem.RightSideAnchor));
				shape.AddRange(nodes);
				return shape;
			}

			throw new InvalidShapeException(str, errorPos);
		}

		/// <summary>
		/// Converts the specified string to a phonetic shape. It matches the longest possible segment
		/// first. If the string segmented successfully, this will return -1, otherwise it will return
		/// the position where the error occurred.
		/// </summary>
		/// <param name="str">The string.</param>
		/// <param name="shape">The shape.</param>
		public int TrySegment(string str, out Shape shape)
		{
			IEnumerable<ShapeNode> nodes;
			int errorPos;
			if (GetShapeNodes(str, out nodes, out errorPos))
			{
				shape = new Shape(_spanFactory, begin => new ShapeNode(_spanFactory, begin ? HCFeatureSystem.LeftSideAnchor : HCFeatureSystem.RightSideAnchor));
				shape.AddRange(nodes);
				return errorPos;
			}

			shape = null;
			return errorPos;
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
			string pattern = shape.ToRegexString(this, false);
			return Regex.IsMatch(word, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		}

		public override string ToString()
		{
			return string.IsNullOrEmpty(Name) ? base.ToString() : Name;
		}
	}
}
