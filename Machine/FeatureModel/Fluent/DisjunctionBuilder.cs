using System;
using System.Collections.Generic;

namespace SIL.Machine.FeatureModel.Fluent
{
	public class DisjunctionBuilder : IFirstDisjunctSyntax, ISecondDisjunctSyntax, IFinalDisjunctSyntax
	{
		private readonly FeatureSystem _featSys;
		private readonly IDictionary<int, FeatureValue> _ids; 
		private readonly List<FeatureStruct> _disjuncts;

		public DisjunctionBuilder(FeatureSystem featSys, IDictionary<int, FeatureValue> ids)
		{
			_featSys = featSys;
			_ids = ids;
			_disjuncts = new List<FeatureStruct>();
		}

		public IEnumerable<FeatureStruct> Disjuncts
		{
			get { return _disjuncts; }
		}

		public ISecondDisjunctSyntax With(Func<IDisjunctiveFeatureStructSyntax, IDisjunctiveFeatureStructSyntax> build)
		{
			AddDisjunct(build);
			return this;
		}

		IFinalDisjunctSyntax ISecondDisjunctSyntax.Or(Func<IDisjunctiveFeatureStructSyntax, IDisjunctiveFeatureStructSyntax> build)
		{
			AddDisjunct(build);
			return this;
		}

		IFinalDisjunctSyntax IFinalDisjunctSyntax.Or(Func<IDisjunctiveFeatureStructSyntax, IDisjunctiveFeatureStructSyntax> build)
		{
			AddDisjunct(build);
			return this;
		}

		private void AddDisjunct(Func<IDisjunctiveFeatureStructSyntax, IDisjunctiveFeatureStructSyntax> build)
		{
			var fsBuilder = new FeatureStructBuilder(_featSys, new FeatureStruct(), _ids);
			IDisjunctiveFeatureStructSyntax result = build(fsBuilder);
			_disjuncts.Add(result.Value);
		}
	}
}
