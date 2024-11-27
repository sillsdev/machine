using System;
using CaseExtensions;

namespace SIL.Machine.Translation.Thot
{
    public enum ThotWordAlignmentModelType
    {
        FastAlign,
        Ibm1,
        Ibm2,
        Hmm,
        Ibm3,
        Ibm4
    }

    public static class ThotWordAlignmentHelpers
    {
        public const string FastAlign = "fast_align";
        public const string Ibm1 = "ibm1";
        public const string Ibm2 = "ibm2";
        public const string Hmm = "hmm";
        public const string Ibm3 = "ibm3";
        public const string Ibm4 = "ibm4";

        public static ThotWordAlignmentModelType GetThotWordAlignmentModelType(
            string modelType,
            ThotWordAlignmentModelType? defaultType = null
        )
        {
            switch (modelType.ToSnakeCase())
            {
                case FastAlign:
                    return ThotWordAlignmentModelType.FastAlign;
                case Ibm1:
                    return ThotWordAlignmentModelType.Ibm1;
                case Ibm2:
                    return ThotWordAlignmentModelType.Ibm2;
                case Hmm:
                    return ThotWordAlignmentModelType.Hmm;
                case Ibm3:
                    return ThotWordAlignmentModelType.Ibm3;
                case Ibm4:
                    return ThotWordAlignmentModelType.Ibm4;
                default:
                    return defaultType
                        ?? throw new ArgumentException($"Invalid word alignment model type: {modelType}");
            }
        }
    }
}
