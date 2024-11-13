using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SIL.ObjectModel;
using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    internal class TextCorpusEnumerator : DisposableBase, IEnumerator<TextRow>
    {
        private readonly IEnumerator<TextRow> _enumerator;
        private readonly bool _isScripture = false;
        private readonly Queue<TextRow> _verseRows;
        private readonly ScrVers _refVersification;
        private TextRow _current;
        private bool _isEnumerating = false;
        private bool _enumeratorHasMoreData = true;

        public TextCorpusEnumerator(IEnumerator<TextRow> enumerator, ScrVers refVersification, ScrVers versification)
        {
            _enumerator = enumerator;
            _refVersification = refVersification;
            _isScripture = refVersification != null && versification != null && refVersification != versification;
            _verseRows = new Queue<TextRow>();
        }

        public TextRow Current => _current;

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (_isScripture)
            {
                if (!_isEnumerating)
                {
                    _enumerator.MoveNext();
                    _isEnumerating = true;
                }
                if (_verseRows.Count == 0 && _enumerator.Current != null && _enumeratorHasMoreData)
                    CollectVerses();
                if (_verseRows.Count > 0)
                {
                    _current = _verseRows.Dequeue();
                    return true;
                }
                _current = null;
                return false;
            }

            _enumeratorHasMoreData = _enumerator.MoveNext();
            _current = _enumerator.Current;
            return _enumeratorHasMoreData;
        }

        public void Reset()
        {
            _enumerator.Reset();
            _isEnumerating = false;
            _enumeratorHasMoreData = true;
        }

        protected override void DisposeManagedResources()
        {
            _enumerator.Dispose();
        }

        private void CollectVerses()
        {
            var rowList = new List<(ScriptureRef Ref, TextRow Row)>();
            bool outOfOrder = false;
            ScriptureRef prevRefRef = ScriptureRef.Empty;
            int rangeStartOffset = -1;
            do
            {
                TextRow row = _enumerator.Current;
                var refRef = (ScriptureRef)row.Ref;
                if (!prevRefRef.IsEmpty && refRef.BookNum != prevRefRef.BookNum)
                    break;

                refRef = refRef.ChangeVersification(_refVersification);
                // convert one-to-many versification mapping to a verse range
                if (refRef.Equals(prevRefRef))
                {
                    (ScriptureRef rangeStartVerseRef, TextRow rangeStartRow) = rowList[
                        rowList.Count + rangeStartOffset
                    ];
                    TextRowFlags flags = TextRowFlags.InRange;
                    if (rangeStartRow.IsSentenceStart)
                        flags |= TextRowFlags.SentenceStart;
                    if (rangeStartOffset == -1 && (!rangeStartRow.IsInRange || rangeStartRow.IsRangeStart))
                        flags |= TextRowFlags.RangeStart;
                    rowList[rowList.Count + rangeStartOffset] = (
                        rangeStartVerseRef,
                        new TextRow(rangeStartRow.TextId, rangeStartRow.Ref)
                        {
                            Segment = rangeStartRow.Segment.Concat(row.Segment).ToArray(),
                            Flags = flags
                        }
                    );
                    row = new TextRow(row.TextId, row.Ref) { Flags = TextRowFlags.InRange };
                    rangeStartOffset--;
                }
                else
                {
                    rangeStartOffset = -1;
                }
                rowList.Add((refRef, row));
                if (!outOfOrder && refRef.CompareTo(prevRefRef) < 0)
                    outOfOrder = true;
                prevRefRef = refRef;
                _enumeratorHasMoreData = _enumerator.MoveNext();
            } while (_enumeratorHasMoreData);

            if (outOfOrder)
                rowList.Sort((x, y) => x.Ref.CompareTo(y.Ref));

            foreach ((ScriptureRef _, TextRow row) in rowList)
                _verseRows.Enqueue(row);
        }
    }
}
