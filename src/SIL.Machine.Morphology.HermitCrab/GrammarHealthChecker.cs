using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;
using SIL.ObjectModel;

namespace SIL.Machine.Morphology.HermitCrab
{
    /// <summary>
    /// Checks a loaded <see cref="Language"/> for problems HermitCrab does not otherwise report:
    /// segments used without a declaration, declared segments with duplicate phonological feature
    /// bundles, and morphemes whose analysis is marked partial. These problems can silently refuse
    /// words, make morpheme identification unreliable, or broaden analysis enough to disable safe
    /// final-template pruning. This checker surfaces them before the grammar ships. It is diagnostic
    /// only: it never changes how a <see cref="Language"/> parses.
    /// </summary>
    public static class GrammarHealthChecker
    {
        /// <summary>
        /// Runs every check against <paramref name="language"/> and returns the findings, in the
        /// order the checks ran. An empty list means every registered check passed, not that nothing
        /// was checked -- see <see cref="GrammarHealthCodes"/> for what each finding's code means.
        /// </summary>
        public static IList<GrammarHealthFinding> Check(Language language)
        {
            if (language == null)
                throw new ArgumentNullException("language");

            var findings = new List<GrammarHealthFinding>();
            CheckDuplicateFeatureBundles(language, findings);
            CheckUndeclaredSegments(language, findings);
            CheckPartialMorphemes(language, findings);
            return findings;
        }

        private static void CheckPartialMorphemes(Language language, List<GrammarHealthFinding> findings)
        {
            var seen = new HashSet<Morpheme>(new ReferenceEqualityComparer<Morpheme>());

            foreach (Stratum stratum in language.Strata)
            {
                foreach (LexEntry entry in stratum.Entries)
                    CheckPartialMorpheme(entry, seen, findings);

                foreach (Morpheme rule in stratum.MorphologicalRules.OfType<Morpheme>())
                    CheckPartialMorpheme(rule, seen, findings);

                foreach (AffixTemplate template in stratum.AffixTemplates)
                {
                    foreach (MorphemicMorphologicalRule rule in template.Slots.SelectMany(slot => slot.Rules))
                        CheckPartialMorpheme(rule, seen, findings);
                }
            }
        }

        private static void CheckPartialMorpheme(
            Morpheme morpheme,
            HashSet<Morpheme> seen,
            List<GrammarHealthFinding> findings
        )
        {
            if (!morpheme.IsPartial || !seen.Add(morpheme))
                return;

            string kind;
            string name;
            var rule = morpheme as MorphemicMorphologicalRule;
            if (rule != null)
            {
                kind = "Morphological rule";
                name = FirstNonEmpty(rule.Name, rule.Id, rule.Gloss);
            }
            else
            {
                kind = "Lexical entry";
                name = FirstNonEmpty(morpheme.Id, morpheme.Gloss);
            }

            findings.Add(
                new GrammarHealthFinding(
                    GrammarHealthSeverity.Warning,
                    GrammarHealthCodes.PartialMorpheme,
                    string.Format(
                        "{0} '{1}' is partially analyzed. Supply its missing category or template/slot analysis; "
                            + "leaving it partial can broaden analysis and disable safe final-template pruning.",
                        kind,
                        name
                    ),
                    new object[] { morpheme }
                )
            );
        }

        private static string FirstNonEmpty(params string[] values)
        {
            return values.FirstOrDefault(value => !string.IsNullOrEmpty(value)) ?? "unnamed";
        }

        // Every table's segments must have distinct phonological feature bundles, or a segment-changing
        // rule cannot tell them apart.
        private static void CheckDuplicateFeatureBundles(Language language, List<GrammarHealthFinding> findings)
        {
            // No feature system means every bundle is the same empty struct by construction (see
            // PhonologicalBundle), not a collision.
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
                    List<CharacterDefinition> group = groups.FirstOrDefault(g =>
                        PhonologicalBundle(g[0]).ValueEquals(bundle)
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

        // Same GetMatchingStrReps lookup used to render a shape back to text; boundary/anchor nodes are
        // structural, not graphemes.
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
