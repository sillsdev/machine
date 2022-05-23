using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SIL.Extensions;
using SIL.Machine.Utils;

namespace SIL.Machine.Translation.Thot
{
    public class MiraModelWeightTuner : IParameterTuner
    {
        private readonly ThotWordAlignmentModelType _wordAlignmentModelType;

        public MiraModelWeightTuner(ThotWordAlignmentModelType wordAlignmentModelType)
        {
            _wordAlignmentModelType = wordAlignmentModelType;
        }

        public int K { get; set; } = 100;
        public int MaxIterations { get; set; } = 10;

        public ThotSmtParameters Tune(
            ThotSmtParameters parameters,
            IReadOnlyList<IReadOnlyList<string>> tuneSourceCorpus,
            IReadOnlyList<IReadOnlyList<string>> tuneTargetCorpus,
            TrainStats stats,
            IProgress<ProgressStatus> progress
        )
        {
            IntPtr weightUpdaterHandle = Thot.llWeightUpdater_create();
            try
            {
                var iterQualities = new List<double>();
                double bestQuality = double.MinValue;
                ThotSmtParameters bestParameters = null;
                int iter = 0;
                HashSet<TranslationInfo>[] curNBestLists = null;
                float[] curWeights = parameters.ModelWeights.ToArray();

                while (true)
                {
                    progress.Report(new ProgressStatus(iter, MaxIterations));

                    ThotSmtParameters newParameters = parameters.Clone();
                    newParameters.ModelWeights = curWeights;
                    newParameters.Freeze();
                    IList<TranslationInfo>[] nbestLists = GetNBestLists(newParameters, tuneSourceCorpus).ToArray();
                    double quality = Evaluation.ComputeBleu(
                        nbestLists.Select(nbl => nbl.First().Translation),
                        tuneTargetCorpus
                    );
                    iterQualities.Add(quality);
                    if (quality > bestQuality)
                    {
                        bestQuality = quality;
                        bestParameters = newParameters;
                    }

                    iter++;
                    if (iter >= MaxIterations || IsTuningConverged(iterQualities))
                        break;

                    if (curNBestLists == null)
                    {
                        curNBestLists = nbestLists.Select(nbl => new HashSet<TranslationInfo>(nbl)).ToArray();
                    }
                    else
                    {
                        for (int i = 0; i < nbestLists.Length; i++)
                            curNBestLists[i].UnionWith(nbestLists[i]);
                    }

                    UpdateWeights(weightUpdaterHandle, tuneTargetCorpus, curNBestLists, curWeights);
                }

                if (iter < MaxIterations)
                    progress.Report(new ProgressStatus(iter, 1.0));
                stats.Metrics["bleu"] = bestQuality;
                return bestParameters;
            }
            finally
            {
                Thot.llWeightUpdater_close(weightUpdaterHandle);
            }
        }

        private IEnumerable<IList<TranslationInfo>> GetNBestLists(
            ThotSmtParameters parameters,
            IReadOnlyList<IReadOnlyList<string>> sourceCorpus
        )
        {
            IntPtr smtModelHandle = IntPtr.Zero;
            try
            {
                smtModelHandle = Thot.LoadSmtModel(_wordAlignmentModelType, parameters);
                var results = new IList<TranslationInfo>[sourceCorpus.Count];
                Parallel.ForEach(
                    Partitioner.Create(0, sourceCorpus.Count),
                    range =>
                    {
                        IntPtr decoderHandle = IntPtr.Zero;
                        try
                        {
                            decoderHandle = Thot.LoadDecoder(smtModelHandle, parameters);
                            for (int i = range.Item1; i < range.Item2; i++)
                            {
                                IReadOnlyList<string> sourceSegment = sourceCorpus[i];
                                results[i] = Thot.DoTranslateNBest(
                                        decoderHandle,
                                        Thot.decoder_translateNBest,
                                        K,
                                        sourceSegment,
                                        CreateTranslationInfo
                                    )
                                    .ToArray();
                            }
                        }
                        finally
                        {
                            if (decoderHandle != IntPtr.Zero)
                                Thot.decoder_close(decoderHandle);
                        }
                    }
                );
                return results;
            }
            finally
            {
                if (smtModelHandle != IntPtr.Zero)
                    Thot.smtModel_close(smtModelHandle);
            }
        }

        private static void UpdateWeights(
            IntPtr weightUpdaterHandle,
            IReadOnlyList<IReadOnlyList<string>> tuneTargetCorpus,
            HashSet<TranslationInfo>[] nbestLists,
            float[] curWeights
        )
        {
            IntPtr[] nativeTuneTargetCorpus = tuneTargetCorpus.Select(Thot.ConvertSegmentToNativeUtf8).ToArray();

            int sizeOfPtr = Marshal.SizeOf<IntPtr>();
            int sizeOfDouble = Marshal.SizeOf<double>();
            IntPtr nativeNBestLists = Marshal.AllocHGlobal(nbestLists.Length * sizeOfPtr);
            IntPtr nativeScoreComps = Marshal.AllocHGlobal(nbestLists.Length * sizeOfPtr);
            var nativeNBestListLens = new uint[nbestLists.Length];
            for (int i = 0; i < nbestLists.Length; i++)
            {
                IntPtr nativeNBestList = Marshal.AllocHGlobal(nbestLists[i].Count * sizeOfPtr);
                IntPtr nativeListScoreComps = Marshal.AllocHGlobal(nbestLists[i].Count * sizeOfPtr);
                int j = 0;
                foreach (TranslationInfo ti in nbestLists[i])
                {
                    IntPtr nativeSegment = Thot.ConvertSegmentToNativeUtf8(ti.Translation);
                    Marshal.WriteIntPtr(nativeNBestList, j * sizeOfPtr, nativeSegment);

                    IntPtr nativeTransScoreComps = Marshal.AllocHGlobal((ti.ScoreComponents.Length - 1) * sizeOfDouble);
                    Marshal.Copy(ti.ScoreComponents, 0, nativeTransScoreComps, ti.ScoreComponents.Length - 1);
                    Marshal.WriteIntPtr(nativeListScoreComps, j * sizeOfPtr, nativeTransScoreComps);
                    j++;
                }
                Marshal.WriteIntPtr(nativeNBestLists, i * sizeOfPtr, nativeNBestList);
                Marshal.WriteIntPtr(nativeScoreComps, i * sizeOfPtr, nativeListScoreComps);
                nativeNBestListLens[i] = (uint)nbestLists[i].Count;
            }

            try
            {
                Thot.llWeightUpdater_updateClosedCorpus(
                    weightUpdaterHandle,
                    nativeTuneTargetCorpus,
                    nativeNBestLists,
                    nativeScoreComps,
                    nativeNBestListLens,
                    curWeights,
                    (uint)nbestLists.Length,
                    (uint)curWeights.Length - 1
                );
            }
            finally
            {
                foreach (IntPtr nativeSegment in nativeTuneTargetCorpus)
                    Marshal.FreeHGlobal(nativeSegment);

                for (int i = 0; i < nbestLists.Length; i++)
                {
                    IntPtr nativeNBestList = Marshal.ReadIntPtr(nativeNBestLists, i * sizeOfPtr);
                    IntPtr nativeListScoreComps = Marshal.ReadIntPtr(nativeScoreComps, i * sizeOfPtr);
                    for (int j = 0; j < nbestLists[i].Count; j++)
                    {
                        IntPtr nativeSegment = Marshal.ReadIntPtr(nativeNBestList, j * sizeOfPtr);
                        Marshal.FreeHGlobal(nativeSegment);

                        IntPtr nativeTransScoreComps = Marshal.ReadIntPtr(nativeListScoreComps, j * sizeOfPtr);
                        Marshal.FreeHGlobal(nativeTransScoreComps);
                    }
                    Marshal.FreeHGlobal(nativeNBestList);
                    Marshal.FreeHGlobal(nativeListScoreComps);
                }

                Marshal.FreeHGlobal(nativeNBestLists);
                Marshal.FreeHGlobal(nativeScoreComps);
            }
        }

        private class TranslationInfo : IEquatable<TranslationInfo>
        {
            public TranslationInfo(double[] scoreComponents, IReadOnlyList<string> translation)
            {
                ScoreComponents = scoreComponents;
                Translation = translation;
            }

            public double[] ScoreComponents { get; }
            public IReadOnlyList<string> Translation { get; }

            public override bool Equals(object obj)
            {
                var other = obj as TranslationInfo;
                return other != null && Equals(other);
            }

            public bool Equals(TranslationInfo other)
            {
                return other != null
                    && ScoreComponents.SequenceEqual(other.ScoreComponents)
                    && Translation.SequenceEqual(other.Translation);
            }

            public override int GetHashCode()
            {
                int code = 23;
                code = code * 31 + ScoreComponents.GetSequenceHashCode();
                code = code * 31 + Translation.GetSequenceHashCode();
                return code;
            }
        }

        private static TranslationInfo CreateTranslationInfo(
            IReadOnlyList<string> sourceSegment,
            IReadOnlyList<string> targetSegment,
            IntPtr data
        )
        {
            var scoreComps = new double[8];
            Thot.tdata_getScoreComponents(data, scoreComps, (uint)scoreComps.Length);
            return new TranslationInfo(scoreComps, targetSegment);
        }

        private static bool IsTuningConverged(IList<double> qualities)
        {
            int decrStreakLen = 0;
            double bestQuality = double.MinValue;
            foreach (double quality in qualities)
            {
                if (quality > bestQuality)
                {
                    bestQuality = quality;
                    decrStreakLen = 1;
                }
                else
                {
                    decrStreakLen++;
                }
            }

            return decrStreakLen >= 4;
        }
    }
}
