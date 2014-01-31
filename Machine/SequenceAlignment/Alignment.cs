using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Collections;

namespace SIL.Machine.SequenceAlignment
{
	public class Alignment<TSeq, TItem>
	{
		private readonly ReadOnlyList<TSeq> _sequences; 
		private readonly AlignmentCell<TItem>[,] _matrix;
		private readonly ReadOnlyList<AlignmentCell<TItem>> _prefixes;
		private readonly ReadOnlyList<AlignmentCell<TItem>> _suffixes;
		private readonly int _rawScore;
		private readonly double _normalizedScore;

		public Alignment(int rawScore, double normalizedScore, params Tuple<TSeq, AlignmentCell<TItem>, IEnumerable<AlignmentCell<TItem>>, AlignmentCell<TItem>>[] sequences)
			: this(rawScore, normalizedScore, (IEnumerable<Tuple<TSeq, AlignmentCell<TItem>, IEnumerable<AlignmentCell<TItem>>, AlignmentCell<TItem>>>) sequences)
		{
		}

		public Alignment(int rawScore, double normalizedScore, IEnumerable<Tuple<TSeq, AlignmentCell<TItem>, IEnumerable<AlignmentCell<TItem>>, AlignmentCell<TItem>>> sequences)
		{
			_rawScore = rawScore;
			_normalizedScore = normalizedScore;
			Tuple<TSeq, AlignmentCell<TItem>, IEnumerable<AlignmentCell<TItem>>, AlignmentCell<TItem>>[] sequenceArray = sequences.ToArray();
			var seqs = new TSeq[sequenceArray.Length];
			var prefixes = new AlignmentCell<TItem>[sequenceArray.Length];
			var suffixes = new AlignmentCell<TItem>[sequenceArray.Length];
			for (int i = 0; i < sequenceArray.Length; i++)
			{
				seqs[i] = sequenceArray[i].Item1;

				prefixes[i] = sequenceArray[i].Item2;

				AlignmentCell<TItem>[] columnArray = sequenceArray[i].Item3.ToArray();
				if (_matrix == null)
					_matrix = new AlignmentCell<TItem>[sequenceArray.Length, columnArray.Length];
				for (int j = 0; j < columnArray.Length; j++)
					_matrix[i, j] = columnArray[j];

				suffixes[i] = sequenceArray[i].Item4;
			}
			_sequences = new ReadOnlyList<TSeq>(seqs);
			_prefixes = new ReadOnlyList<AlignmentCell<TItem>>(prefixes);
			_suffixes = new ReadOnlyList<AlignmentCell<TItem>>(suffixes);
		}

		public int RawScore
		{
			get { return _rawScore; }
		}

		public double NormalizedScore
		{
			get { return _normalizedScore; }
		}

		public int SequenceCount
		{
			get { return _matrix.GetLength(0); }
		}

		public int ColumnCount
		{
			get { return _matrix.GetLength(1); }
		}

		public IReadOnlyList<TSeq> Sequences
		{
			get { return _sequences; }
		}

		public IReadOnlyList<AlignmentCell<TItem>> Prefixes
		{
			get { return _prefixes; }
		}

		public IReadOnlyList<AlignmentCell<TItem>> Suffixes
		{
			get { return _suffixes; }
		}

		public AlignmentCell<TItem> this[int sequenceIndex, int columnIndex]
		{
			get { return _matrix[sequenceIndex, columnIndex]; }
		}
	}
}
