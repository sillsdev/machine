using System.Collections.Generic;
using SIL.APRE.Matching;

namespace SIL.APRE.Transduction
{
	public abstract class PatternRuleBase<TData, TOffset> : IPatternRule<TData, TOffset> where TData : IData<TOffset>
	{
		private readonly Pattern<TData, TOffset> _lhs;
		private readonly ApplicationMode _appMode;

		protected PatternRuleBase(Pattern<TData, TOffset> lhs, ApplicationMode appMode)
		{
			_lhs = lhs;
			_appMode = appMode;
		}

		public Pattern<TData, TOffset> Lhs
		{
			get { return _lhs; }
		}

		public ApplicationMode ApplicationMode
		{
			get { return _appMode; }
		}

		public void Compile()
		{
			_lhs.Compile();
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
			switch (_appMode)
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
						Annotation<TOffset> startAnn = input.Annotations.GetFirst(_lhs.Direction, _lhs.Filter);
						PatternMatch<TOffset> curMatch;
						bool applied = false;
						while (_lhs.IsMatch(inputData, startAnn, out curMatch))
						{
							startAnn = ApplyRhs(input, curMatch, out outputData);
							applied = true;
							if (startAnn == input.Annotations.GetEnd(Lhs.Direction))
								break;
							if (!Lhs.Filter(startAnn))
								startAnn = startAnn.GetNext(Lhs.Direction, Lhs.Filter);
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
