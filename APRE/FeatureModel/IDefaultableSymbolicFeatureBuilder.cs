using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.APRE.FeatureModel
{
	public interface IDefaultableSymbolicFeatureBuilder : ISymbolicFeatureBuilder
	{
		ISymbolicFeatureBuilder Default { get; }
	}
}
