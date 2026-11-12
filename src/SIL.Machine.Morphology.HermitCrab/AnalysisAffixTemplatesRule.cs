using System.Collections.Generic;
using System.Linq;
using SIL.Machine.Annotations;
using SIL.Machine.Rules;
using SIL.ObjectModel;

namespace SIL.Machine.Morphology.HermitCrab
{
    internal class AnalysisAffixTemplatesRule : RuleBatch<Word, ShapeNode>
    {
        private readonly Morpher _morpher;
        private readonly AnalysisAffixTemplateRule[] _templates;
        private readonly bool[][] _slotOptionality;
        private readonly SlotNode _root = new SlotNode();
        private readonly bool _hasSharedSlots;

        public AnalysisAffixTemplatesRule(Morpher morpher, IEnumerable<IRule<Word, ShapeNode>> rules)
            : base(rules, false, FreezableEqualityComparer<Word>.Default)
        {
            _morpher = morpher;
            _templates = Rules.OfType<AnalysisAffixTemplateRule>().ToArray();
            if (_templates.Length != Rules.Count)
                return;

            _slotOptionality = _templates.Select(t => t.Template.Slots.Select(s => s.Optional).ToArray()).ToArray();
            for (int t = 0; t < _templates.Length; t++)
            {
                AnalysisAffixTemplateRule template = _templates[t];
                SlotNode node = _root;
                for (int s = template.Template.Slots.Count - 1; s >= 0; s--)
                {
                    AffixTemplateSlot slot = template.Template.Slots[s];
                    SlotNode child = node.Children.FirstOrDefault(n => n.Matches(slot));
                    if (child == null)
                    {
                        child = new SlotNode
                        {
                            Slot = slot,
                            Optional = slot.Optional,
                            Rule = template.SlotRules[s],
                        };
                        node.Children.Add(child);
                    }
                    else
                    {
                        _hasSharedSlots = true;
                    }
                    child.Templates.Add(t);
                    node = child;
                }
                node.Terminals.Add(t);
            }
        }

        public override IEnumerable<Word> Apply(Word input)
        {
            // Use the original walk when no tail is shared, when tracing (per-template Begin/End events), for
            // parallel slots, under an alternative limit, or if slot optionality changed after the trie was built.
            if (
                !_hasSharedSlots
                || _morpher.TraceManager.IsTracing
                || _morpher.MaxDegreeOfParallelism != 1
                || MaxAlternatives > 0
                || !SlotsUnchanged()
            )
                return base.Apply(input);

            var outputs = new HashSet<Word>[_templates.Length];
            for (int t = 0; t < _templates.Length; t++)
            {
                if (_templates[t].CanApply(input))
                    outputs[t] = new HashSet<Word>(Comparer);
            }

            if (outputs.Any(o => o != null))
            {
                Word inWord = input.Clone();
                inWord.Freeze();
                Walk(_root, inWord, outputs);
            }

            // Template order determines which equal word survives and the order seen by analysis folding.
            var output = new HashSet<Word>(Comparer);
            foreach (HashSet<Word> templateOutput in outputs)
            {
                if (templateOutput != null)
                    output.UnionWith(templateOutput);
            }
            return output;
        }

        private bool SlotsUnchanged()
        {
            for (int t = 0; t < _templates.Length; t++)
            {
                IList<AffixTemplateSlot> slots = _templates[t].Template.Slots;
                bool[] optionality = _slotOptionality[t];
                if (slots.Count != optionality.Length)
                    return false;
                for (int s = 0; s < slots.Count; s++)
                {
                    if (slots[s].Optional != optionality[s])
                        return false;
                }
            }
            return true;
        }

        private static void Walk(SlotNode node, Word input, HashSet<Word>[] outputs)
        {
            foreach (int t in node.Terminals)
                outputs[t]?.Add(input);

            foreach (SlotNode child in node.Children)
            {
                if (!AnyAdmitted(child.Templates, outputs))
                    continue;

                foreach (Word word in child.Rule.Apply(input))
                    Walk(child, word, outputs);
                if (child.Optional)
                    Walk(child, input, outputs);
            }
        }

        private static bool AnyAdmitted(List<int> templates, HashSet<Word>[] outputs)
        {
            for (int i = 0; i < templates.Count; i++)
            {
                if (outputs[templates[i]] != null)
                    return true;
            }
            return false;
        }

        private sealed class SlotNode
        {
            public AffixTemplateSlot Slot { get; set; }
            public bool Optional { get; set; }
            public IRule<Word, ShapeNode> Rule { get; set; }
            public List<SlotNode> Children { get; } = new List<SlotNode>();
            public List<int> Templates { get; } = new List<int>();
            public List<int> Terminals { get; } = new List<int>();

            public bool Matches(AffixTemplateSlot slot)
            {
                if (Optional != slot.Optional || Slot.Rules.Count != slot.Rules.Count)
                    return false;

                return Slot.Rules.Zip(slot.Rules, (a, b) => ReferenceEquals(a, b)).All(equal => equal);
            }
        }
    }
}
