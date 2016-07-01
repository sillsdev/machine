using SIL.Machine.FeatureModel;

namespace SIL.HermitCrab
{
	public class NaturalClass
	{
		public NaturalClass(FeatureStruct fs)
		{
			FeatureStruct = fs;
		}

		protected NaturalClass()
		{
		}

		public string Name { get; set; }

		public FeatureStruct FeatureStruct { get; protected set; }

		public override string ToString()
		{
			return string.IsNullOrEmpty(Name) ? base.ToString() : Name;
		}
	}
}
