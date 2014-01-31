using System;
using System.Collections.Generic;
using System.Linq;
using SIL.Collections;
using SIL.Machine.Statistics;

namespace SIL.Machine.NgramModeling
{
	public class NgramModel<TSeq, TItem>
	{
		public static IEnumerable<NgramModel<TSeq, TItem>> TrainAll(int maxNgramSize, IEnumerable<TSeq> sequences, Func<TSeq, IEnumerable<TItem>> itemsSelector)
		{
			return TrainAll(maxNgramSize, sequences, itemsSelector, Direction.LeftToRight);
		}

		public static IEnumerable<NgramModel<TSeq, TItem>> TrainAll(int maxNgramSize, IEnumerable<TSeq> sequences, Func<TSeq, IEnumerable<TItem>> itemsSelector, Direction dir)
		{
			return TrainAll(maxNgramSize, sequences, itemsSelector, dir, () => new ModifiedKneserNeySmoother<TSeq, TItem>());
		}

		public static IEnumerable<NgramModel<TSeq, TItem>> TrainAll(int maxNgramSize, IEnumerable<TSeq> sequences, Func<TSeq, IEnumerable<TItem>> itemsSelector,
			Func<INgramModelSmoother<TSeq, TItem>> smootherFactory)
		{
			return TrainAll(maxNgramSize, sequences, itemsSelector, Direction.LeftToRight, smootherFactory);
		}

		public static IEnumerable<NgramModel<TSeq, TItem>> TrainAll(int maxNgramSize, IEnumerable<TSeq> sequences, Func<TSeq, IEnumerable<TItem>> itemsSelector, Direction dir,
			Func<INgramModelSmoother<TSeq, TItem>> smootherFactory)
		{
			TSeq[] seqArray = sequences.ToArray();
			var model = new NgramModel<TSeq, TItem>(maxNgramSize, seqArray, itemsSelector, dir, smootherFactory());
			var models = new NgramModel<TSeq, TItem>[maxNgramSize];
			for (int i = maxNgramSize - 1; i >= 0; i--)
			{
				models[i] = model;
				if (i > 0)
					model = model.Smoother.LowerOrderModel ?? new NgramModel<TSeq, TItem>(i, seqArray, itemsSelector, dir, smootherFactory());
			}
			return models;
		}

		public static NgramModel<TSeq, TItem> Train(int ngramSize, IEnumerable<TSeq> sequences, Func<TSeq, IEnumerable<TItem>> itemsSelector)
		{
			return Train(ngramSize, sequences, itemsSelector, Direction.LeftToRight);
		}

		public static NgramModel<TSeq, TItem> Train(int ngramSize, IEnumerable<TSeq> sequences, Func<TSeq, IEnumerable<TItem>> itemsSelector, Direction dir)
		{
			return Train(ngramSize, sequences, itemsSelector, dir, new ModifiedKneserNeySmoother<TSeq, TItem>());
		}

		public static NgramModel<TSeq, TItem> Train(int ngramSize, IEnumerable<TSeq> sequences, Func<TSeq, IEnumerable<TItem>> itemsSelector, INgramModelSmoother<TSeq, TItem> smoother)
		{
			return Train(ngramSize, sequences, itemsSelector, Direction.LeftToRight, smoother);
		}

		public static NgramModel<TSeq, TItem> Train(int ngramSize, IEnumerable<TSeq> sequences, Func<TSeq, IEnumerable<TItem>> itemsSelector, Direction dir, INgramModelSmoother<TSeq, TItem> smoother)
		{
			return new NgramModel<TSeq, TItem>(ngramSize, sequences.ToArray(), itemsSelector, dir, smoother);
		}

		private readonly int _ngramSize;
		private readonly HashSet<Ngram<TItem>> _ngrams; 
		private readonly INgramModelSmoother<TSeq, TItem> _smoother;
		private readonly Direction _dir;

		public NgramModel(int ngramSize, TSeq[] sequences, Func<TSeq, IEnumerable<TItem>> itemsSelector, Direction dir, INgramModelSmoother<TSeq, TItem> smoother)
		{
			_ngramSize = ngramSize;
			_dir = dir;
			_smoother = smoother;
			_ngrams = new HashSet<Ngram<TItem>>();
			var cfd = new ConditionalFrequencyDistribution<Ngram<TItem>, TItem>();
			foreach (TSeq seq in sequences)
			{
				TItem[] items = itemsSelector(seq).ToArray();

				for (int i = 0; i <= items.Length - ngramSize; i++)
				{
					var ngram = new Ngram<TItem>(Enumerable.Range(i, _ngramSize).Select(j => items[j]));
					_ngrams.Add(ngram);
					Ngram<TItem> context = ngram.TakeAllExceptLast(dir);
					TItem item = ngram.GetLast(dir);
					cfd[context].Increment(item);
				}
			}

			_smoother.Smooth(ngramSize, sequences, itemsSelector, dir, cfd);
		}

		public Direction Direction
		{
			get { return _dir; }
		}

		public double GetProbability(TItem item, Ngram<TItem> context)
		{
			if (context.Length != _ngramSize - 1)
				throw new ArgumentException("The context size is not valid.", "context");
			return _smoother.GetProbability(item, context);
		}

		public IReadOnlyCollection<Ngram<TItem>> Ngrams
		{
			get { return _ngrams.ToReadOnlyCollection(); }
		}

		public INgramModelSmoother<TSeq, TItem> Smoother
		{
			get { return _smoother; }
		}

		public int NgramSize
		{
			get { return _ngramSize; }
		}
	}
}
