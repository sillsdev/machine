namespace SIL.Machine.Translation.Thot
{
    public class ThotHmmWordAlignmentModel : ThotHmmWordAlignmentModelBase
    {
        public ThotHmmWordAlignmentModel() { }

        public ThotHmmWordAlignmentModel(string prefFileName, bool createNew = false)
            : base(prefFileName, createNew) { }

        public override ThotWordAlignmentModelType Type => ThotWordAlignmentModelType.Hmm;

        public override double GetAlignmentProbability(int sourceLen, int prevSourceIndex, int sourceIndex)
        {
            CheckDisposed();

            // add 1 to convert the specified indices to Thot position indices, which are 1-based
            return Thot.swAlignModel_getHmmAlignmentProbability(
                Handle,
                (uint)(prevSourceIndex + 1),
                (uint)sourceLen,
                (uint)(sourceIndex + 1)
            );
        }
    }
}
