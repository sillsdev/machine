using System.Collections.Generic;
using SIL.APRE.Matching;

namespace SIL.APRE.Transduction
{
	public enum ApplicationMode
	{
		Single,
		Multiple,
		Iterative,
		Simultaneous
	}

	public abstract class PatternRule<TData, TOffset> : IRule<TData, TOffset> where TData : IData<TOffset>
	{
		private readonly Pattern<TData, TOffset> _lhs;
		private ApplicationMode _appMode;

		protected PatternRule(Pattern<TData, TOffset> lhs)
		{
			_lhs = lhs;
		}

		public PatternRule<TData, TOffset> Parent { get; internal set; }

		public Pattern<TData, TOffset> Lhs
		{
			get { return _lhs; }
		}

		public ApplicationMode ApplicationMode
		{
			get
			{
				if (Parent != null)
					return Parent.ApplicationMode;
				return _appMode;
			}

			set { _appMode = value; }
		}

		public Direction Direction
		{
			get
			{
				if (Parent != null)
					return Parent._lhs.Direction;
				return _lhs.Direction;
			}

			set { _lhs.Direction = value; }
		}

		public abstract bool IsApplicable(TData input);

		public virtual bool Apply(TData input, out IEnumerable<TData> output)
		{
			if (input.Annotations.Count == 0 || !IsApplicable(input))
			{
				output = null;
				return false;
			}

			List<TData> outputList = null;
			switch (ApplicationMode)
			{
				case ApplicationMode.Simultaneous:
					{
						IEnumerable<PatternMatch<TOffset>> matches;
						TData inputData = input;
						if (_lhs.IsMatch(inputData, out matches))
						{
							TData outputData = default(TData);
							foreach (PatternMatch<TOffset> match in matches)
							{
								ApplyRhs(inputData, match, out outputData);
								inputData = outputData;
							}
							outputList = new List<TData> {outputData};
						}
					}
					break;

				case ApplicationMode.Iterative:
					{
						TData inputData = input;
						TData outputData = default(TData);
						Annotation<TOffset> startAnn = input.Annotations.GetFirst(Direction, _lhs.Filter);
						PatternMatch<TOffset> curMatch;
						bool applied = false;
						while (_lhs.IsMatch(inputData, startAnn, out curMatch))
						{
							startAnn = ApplyRhs(input, curMatch, out outputData);
							applied = true;
							if (startAnn == input.Annotations.GetEnd(Direction))
								break;
							if (!_lhs.Filter(startAnn))
								startAnn = startAnn.GetNext(Direction, _lhs.Filter);
							inputData = outputData;
						}
						if (applied)
							outputList = new List<TData> {outputData};
					}
					break;

				case ApplicationMode.Single:
					{
						PatternMatch<TOffset> match;
						if (_lhs.IsMatch(input, out match))
						{
							TData outputData;
							ApplyRhs(input, match, out outputData);
							outputList = new List<TData> {outputData};
						}
					}
					break;

				case ApplicationMode.Multiple:
					{
						IEnumerable<PatternMatch<TOffset>> matches;
						if (_lhs.IsMatch(input, out matches))
						{
							outputList = new List<TData>();
							foreach (PatternMatch<TOffset> match in matches)
							{
								TData outputData;
								ApplyRhs(input, match, out outputData);
								outputList.Add(outputData);
							}
						}
					}
					break;
			}

			output = outputList;
			return output != null;
		}

		public abstract Annotation<TOffset> ApplyRhs(TData input, PatternMatch<TOffset> match, out TData output);
	}
}
