namespace SIL.Machine.Translation.Thot
{
    public class ThotIbm4WordAlignmentModel : ThotIbm3WordAlignmentModel
    {
        public ThotIbm4WordAlignmentModel() { }

        public ThotIbm4WordAlignmentModel(string prefFileName, bool createNew = false) : base(prefFileName, createNew)
        { }

        public override ThotWordAlignmentModelType Type => ThotWordAlignmentModelType.Ibm4;
    }
}
