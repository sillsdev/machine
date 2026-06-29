namespace SIL.Machine.Translation.Thot
{
    public class ThotEflomalWordAlignmentModel : ThotHmmWordAlignmentModelBase
    {
        public ThotEflomalWordAlignmentModel() { }

        public ThotEflomalWordAlignmentModel(string prefFileName, bool createNew = false)
            : base(prefFileName, createNew) { }

        public override ThotWordAlignmentModelType Type => ThotWordAlignmentModelType.Eflomal;

        public override double GetAlignmentProbability(int sourceLen, int prevSourceIndex, int sourceIndex)
        {
            CheckDisposed();

            // add 1 to convert the specified indices to Thot position indices, which are 1-based
            return Thot.swAlignModel_getEflomalAlignmentProbability(
                Handle,
                (uint)(prevSourceIndex + 1),
                (uint)sourceLen,
                (uint)(sourceIndex + 1)
            );
        }
    }
}
