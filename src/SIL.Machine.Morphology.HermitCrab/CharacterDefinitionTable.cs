using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SIL.Extensions;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;

namespace SIL.Machine.Morphology.HermitCrab
{
    public class CharacterDefinitionTable : ICollection<CharacterDefinition>
    {
        private readonly Dictionary<string, CharacterDefinition> _charDefLookup;
        private readonly HashSet<CharacterDefinition> _charDefs;

        public CharacterDefinitionTable()
        {
            _charDefLookup = new Dictionary<string, CharacterDefinition>();
            _charDefs = new HashSet<CharacterDefinition>();
        }

        public string Name { get; set; }

        public CharacterDefinition AddSegment(string strRep, FeatureStruct fs = null)
        {
            return AddSegment(strRep.ToEnumerable(), fs);
        }

        public CharacterDefinition AddBoundary(string strRep)
        {
            return AddBoundary(strRep.ToEnumerable());
        }

        public CharacterDefinition AddSegment(IEnumerable<string> strRep, FeatureStruct fs = null)
        {
            return Add(strRep, HCFeatureSystem.Segment, fs);
        }

        public CharacterDefinition AddBoundary(IEnumerable<string> strRep)
        {
            return Add(strRep, HCFeatureSystem.Boundary, null);
        }

        /// <summary>
        /// Adds the character definition.
        /// </summary>
        private CharacterDefinition Add(IEnumerable<string> strReps, FeatureSymbol type, FeatureStruct fs)
        {
            string[] strRepsArray = strReps.ToArray();
            string[] normalizedStrRepsArray = strRepsArray.Select(s => s.Normalize(NormalizationForm.FormD)).ToArray();
            if (normalizedStrRepsArray.Any(s => _charDefLookup.ContainsKey(s)))
            {
                throw new ArgumentException(
                    "The table already contains a character definition with one of the specified representations.",
                    "strReps"
                );
            }

            if (fs == null)
            {
                fs = FeatureStruct
                    .New()
                    .Symbol(type)
                    .Feature(HCFeatureSystem.StrRep)
                    .EqualTo(normalizedStrRepsArray)
                    .Value;
            }
            else
            {
                fs.AddValue(HCFeatureSystem.Type, type);
                fs.Freeze();
            }

            var cd = new CharacterDefinition(strRepsArray, fs);
            _charDefs.Add(cd);
            foreach (string rep in normalizedStrRepsArray)
                _charDefLookup[rep] = cd;
            cd.CharacterDefinitionTable = this;
            return cd;
        }

        /// <summary>
        /// Gets all of the string representations that match the specified segment.
        /// </summary>
        /// <param name="node">The phonetic shape node.</param>
        /// <returns>The string representations.</returns>
        public IEnumerable<string> GetMatchingStrReps(ShapeNode node)
        {
            foreach (CharacterDefinition cd in this)
            {
                if (cd.FeatureStruct.IsUnifiable(node.Annotation.FeatureStruct))
                {
                    foreach (string representation in cd.Representations)
                        yield return representation;
                }
            }
        }

        private bool GetShapeNodes(string str, out IEnumerable<ShapeNode> nodes, out int errorPos)
        {
            var nodesList = new List<ShapeNode>();
            int i = 0;
            string normalized = str.Normalize(NormalizationForm.FormD);
            while (i < normalized.Length)
            {
                bool match = false;
                for (int j = normalized.Length - i; j > 0; j--)
                {
                    string s = normalized.Substring(i, j);
                    CharacterDefinition cd;
                    if (_charDefLookup.TryGetValue(s, out cd))
                    {
                        var node = new ShapeNode(cd.FeatureStruct.Clone());
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
                    if (!str.IsNormalized(NormalizationForm.FormD))
                        errorPos = normalized.Substring(0, errorPos).Normalize().Length;
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
                var shape = new Shape(
                    begin => new ShapeNode(begin ? HCFeatureSystem.LeftSideAnchor : HCFeatureSystem.RightSideAnchor)
                );
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
                shape = new Shape(
                    begin => new ShapeNode(begin ? HCFeatureSystem.LeftSideAnchor : HCFeatureSystem.RightSideAnchor)
                );
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
            return Regex.IsMatch(
                word.Normalize(NormalizationForm.FormD),
                pattern.Normalize(NormalizationForm.FormD),
                RegexOptions.CultureInvariant
            );
        }

        public IEnumerator<CharacterDefinition> GetEnumerator()
        {
            return _charDefs.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void ICollection<CharacterDefinition>.Add(CharacterDefinition item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            foreach (CharacterDefinition cd in _charDefs)
                cd.CharacterDefinitionTable = null;
            _charDefs.Clear();
            _charDefLookup.Clear();
        }

        public bool Contains(CharacterDefinition item)
        {
            return _charDefs.Contains(item);
        }

        public void CopyTo(CharacterDefinition[] array, int arrayIndex)
        {
            _charDefs.CopyTo(array, arrayIndex);
        }

        public bool Remove(CharacterDefinition item)
        {
            if (_charDefs.Remove(item))
            {
                foreach (string rep in item.Representations)
                    _charDefLookup.Remove(rep.Normalize(NormalizationForm.FormD));
                item.CharacterDefinitionTable = null;
                return true;
            }
            return false;
        }

        public int Count
        {
            get { return _charDefs.Count; }
        }

        bool ICollection<CharacterDefinition>.IsReadOnly
        {
            get { return false; }
        }

        public bool TryGetValue(string key, out CharacterDefinition value)
        {
            return _charDefLookup.TryGetValue(key.Normalize(NormalizationForm.FormD), out value);
        }

        public bool Contains(string key)
        {
            return _charDefLookup.ContainsKey(key.Normalize(NormalizationForm.FormD));
        }

        public CharacterDefinition this[string key]
        {
            get { return _charDefLookup[key.Normalize(NormalizationForm.FormD)]; }
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(Name) ? base.ToString() : Name;
        }
    }
}
