using System;
using System.Collections.Generic;

namespace SIL.Machine.Translation
{
    public class SymmetrizedWordAlignmentMethod : SymmetrizedWordAligner, IWordAlignmentMethod
    {
        private readonly IWordAlignmentMethod _srcTrgMethod;
        private readonly IWordAlignmentMethod _trgSrcMethod;
        private Func<IReadOnlyList<string>, int, IReadOnlyList<string>, int, double> _scoreSelector;

        public SymmetrizedWordAlignmentMethod(IWordAlignmentMethod srcTrgMethod, IWordAlignmentMethod trgSrcMethod)
            : base(srcTrgMethod, trgSrcMethod)
        {
            _srcTrgMethod = srcTrgMethod;
            _trgSrcMethod = trgSrcMethod;
        }

        public Func<IReadOnlyList<string>, int, IReadOnlyList<string>, int, double> ScoreSelector
        {
            get => _scoreSelector;
            set
            {
                _scoreSelector = value;
                _srcTrgMethod.ScoreSelector = _scoreSelector;
                _trgSrcMethod.ScoreSelector = (s, si, t, ti) => _scoreSelector(t, ti, s, si);
            }
        }
    }
}
