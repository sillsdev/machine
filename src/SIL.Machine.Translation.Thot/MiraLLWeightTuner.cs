using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using SIL.Extensions;
using SIL.Progress;

namespace SIL.Machine.Translation.Thot
{
	public class MiraLLWeightTuner : ILLWeightTuner
	{
		public MiraLLWeightTuner()
		{
			MaxIterations = 10;
			K = 100;
		}

		public double ProgressIncrement { get; set; }
		public int K { get; set; }
		public int MaxIterations { get; set; }

		public double[] Tune(string cfgFileName, IList<IList<string>> tuneSourceCorpus, IList<IList<string>> tuneTargetCorpus, double[] initialWeights, IProgress progress = null)
		{
			if (progress == null)
				progress = new NullProgress();

			IntPtr weightUpdaterHandle = Thot.llWeightUpdater_create();
			try
			{
				var iterQualities = new List<double>();
				double bestQuality = double.MinValue;
				double[] bestWeights = null;
				int iter = 1;
				HashSet<TranslationInfo>[] curNBestLists = null;
				double[] curWeights = initialWeights.ToArray();

				while (true)
				{
					IList<TranslationInfo>[] nbestLists = GetNBestLists(cfgFileName, tuneSourceCorpus, curWeights).ToArray();
					double quality = Evaluation.CalculateBleu(nbestLists.Select(nbl => nbl.First().Translation), tuneTargetCorpus);
					iterQualities.Add(quality);
					if (quality > bestQuality)
					{
						bestQuality = quality;
						bestWeights = curWeights;
					}

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

					iter++;

					progress.ProgressIndicator.PercentCompleted += ProgressIncrement;
				}

				return bestWeights;
			}
			finally
			{
				Thot.llWeightUpdater_close(weightUpdaterHandle);
			}
		}

		private IEnumerable<IList<TranslationInfo>> GetNBestLists(string cfgFileName, IList<IList<string>> sourceCorpus, IEnumerable<double> weights)
		{
			IntPtr decoderHandle = IntPtr.Zero, sessionHandle = IntPtr.Zero;
			try
			{
				decoderHandle = Thot.decoder_open(cfgFileName);
				float[] weightArray = weights.Select(w => (float) w).ToArray();
				Thot.decoder_setLlWeights(decoderHandle, weightArray, (uint) weightArray.Length);
				sessionHandle = Thot.decoder_openSession(decoderHandle);
				foreach (IList<string> sourceSegment in sourceCorpus)
					yield return Thot.DoTranslateNBest(sessionHandle, Thot.session_translateNBest, K, sourceSegment, false, sourceSegment, CreateTranslationInfo).ToArray();
			}
			finally
			{
				if (sessionHandle != IntPtr.Zero)
					Thot.session_close(sessionHandle);
				if (decoderHandle != IntPtr.Zero)
					Thot.decoder_close(decoderHandle);
			}
		}

		private static void UpdateWeights(IntPtr weightUpdaterHandle, IList<IList<string>> tuneTargetCorpus, HashSet<TranslationInfo>[] nbestLists, double[] curWeights)
		{
			IntPtr[] nativeTuneTargetCorpus = tuneTargetCorpus.Select(Thot.ConvertStringsToNativeUtf8).ToArray();

			int sizeOfPtr = Marshal.SizeOf(typeof(IntPtr));
			int sizeOfDouble = Marshal.SizeOf(typeof(double));
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
					IntPtr nativeSegment = Thot.ConvertStringsToNativeUtf8(ti.Translation);
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
				Thot.llWeightUpdater_updateClosedCorpus(weightUpdaterHandle, nativeTuneTargetCorpus, nativeNBestLists, nativeScoreComps, nativeNBestListLens,
					curWeights, (uint) nbestLists.Length, (uint) curWeights.Length - 1);
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
			public TranslationInfo(double[] scoreComponents, IList<string> translation)
			{
				ScoreComponents = scoreComponents;
				Translation = translation;
			}

			public double[] ScoreComponents { get; }
			public IList<string> Translation { get; }

			public override bool Equals(object obj)
			{
				var other = obj as TranslationInfo;
				return other != null && Equals(other);
			}

			public bool Equals(TranslationInfo other)
			{
				return other != null && ScoreComponents.SequenceEqual(other.ScoreComponents) && Translation.SequenceEqual(other.Translation);
			}

			public override int GetHashCode()
			{
				int code = 23;
				code = code * 31 + ScoreComponents.GetSequenceHashCode();
				code = code * 31 + Translation.GetSequenceHashCode();
				return code;
			}
		}

		private static TranslationInfo CreateTranslationInfo(IList<string> sourceSegment, IList<string> targetSegment, IntPtr data)
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
