using System;
using SIL.Machine.FeatureModel;

namespace SIL.Machine
{
	public class AnnotationFeatureSystem : FeatureSystem
	{
		public static readonly StringFeature Type;

		private static readonly AnnotationFeatureSystem FeatureSystem;

		static AnnotationFeatureSystem()
		{
			Type = new StringFeature(Guid.NewGuid().ToString()) {Description = "Type"};

			FeatureSystem = new AnnotationFeatureSystem();
		}

		public static AnnotationFeatureSystem Instance
		{
			get { return FeatureSystem; }
		}

		private AnnotationFeatureSystem()
		{
			base.AddFeature(Type);
		}

		public override void AddFeature(Feature feature)
		{
			throw new NotSupportedException("This feature system is readonly.");
		}

		public override void Reset()
		{
			throw new NotSupportedException("This feature system is readonly.");
		}
	}
}
