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

    public class ThotWordAlignmentModelTypeHelpers
    {
        public static ThotWordAlignmentModelType GetThotWordAlignmentModelType(string modelType)
        {
            switch (modelType)
            {
                case "fastAlign":
                case "fast_align":
                    return ThotWordAlignmentModelType.FastAlign;
                case "ibm1":
                    return ThotWordAlignmentModelType.Ibm1;
                case "ibm2":
                    return ThotWordAlignmentModelType.Ibm2;
                default:
                case "hmm":
                    return ThotWordAlignmentModelType.Hmm;
                case "ibm3":
                    return ThotWordAlignmentModelType.Ibm3;
                case "ibm4":
                    return ThotWordAlignmentModelType.Ibm4;
            }
        }
    }
}
