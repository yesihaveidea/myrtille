using System;
using System.Diagnostics;
using Myrtille.Services.Contracts;

namespace Myrtille.Services
{
    public class RemoteSessionCounter : IRemoteSessionCounter
    {
        public int GetRemoteSessionId()
        {
            try
            {
                lock (Program._remoteSessionsCounterLock)
                {
                    return ++Program._remoteSessionsCounter;
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to get a remote session id ({0})", exc);
                throw;
            }
        }
    }
}