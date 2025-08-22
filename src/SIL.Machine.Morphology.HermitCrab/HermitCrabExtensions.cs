using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;
using SIL.ObjectModel;

namespace SIL.Machine.Morphology.HermitCrab
{
    public static class HermitCrabExtensions
    {
        public static FeatureSymbol Type(this ShapeNode node)
        {
            FeatureSymbol fSym = SymbolicFeatureValue.GetFeatureSymbolFromFeatureStruct(
                node.Annotation.FeatureStruct,
                HCFeatureSystem.Type
            );
            return fSym;
        }

        public static FeatureSymbol Type(this Annotation<ShapeNode> ann)
        {
            FeatureSymbol fSym = SymbolicFeatureValue.GetFeatureSymbolFromFeatureStruct(
                ann.FeatureStruct,
                HCFeatureSystem.Type
            );
            return fSym;
        }

        public static FeatureSymbol Type(this Constraint<Word, ShapeNode> constraint)
        {
            FeatureSymbol fSym = SymbolicFeatureValue.GetFeatureSymbolFromFeatureStruct(
                constraint.FeatureStruct,
                HCFeatureSystem.Type
            );
            return fSym;
        }

        internal static FeatureStruct AntiFeatureStruct(this FeatureStruct fs)
        {
            // TODO: handle reentrance properly

            var result = new FeatureStruct();
            foreach (Feature feature in fs.Features)
            {
                FeatureValue value = fs.GetValue(feature);
                FeatureValue newValue;
                if (value is FeatureStruct childFS)
                {
                    newValue = HCFeatureSystem.Instance.ContainsFeature(feature)
                        ? childFS.Clone()
                        : childFS.AntiFeatureStruct();
                }
                else
                {
                    var childSfv = (SimpleFeatureValue)value;
                    newValue = HCFeatureSystem.Instance.ContainsFeature(feature)
                        ? childSfv.Clone()
                        : childSfv.Negation();
                }
                result.AddValue(feature, newValue);
            }
            return result;
        }

        internal static bool IsDirty(this ShapeNode node)
        {
            FeatureSymbol fSym = SymbolicFeatureValue.GetFeatureSymbolFromFeatureStruct(
                node.Annotation.FeatureStruct,
                HCFeatureSystem.Modified
            );
            return fSym == HCFeatureSystem.Dirty;
            //return ((FeatureSymbol)node.Annotation.FeatureStruct.GetValue(HCFeatureSystem.Modified))
            //    == HCFeatureSystem.Dirty;
        }

        internal static void SetDirty(this ShapeNode node, bool dirty)
        {
            node.Annotation.FeatureStruct.AddValue(
                HCFeatureSystem.Modified,
                dirty ? HCFeatureSystem.Dirty : HCFeatureSystem.Clean
            );
        }

        internal static bool IsDeleted(this Annotation<ShapeNode> ann)
        {
            SymbolicFeatureValue sfv;
            if (ann.FeatureStruct.TryGetValue(HCFeatureSystem.Deletion, out sfv))
            {
                FeatureSymbol fSym = SymbolicFeatureValue.GetFeatureSymbolFromFeatureStruct(
                    ann.FeatureStruct,
                    HCFeatureSystem.Deleted.Feature
                );
                return fSym == HCFeatureSystem.Deleted;
            }
            return false;
        }

        internal static bool IsDeleted(this ShapeNode node)
        {
            return node.Annotation.IsDeleted();
        }

        internal static void SetDeleted(this ShapeNode node, bool deleted)
        {
            node.Annotation.FeatureStruct.AddValue(
                HCFeatureSystem.Deletion,
                deleted ? HCFeatureSystem.Deleted : HCFeatureSystem.NotDeleted
            );
        }

        internal static bool IsIterative(this ShapeNode node)
        {
            return node.Annotation.Data != null;
        }

        internal static void SetIterative(this ShapeNode node, bool iterative)
        {
            if (iterative)
                node.Annotation.Data = iterative;
            else
                node.Annotation.Data = null;
        }

        private static readonly IEqualityComparer<ShapeNode> NodeComparer = new ProjectionEqualityComparer<
            ShapeNode,
            FeatureStruct
        >(node => node.Annotation.FeatureStruct, FreezableEqualityComparer<FeatureStruct>.Default);

        internal static bool Duplicates(this Shape x, Shape y)
        {
            return x.Where(n => !n.Annotation.Optional)
                .SequenceEqual(y.Where(n => !n.Annotation.Optional), NodeComparer);
        }

        internal static IEnumerable<Word> RemoveDuplicates(this IEnumerable<Word> words)
        {
            var output = new List<Word>();
            foreach (Word word in words)
            {
                // check to see if this is a duplicate of another output analysis, this is not strictly necessary, but
                // it helps to reduce the search space
                bool add = true;
                for (int i = 0; i < output.Count; i++)
                {
                    if (word.Shape.Duplicates(output[i].Shape))
                    {
                        if (word.Shape.Count > output[i].Shape.Count)
                            // if this is a duplicate and it is longer, then use this analysis and remove the previous one
                            output.RemoveAt(i);
                        else
                            // if it is shorter, then do not add it to the output list
                            add = false;
                        break;
                    }
                }

                if (add)
                    output.Add(word);
            }
            return output;
        }

        internal static IEnumerable<PatternNode<Word, ShapeNode>> DeepCloneExceptBoundaries(
            this IEnumerable<PatternNode<Word, ShapeNode>> nodes
        )
        {
            foreach (PatternNode<Word, ShapeNode> node in nodes)
            {
                if (
                    node is Constraint<Word, ShapeNode> constraint
                    && (constraint.FeatureStruct.IsEmpty || constraint.Type() != HCFeatureSystem.Boundary)
                )
                {
                    yield return constraint.Clone();
                    continue;
                }

                if (node is Alternation<Word, ShapeNode> alternation)
                {
                    var newAlteration = new Alternation<Word, ShapeNode>(
                        alternation.Children.DeepCloneExceptBoundaries()
                    );
                    if (newAlteration.Children.Count > 0)
                        yield return newAlteration;
                    continue;
                }

                if (node is Group<Word, ShapeNode> group)
                {
                    var newGroup = new Group<Word, ShapeNode>(group.Name, group.Children.DeepCloneExceptBoundaries());
                    if (newGroup.Children.Count > 0)
                        yield return newGroup;
                    continue;
                }

                if (node is Quantifier<Word, ShapeNode> quantifier)
                {
                    var newQuantifier = new Quantifier<Word, ShapeNode>(
                        quantifier.MinOccur,
                        quantifier.MaxOccur,
                        quantifier.Children.DeepCloneExceptBoundaries().SingleOrDefault()
                    );
                    if (newQuantifier.Children.Count > 0)
                        yield return newQuantifier;
                    continue;
                }

                if (node is Pattern<Word, ShapeNode> pattern)
                {
                    var newPattern = new Pattern<Word, ShapeNode>(
                        pattern.Name,
                        pattern.Children.DeepCloneExceptBoundaries()
                    );
                    if (newPattern.Children.Count > 0)
                        yield return newPattern;
                }
            }
        }

        /// <summary>
        /// Converts the specified phonetic shape to a valid regular expression string. Regular expressions
        /// formatted for display purposes are NOT guaranteed to compile.
        /// </summary>
        /// <param name="shape">The phonetic shape.</param>
        /// <param name="table">The symbol table.</param>
        /// <param name="displayFormat">if <c>true</c> the result will be formatted for display, otherwise
        /// it will be formatted for compilation.</param>
        /// <returns>The regular expression string.</returns>
        public static string ToRegexString(this Shape shape, CharacterDefinitionTable table, bool displayFormat)
        {
            var sb = new StringBuilder();
            if (!displayFormat)
                sb.Append("^");
            foreach (ShapeNode node in shape)
            {
                if (node.IsDeleted())
                    continue;

                string[] strReps = table.GetMatchingStrReps(node).ToArray();
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

        public static string ToString(
            this IEnumerable<ShapeNode> nodes,
            CharacterDefinitionTable table,
            bool includeBdry
        )
        {
            var sb = new StringBuilder();
            foreach (ShapeNode node in nodes)
            {
                if ((!includeBdry && node.Annotation.Type() == HCFeatureSystem.Boundary) || node.IsDeleted())
                    continue;

                IEnumerable<string> strReps = table.GetMatchingStrReps(node);
                string strRep = strReps.FirstOrDefault();
                if (strRep != null)
                    sb.Append(strRep);
            }
            return sb.ToString();
        }

        public static IEnumerable<FeatureSymbol> PartsOfSpeech(this FeatureStruct fs)
        {
            SymbolicFeatureValue pos;
            if (fs.TryGetValue(SyntacticFeatureSystem.PartOfSpeechID, out pos))
            {
                return pos.Values;
            }
            return Enumerable.Empty<FeatureSymbol>();
        }

        public static FeatureStruct Head(this FeatureStruct fs)
        {
            FeatureStruct head;
            if (fs.TryGetValue(SyntacticFeatureSystem.HeadID, out head))
                return head;
            return null;
        }

        public static FeatureStruct Foot(this FeatureStruct fs)
        {
            FeatureStruct head;
            if (fs.TryGetValue(SyntacticFeatureSystem.FootID, out head))
                return head;
            return null;
        }
    }
}
