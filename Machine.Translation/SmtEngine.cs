using System;
using System.Collections.Generic;
using SIL.Collections;

namespace SIL.Machine.Translation
{
	public class SmtEngine : DisposableBase
	{
		private readonly IntPtr _handle;
		private readonly HashSet<SmtSession> _sessions; 

		public SmtEngine(string cfgFileName)
		{
			_sessions = new HashSet<SmtSession>();
			_handle = Thot.decoder_open(cfgFileName);
		}

		public SmtSession StartSession()
		{
			CheckDisposed();

			var session = new SmtSession(this);
			_sessions.Add(session);
			return session;
		}

		public void SaveModels()
		{
			Thot.decoder_saveModels(_handle);
		}

		internal IntPtr Handle
		{
			get { return _handle; }
		}

		internal void RemoveSession(SmtSession session)
		{
			_sessions.Remove(session);
		}

		protected override void DisposeUnmanagedResources()
		{
			Thot.decoder_close(_handle);
			foreach (SmtSession session in _sessions)
				session.Dispose();
		}
	}
}
