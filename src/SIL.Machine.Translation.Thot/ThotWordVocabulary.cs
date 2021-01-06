using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SIL.Machine.Translation.Thot
{
	internal class ThotWordVocabulary : IReadOnlyList<string>
	{
		private readonly IntPtr _swAlignModelHandle;
		private readonly bool _isSource;

		public ThotWordVocabulary(IntPtr swAlignModelHandle, bool isSource)
		{
			_swAlignModelHandle = swAlignModelHandle;
			_isSource = isSource;
		}

		public string this[int index]
		{
			get
			{
				if (index >= Count)
					throw new ArgumentOutOfRangeException(nameof(index));

				uint capacity = 112;
				IntPtr nativeWordStr = Marshal.AllocHGlobal((int)capacity);
				try
				{
					return GetWord((uint)index, ref nativeWordStr, ref capacity);
				}
				finally
				{
					Marshal.FreeHGlobal(nativeWordStr);
				}
			}
		}

		public int Count
		{
			get
			{
				return _isSource ? (int)Thot.swAlignModel_getSourceWordCount(_swAlignModelHandle)
					: (int)Thot.swAlignModel_getTargetWordCount(_swAlignModelHandle);
			}
		}

		public IEnumerator<string> GetEnumerator()
		{
			int count = Count;
			if (count == 0)
				yield break;

			uint capacity = 112;
			IntPtr nativeWordStr = Marshal.AllocHGlobal((int)capacity);
			try
			{
				for (uint i = 0; i < count; i++)
					yield return GetWord(i, ref nativeWordStr, ref capacity);
			}
			finally
			{
				Marshal.FreeHGlobal(nativeWordStr);
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		private string GetWord(uint index, ref IntPtr nativeWordStr, ref uint capacity)
		{
			uint len = GetWordNative(index, nativeWordStr, capacity);
			if (len > capacity)
			{
				capacity = len;
				nativeWordStr = Marshal.ReAllocHGlobal(nativeWordStr, (IntPtr)capacity);
				len = GetWordNative(index, nativeWordStr, capacity);
			}
			return Thot.ConvertNativeUtf8ToToken(nativeWordStr, len);
		}

		private uint GetWordNative(uint index, IntPtr nativeWordStr, uint capacity)
		{
			if (_isSource)
				return Thot.swAlignModel_getSourceWord(_swAlignModelHandle, index, nativeWordStr, capacity);
			else
				return Thot.swAlignModel_getTargetWord(_swAlignModelHandle, index, nativeWordStr, capacity);
		}
	}
}
