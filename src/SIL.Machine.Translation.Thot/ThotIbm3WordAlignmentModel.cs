namespace SIL.Machine.Translation.Thot
{
    public class ThotIbm3WordAlignmentModel : ThotIbm2WordAlignmentModel
    {
        public ThotIbm3WordAlignmentModel() { }

        public ThotIbm3WordAlignmentModel(string prefFileName, bool createNew = false)
            : base(prefFileName, createNew) { }

        public override ThotWordAlignmentModelType Type => ThotWordAlignmentModelType.Ibm3;
    }
}
