using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SIL.Extensions;
using SIL.Machine.Corpora;
using SIL.ObjectModel;

namespace SIL.Machine.Translation.Thot
{
    public abstract class ThotWordAlignmentModel : DisposableBase, IIbm1WordAlignmentModel
    {
        public static ThotWordAlignmentModel Create(ThotWordAlignmentModelType type)
        {
            switch (type)
            {
                case ThotWordAlignmentModelType.FastAlign:
                    return new ThotFastAlignWordAlignmentModel();
                case ThotWordAlignmentModelType.Ibm1:
                    return new ThotIbm1WordAlignmentModel();
                case ThotWordAlignmentModelType.Ibm2:
                    return new ThotIbm2WordAlignmentModel();
                case ThotWordAlignmentModelType.Hmm:
                    return new ThotHmmWordAlignmentModel();
                case ThotWordAlignmentModelType.Ibm3:
                    return new ThotIbm3WordAlignmentModel();
                case ThotWordAlignmentModelType.Ibm4:
                    return new ThotIbm4WordAlignmentModel();
            }
            throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(ThotWordAlignmentModelType));
        }

        private bool _owned;
        private ThotWordVocabulary _sourceWords;
        private ThotWordVocabulary _targetWords;
        private string _prefFileName;

        protected ThotWordAlignmentModel()
        {
            SetHandle(Thot.CreateAlignmentModel(Type));
        }

        protected ThotWordAlignmentModel(string prefFileName, bool createNew = false)
        {
            if (createNew || !File.Exists(prefFileName + ".src"))
                CreateNew(prefFileName);
            else
                Load(prefFileName);
        }

        public IWordVocabulary SourceWords
        {
            get
            {
                CheckDisposed();

                return _sourceWords;
            }
        }

        public IWordVocabulary TargetWords
        {
            get
            {
                CheckDisposed();

                return _targetWords;
            }
        }

        public IReadOnlySet<int> SpecialSymbolIndices { get; } = new HashSet<int> { 0, 1, 2 }.ToReadOnlySet();

        public ThotWordAlignmentParameters Parameters { get; set; } = new ThotWordAlignmentParameters();

        public abstract ThotWordAlignmentModelType Type { get; }

        protected IntPtr Handle { get; set; }

        internal void SetHandle(IntPtr handle, bool owned = false)
        {
            if (!_owned && Handle != IntPtr.Zero)
                Thot.swAlignModel_close(Handle);
            Handle = handle;
            _owned = owned;
            _sourceWords = new ThotWordVocabulary(Handle, true);
            _targetWords = new ThotWordVocabulary(Handle, false);
        }

        public void Load(string prefFileName)
        {
            if (_owned)
                throw new InvalidOperationException("The word alignment model is owned by an SMT model.");
            if (!File.Exists(prefFileName + ".src"))
                throw new FileNotFoundException("The word alignment model configuration could not be found.");

            _prefFileName = prefFileName;
            SetHandle(Thot.OpenAlignmentModel(Type, _prefFileName));
        }

        public Task LoadAsync(string prefFileName)
        {
            Load(prefFileName);
            return Task.CompletedTask;
        }

        public void CreateNew(string prefFileName)
        {
            if (_owned)
                throw new InvalidOperationException("The word alignment model is owned by an SMT model.");

            _prefFileName = prefFileName;
            SetHandle(Thot.CreateAlignmentModel(Type));
        }

        public ITrainer CreateTrainer(IParallelTextCorpus corpus)
        {
            CheckDisposed();

            if (_owned)
            {
                throw new InvalidOperationException(
                    "The word alignment model cannot be trained independently of its SMT model."
                );
            }

            return new Trainer(this, corpus);
        }

        public Task SaveAsync()
        {
            CheckDisposed();

            Save();
            return Task.CompletedTask;
        }

        public void Save()
        {
            CheckDisposed();

            if (!string.IsNullOrEmpty(_prefFileName))
                Thot.swAlignModel_save(Handle, _prefFileName);
        }

        public double GetTranslationScore(string sourceWord, string targetWord)
        {
            return GetTranslationProbability(sourceWord, targetWord);
        }

        public double GetTranslationScore(int sourceWordIndex, int targetWordIndex)
        {
            return GetTranslationProbability(sourceWordIndex, targetWordIndex);
        }

        public double GetTranslationProbability(string sourceWord, string targetWord)
        {
            CheckDisposed();

            if (targetWord == null)
                return 0;

            IntPtr nativeSourceWord = Thot.ConvertTokenToNativeUtf8(sourceWord ?? "NULL");
            IntPtr nativeTargetWord = Thot.ConvertTokenToNativeUtf8(targetWord ?? "NULL");
            try
            {
                return Thot.swAlignModel_getTranslationProbability(Handle, nativeSourceWord, nativeTargetWord);
            }
            finally
            {
                Marshal.FreeHGlobal(nativeTargetWord);
                Marshal.FreeHGlobal(nativeSourceWord);
            }
        }

        public double GetTranslationProbability(int sourceWordIndex, int targetWordIndex)
        {
            CheckDisposed();

            if (targetWordIndex == 0)
                return 0;

            return Thot.swAlignModel_getTranslationProbabilityByIndex(
                Handle,
                (uint)sourceWordIndex,
                (uint)targetWordIndex
            );
        }

        public WordAlignmentMatrix Align(IReadOnlyList<string> sourceSegment, IReadOnlyList<string> targetSegment)
        {
            CheckDisposed();

            IntPtr nativeSourceSegment = Thot.ConvertSegmentToNativeUtf8(sourceSegment);
            IntPtr nativeTargetSegment = Thot.ConvertSegmentToNativeUtf8(targetSegment);
            IntPtr nativeMatrix = Thot.AllocNativeMatrix(sourceSegment.Count, targetSegment.Count);

            uint iLen = (uint)sourceSegment.Count;
            uint jLen = (uint)targetSegment.Count;
            try
            {
                Thot.swAlignModel_getBestAlignment(
                    Handle,
                    nativeSourceSegment,
                    nativeTargetSegment,
                    nativeMatrix,
                    ref iLen,
                    ref jLen
                );
                return Thot.ConvertNativeMatrixToWordAlignmentMatrix(nativeMatrix, iLen, jLen);
            }
            finally
            {
                Thot.FreeNativeMatrix(nativeMatrix, iLen);
                Marshal.FreeHGlobal(nativeTargetSegment);
                Marshal.FreeHGlobal(nativeSourceSegment);
            }
        }

        public IReadOnlyList<WordAlignmentMatrix> AlignBatch(
            IReadOnlyList<(IReadOnlyList<string> SourceSegment, IReadOnlyList<string> TargetSegment)> segments
        )
        {
            CheckDisposed();

            return segments.AsParallel().AsOrdered().Select(s => Align(s.SourceSegment, s.TargetSegment)).ToArray();
        }

        public IEnumerable<(string TargetWord, double Score)> GetTranslations(string sourceWord, double threshold = 0)
        {
            CheckDisposed();

            IntPtr nativeSourceWord = Thot.ConvertTokenToNativeUtf8(sourceWord ?? "NULL");
            IntPtr transHandle = Thot.swAlignModel_getTranslations(Handle, nativeSourceWord, threshold);
            try
            {
                uint transCount = Thot.swAlignTrans_getCount(transHandle);

                var wordIndices = new uint[transCount];
                var probs = new double[transCount];
                Thot.swAlignTrans_getTranslations(transHandle, wordIndices, probs, transCount);

                return wordIndices.Zip(probs, (w, p) => (TargetWords[(int)w], p));
            }
            finally
            {
                Marshal.FreeHGlobal(nativeSourceWord);
                Thot.swAlignTrans_destroy(transHandle);
            }
        }

        public IEnumerable<(int TargetWordIndex, double Score)> GetTranslations(
            int sourceWordIndex,
            double threshold = 0
        )
        {
            CheckDisposed();

            IntPtr transHandle = Thot.swAlignModel_getTranslationsByIndex(Handle, (uint)sourceWordIndex, threshold);
            try
            {
                uint transCount = Thot.swAlignTrans_getCount(transHandle);

                var wordIndices = new uint[transCount];
                var probs = new double[transCount];
                Thot.swAlignTrans_getTranslations(transHandle, wordIndices, probs, transCount);

                return wordIndices.Zip(probs, (w, p) => ((int)w, p));
            }
            finally
            {
                Thot.swAlignTrans_destroy(transHandle);
            }
        }

        public IReadOnlyCollection<AlignedWordPair> GetBestAlignedWordPairs(
            IReadOnlyList<string> sourceSegment,
            IReadOnlyList<string> targetSegment
        )
        {
            CheckDisposed();

            WordAlignmentMatrix matrix = Align(sourceSegment, targetSegment);
            IReadOnlyCollection<AlignedWordPair> wordPairs = matrix.ToAlignedWordPairs();
            ComputeAlignedWordPairScores(sourceSegment, targetSegment, wordPairs);
            return wordPairs;
        }

        public abstract void ComputeAlignedWordPairScores(
            IReadOnlyList<string> sourceSegment,
            IReadOnlyList<string> targetSegment,
            IReadOnlyCollection<AlignedWordPair> wordPairs
        );

        protected override void DisposeUnmanagedResources()
        {
            if (!_owned)
                Thot.swAlignModel_close(Handle);
        }

        private class Trainer : ThotWordAlignmentModelTrainer
        {
            private readonly ThotWordAlignmentModel _model;

            public Trainer(ThotWordAlignmentModel model, IParallelTextCorpus corpus)
                : base(model.Type, corpus, model._prefFileName, model.Parameters)
            {
                _model = model;
                CloseOnDispose = false;
            }

            public override void Save()
            {
                Thot.swAlignModel_close(_model.Handle);
                _model.Handle = IntPtr.Zero;

                base.Save();

                _model.SetHandle(Handle);
            }
        }
    }
}
