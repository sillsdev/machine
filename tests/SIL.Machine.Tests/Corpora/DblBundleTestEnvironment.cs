using System.IO;
using SIL.ObjectModel;

namespace SIL.Machine.Corpora
{
    public class DblBundleTestEnvironment : DisposableBase
    {
        private readonly string _bundlePath;

        public DblBundleTestEnvironment()
        {
            _bundlePath = CorporaTestHelpers.CreateTestDblBundle();
            Corpus = new DblBundleTextCorpus(_bundlePath);
        }

        public DblBundleTextCorpus Corpus { get; }

        protected override void DisposeManagedResources()
        {
            if (File.Exists(_bundlePath))
                File.Delete(_bundlePath);
        }
    }
}
