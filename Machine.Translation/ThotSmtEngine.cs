using System;
using System.Collections.Generic;
using SIL.ObjectModel;

namespace SIL.Machine.Translation
{
	public class ThotSmtEngine : DisposableBase, ISmtEngine
	{
		private readonly IntPtr _handle;
		private readonly HashSet<ThotSmtSession> _sessions; 

		public ThotSmtEngine(string cfgFileName)
		{
			_sessions = new HashSet<ThotSmtSession>();
			_handle = Thot.decoder_open(cfgFileName);
		}

		public ISmtSession StartSession()
		{
			CheckDisposed();

			var session = new ThotSmtSession(this);
			_sessions.Add(session);
			return session;
		}

		public void SaveModels()
		{
			Thot.decoder_saveModels(_handle);
		}

		public double GetTranslationProbability(string sourceWord, string targetWord)
		{
			return Thot.decoder_getTranslationProbability(_handle, Thot.ConvertStringToNativeUtf8(sourceWord),
				Thot.ConvertStringToNativeUtf8(targetWord));
		}

		public int[] GetBestAlignment(IList<string> sourceSegment, IList<string> targetSegment)
		{
			int[] alignment = new int[targetSegment.Count];
			Thot.decoder_getBestAlignment(_handle, Thot.ConvertStringToNativeUtf8(string.Join(" ", sourceSegment)),
				Thot.ConvertStringToNativeUtf8(string.Join(" ", targetSegment)), alignment, alignment.Length);
			return alignment;
		}

		internal IntPtr Handle
		{
			get { return _handle; }
		}

		internal void RemoveSession(ThotSmtSession session)
		{
			_sessions.Remove(session);
		}

		protected override void DisposeManagedResources()
		{
			foreach (ThotSmtSession session in _sessions)
				session.Dispose();
			_sessions.Clear();
		}

		protected override void DisposeUnmanagedResources()
		{
			Thot.decoder_close(_handle);
		}
	}
}
