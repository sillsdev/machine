using System.Collections;
using System.Collections.Generic;
using SIL.Machine.Annotations;
using SIL.Machine.Rules;

namespace SIL.Machine.Morphology.HermitCrab
{
    public abstract class HCRuleBase : IHCRule
    {
        private readonly IDictionary _properties;

        protected HCRuleBase()
        {
            _properties = new Dictionary<object, object>();
        }

        public string Name { get; set; }

        public abstract IRule<Word, ShapeNode> CompileAnalysisRule(Morpher morpher);

        public abstract IRule<Word, ShapeNode> CompileSynthesisRule(Morpher morpher);

        public IDictionary Properties
        {
            get { return _properties; }
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(Name) ? base.ToString() : Name;
        }
    }
}
