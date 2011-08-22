using System;
using System.Collections.Generic;

namespace SIL.APRE.FeatureModel
{
	public class DisjunctionBuilder
	{
		private readonly FeatureSystem _featSys;
		private readonly FeatureStructure _rootFs;
		private readonly HashSet<FeatureStructure> _disjunction;

		internal DisjunctionBuilder(FeatureSystem featSys, FeatureStructure rootFs)
		{
			_featSys = featSys;
			_rootFs = rootFs;
			_disjunction = new HashSet<FeatureStructure>();
		}

		public DisjunctionBuilder FeatureStructure(Action<DisjunctiveFeatureStructureBuilder> build)
		{
			var fsBuilder = new DisjunctiveFeatureStructureBuilder(_featSys, _rootFs);
			_disjunction.Add(fsBuilder.ToFeatureStructure());
			build(fsBuilder);
			return this;
		}

		public IEnumerable<FeatureStructure> ToDisjunction()
		{
			return _disjunction;
		}
	}
}
