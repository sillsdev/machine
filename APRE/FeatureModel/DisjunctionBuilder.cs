using System;
using System.Collections.Generic;

namespace SIL.APRE.FeatureModel
{
	public class DisjunctionBuilder : IFirstDisjunctBuilder, ISecondDisjunctBuilder, IFinalDisjunctBuilder
	{
		private readonly FeatureSystem _featSys;
		private readonly FeatureStruct _rootFs;
		private readonly List<FeatureStruct> _disjuncts;

		public DisjunctionBuilder(FeatureSystem featSys, FeatureStruct rootFs)
		{
			_featSys = featSys;
			_rootFs = rootFs;
			_disjuncts = new List<FeatureStruct>();
		}

		public ISecondDisjunctBuilder With(Func<IDisjunctiveFeatureStructBuilder, IDisjunctiveFeatureStructBuilder> build)
		{
			AddDisjunct(build);
			return this;
		}

		IFinalDisjunctBuilder ISecondDisjunctBuilder.Or(Func<IDisjunctiveFeatureStructBuilder, IDisjunctiveFeatureStructBuilder> build)
		{
			AddDisjunct(build);
			return this;
		}

		IFinalDisjunctBuilder IFinalDisjunctBuilder.Or(Func<IDisjunctiveFeatureStructBuilder, IDisjunctiveFeatureStructBuilder> build)
		{
			AddDisjunct(build);
			return this;
		}

		private void AddDisjunct(Func<IDisjunctiveFeatureStructBuilder, IDisjunctiveFeatureStructBuilder> build)
		{
			var fsBuilder = new DisjunctiveFeatureStructBuilder(_featSys, _rootFs);
			IDisjunctiveFeatureStructBuilder result = build(fsBuilder);
			_disjuncts.Add(result.Value);
		}

		Disjunction IFinalDisjunctBuilder.ToDisjunction()
		{
			return new Disjunction(_disjuncts);
		}
	}
}
