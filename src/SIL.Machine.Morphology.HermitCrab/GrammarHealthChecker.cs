using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// Checks a loaded <see cref="Language"/> against two admissibility preconditions HermitCrab
    /// depends on but never enforces itself: every segment used by the grammar must be declared in
    /// a <see cref="CharacterDefinitionTable"/> (an undeclared segment makes the engine refuse the
    /// whole word, silently), and every declared segment in a table must have a phonological
    /// feature bundle distinct from its neighbors (otherwise a segment-changing rule cannot tell
    /// which one it is looking at). Both violations parse successfully today with no warning, so
    /// this exists to surface them before the grammar ships. It is diagnostic only: it never
    /// changes how a <see cref="Language"/> parses.
    /// </summary>
    public static class GrammarHealthChecker
    {
        /// <summary>
        /// Runs every check against <paramref name="language"/> and returns the findings, in the
        /// order the checks ran. An empty list means both preconditions hold, not that nothing was
        /// checked -- see <see cref="GrammarHealthCodes"/> for what each finding's code means.
        /// </summary>
        public static IList<GrammarHealthFinding> Check(Language language)
        {
            if (language == null)
                throw new ArgumentNullException("language");

            var findings = new List<GrammarHealthFinding>();
            CheckDuplicateFeatureBundles(language, findings);
            CheckUndeclaredSegments(language, findings);
            return findings;
        }

        // Every table's segments must have distinct phonological feature bundles, or a segment-changing rule cannot tell them apart.
        private static void CheckDuplicateFeatureBundles(Language language, List<GrammarHealthFinding> findings)
        {
            // No feature system means every bundle is the same empty struct by construction (see PhonologicalBundle), not a collision.
            if (language.PhonologicalFeatureSystem.Count == 0)
                return;

            foreach (CharacterDefinitionTable table in language.CharacterDefinitionTables)
            {
                List<CharacterDefinition> segmentDefs = table
                    .Where(cd => cd.Type == HCFeatureSystem.Segment)
                    .OrderBy(cd => cd.Representations.First(), StringComparer.Ordinal)
                    .ToList();

                // ValueEquals is the model's own deep, order-independent feature-value equality.
                var groups = new List<List<CharacterDefinition>>();
                foreach (CharacterDefinition cd in segmentDefs)
                {
                    FeatureStruct bundle = PhonologicalBundle(cd);
                    List<CharacterDefinition> group = groups.FirstOrDefault(
                        g => PhonologicalBundle(g[0]).ValueEquals(bundle)
                    );
                    if (group == null)
                    {
                        group = new List<CharacterDefinition>();
                        groups.Add(group);
                    }
                    group.Add(cd);
                }

                foreach (List<CharacterDefinition> group in groups)
                {
                    if (group.Count < 2)
                        continue;

                    string names = string.Join(", ", group.Select(cd => cd.Representations.First()));
                    var subjects = new List<object> { table };
                    subjects.AddRange(group);
                    findings.Add(
                        new GrammarHealthFinding(
                            GrammarHealthSeverity.Warning,
                            GrammarHealthCodes.DuplicateFeatureBundle,
                            string.Format(
                                "Character definition table '{0}' has {1} segments with an identical "
                                    + "phonological feature bundle, so a segment-changing rule cannot reliably "
                                    + "tell them apart: {2}.",
                                table.Name,
                                group.Count,
                                names
                            ),
                            subjects
                        )
                    );
                }
            }
        }

        // Strips Type (constant per segment) and any synthesized StrRep, neither of which the grammar author chose.
        private static FeatureStruct PhonologicalBundle(CharacterDefinition cd)
        {
            FeatureStruct bundle = cd.FeatureStruct.Clone();
            bundle.RemoveValue(HCFeatureSystem.Type);
            bundle.RemoveValue(HCFeatureSystem.StrRep);
            return bundle;
        }

        // Every segment the grammar actually uses must be declared in the table it is used against.
        private static void CheckUndeclaredSegments(Language language, List<GrammarHealthFinding> findings)
        {
            var declaredTables = new HashSet<CharacterDefinitionTable>(language.CharacterDefinitionTables);

            foreach (Stratum stratum in language.Strata)
            {
                foreach (LexEntry entry in stratum.Entries)
                {
                    foreach (RootAllomorph allomorph in entry.Allomorphs)
                    {
                        CheckSegmentsDeclared(
                            allomorph.Segments,
                            string.Format(
                                "Lexical entry '{0}' allomorph '{1}'",
                                entry.Id,
                                allomorph.Segments.Representation
                            ),
                            findings
                        );
                    }
                }

                foreach (IMorphologicalRule rule in stratum.MorphologicalRules)
                {
                    var affixRule = rule as AffixProcessRule;
                    if (affixRule != null)
                    {
                        foreach (AffixProcessAllomorph allomorph in affixRule.Allomorphs)
                        {
                            foreach (InsertSegments insert in allomorph.Rhs.OfType<InsertSegments>())
                            {
                                CheckSegmentsDeclared(
                                    insert.Segments,
                                    string.Format(
                                        "Morphological rule '{0}' inserted segments '{1}'",
                                        affixRule.Name,
                                        insert.Segments.Representation
                                    ),
                                    findings
                                );
                            }
                        }
                    }

                    var compoundingRule = rule as CompoundingRule;
                    if (compoundingRule != null)
                    {
                        foreach (CompoundingSubrule subrule in compoundingRule.Subrules)
                        {
                            foreach (InsertSegments insert in subrule.Rhs.OfType<InsertSegments>())
                            {
                                CheckSegmentsDeclared(
                                    insert.Segments,
                                    string.Format(
                                        "Compounding rule '{0}' inserted segments '{1}'",
                                        compoundingRule.Name,
                                        insert.Segments.Representation
                                    ),
                                    findings
                                );
                            }
                        }
                    }
                }
            }

            foreach (NaturalClass naturalClass in language.NaturalClasses)
            {
                var segmentClass = naturalClass as SegmentNaturalClass;
                if (segmentClass == null)
                    continue;

                foreach (CharacterDefinition cd in segmentClass.Segments)
                {
                    if (cd.CharacterDefinitionTable != null && declaredTables.Contains(cd.CharacterDefinitionTable))
                        continue;

                    findings.Add(
                        new GrammarHealthFinding(
                            GrammarHealthSeverity.Error,
                            GrammarHealthCodes.UndeclaredSegment,
                            string.Format(
                                "Natural class '{0}' references a segment ('{1}') that does not belong to any "
                                    + "character definition table in this language.",
                                naturalClass.Name,
                                cd.Representations.Count > 0 ? cd.Representations.First() : cd.FeatureStruct.ToString()
                            ),
                            new object[] { naturalClass, cd }
                        )
                    );
                }
            }
        }

        // Same GetMatchingStrReps lookup used to render a shape back to text; boundary/anchor nodes are structural, not graphemes.
        private static void CheckSegmentsDeclared(Segments segments, string where, List<GrammarHealthFinding> findings)
        {
            CharacterDefinitionTable table = segments.CharacterDefinitionTable;
            foreach (ShapeNode node in segments.Shape)
            {
                if (node.Annotation.Type() != HCFeatureSystem.Segment)
                    continue;
                if (table.GetMatchingStrReps(node).Any())
                    continue;

                findings.Add(
                    new GrammarHealthFinding(
                        GrammarHealthSeverity.Error,
                        GrammarHealthCodes.UndeclaredSegment,
                        string.Format(
                            "{0} contains a segment with feature bundle {1} that character definition table "
                                + "'{2}' does not declare.",
                            where,
                            node.Annotation.FeatureStruct,
                            table.Name
                        ),
                        new object[] { table, segments, node }
                    )
                );
            }
        }
    }
}
