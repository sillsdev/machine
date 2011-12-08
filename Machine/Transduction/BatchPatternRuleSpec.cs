using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SIL.Machine.Matching;

namespace SIL.Machine.Transduction
{
	public class BatchPatternRuleSpec<TData, TOffset> : IPatternRuleSpec<TData, TOffset> where TData : IData<TOffset>
	{
		private readonly Pattern<TData, TOffset> _pattern;
		private readonly List<IPatternRuleSpec<TData, TOffset>> _ruleSpecs;
		private readonly Dictionary<string, IPatternRuleSpec<TData, TOffset>> _ruleIds;

		public BatchPatternRuleSpec()
		{
			_ruleSpecs = new List<IPatternRuleSpec<TData, TOffset>>();
			_ruleIds = new Dictionary<string, IPatternRuleSpec<TData, TOffset>>();
			_pattern = new Pattern<TData, TOffset>();
		}

		public BatchPatternRuleSpec(IEnumerable<IPatternRuleSpec<TData, TOffset>> ruleSpecs)
			: this()
		{
			foreach (IPatternRuleSpec<TData, TOffset> ruleSpec in ruleSpecs)
				AddRuleSpec(ruleSpec);
		}

		public ReadOnlyCollection<IPatternRuleSpec<TData, TOffset>> RuleSpecs
		{
			get { return _ruleSpecs.AsReadOnly(); }
		}

		public void AddRuleSpec(IPatternRuleSpec<TData, TOffset> ruleSpec)
		{
			InsertRuleSpec(_ruleSpecs.Count, ruleSpec);
		}

		public void InsertRuleSpec(int index, IPatternRuleSpec<TData, TOffset> ruleSpec)
		{
			string id = "rule" + _ruleSpecs.Count;
			_ruleSpecs.Insert(index, ruleSpec);
			_ruleIds[id] = ruleSpec;
			var subpattern = new Pattern<TData, TOffset>(id, ruleSpec.Pattern.Children.Clone())
			{
				Acceptable = match => ruleSpec.IsApplicable(match.Input) && ruleSpec.Pattern.Acceptable(match)
			};

			_pattern.Children.Insert(index == _ruleSpecs.Count - 1 ? _pattern.Children.Last : _pattern.Children.ElementAtOrDefault(index), subpattern);
		}

		public Pattern<TData, TOffset> Pattern
		{
			get { return _pattern; }
		}

		public bool IsApplicable(TData input)
		{
			return true;
		}

		public TOffset ApplyRhs(PatternRule<TData, TOffset> rule, Match<TData, TOffset> match, out TData output)
		{
			IPatternRuleSpec<TData, TOffset> ruleSpec = _ruleIds[match.PatternPath.First()];
			var newMatch = new Match<TData, TOffset>(match.Matcher, match.Span, match.Input, match.GroupCaptures, match.PatternPath.Skip(1),
				match.VariableBindings, match.NextAnnotation);
			return ruleSpec.ApplyRhs(rule, newMatch, out output);
		}
	}
}
