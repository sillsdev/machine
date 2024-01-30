using System;
using System.Linq;
using SIL.Machine.Annotations;

namespace SIL.Machine.Morphology.HermitCrab
{
    public class MorphInfo : IEquatable<MorphInfo>
    {
        private readonly string _form;
        private readonly string _gloss;

        public MorphInfo(string form, string gloss)
        {
            _form = form;
            _gloss = gloss;
        }

        public MorphInfo(Word parse, Annotation<ShapeNode> morph)
        {
            _form = morph
                .Children.Where(a => a.Type() != HCFeatureSystem.Morph)
                .Select(a => a.Range.Start)
                .ToString(parse.Stratum.CharacterDefinitionTable, false);
            _gloss = parse.GetAllomorph(morph).Morpheme.Gloss;
            if (string.IsNullOrEmpty(_gloss))
                _gloss = "?";
        }

        public string Form
        {
            get { return _form; }
        }

        public string Gloss
        {
            get { return _gloss; }
        }

        public bool Equals(MorphInfo other)
        {
            return other != null && _form == other._form && _gloss == other._gloss;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as MorphInfo);
        }

        public override int GetHashCode()
        {
            int code = 23;
            code = code * 31 + _form.GetHashCode();
            code = code * 31 + _gloss.GetHashCode();
            return code;
        }
    }
}
