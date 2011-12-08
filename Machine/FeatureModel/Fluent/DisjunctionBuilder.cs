using System;
using System.Collections.Generic;

namespace SIL.Machine.FeatureModel.Fluent
{
	public class DisjunctionBuilder : IFirstDisjunctSyntax, ISecondDisjunctSyntax, IFinalDisjunctSyntax
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
			var fsBuilder = new FeatureStructBuilder(_featSys, new FeatureStruct(), _rootFs);
			IDisjunctiveFeatureStructSyntax result = build(fsBuilder);
			_disjuncts.Add(result.Value);
		}

		Disjunction IFinalDisjunctSyntax.ToDisjunction()
		{
			return new Disjunction(_disjuncts);
		}
	}
}
