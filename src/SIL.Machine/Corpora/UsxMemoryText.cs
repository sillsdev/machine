using SIL.Scripture;

namespace SIL.Machine.Corpora
{
    public class UsxMemoryText : UsxTextBase
    {
        private readonly string _usx;

        public UsxMemoryText(string id, string usx, ScrVers versification = null)
            : base(id, versification)
        {
            _usx = usx;
        }

        protected override IStreamContainer CreateStreamContainer()
        {
            return new MemoryStreamContainer(_usx);
        }
    }
}
