using System;
using System.Collections.Generic;
using SIL.Collections;

namespace SIL.Machine.Translation
{
	public class ThotDecoder : DisposableBase
	{
		private readonly IntPtr _handle;
		private readonly HashSet<ThotSession> _sessions; 

		public ThotDecoder(string cfgFileName)
		{
			_sessions = new HashSet<ThotSession>();
			_handle = Thot.decoder_open(cfgFileName);
		}

		public ThotSession StartSession()
		{
			CheckDisposed();

			var session = new ThotSession(this);
			_sessions.Add(session);
			return session;
		}

		internal IntPtr Handle
		{
			get { return _handle; }
		}

		internal void RemoveSession(ThotSession session)
		{
			_sessions.Remove(session);
		}

		protected override void DisposeUnmanagedResources()
		{
			Thot.decoder_close(_handle);
			foreach (ThotSession session in _sessions)
				session.Dispose();
		}
	}
}
