/*
    Myrtille: A native HTML4/5 Remote Desktop Protocol client.

    Copyright(c) 2014-2018 Cedric Coste

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

        http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.
*/

using System;
using System.Diagnostics;
using System.ServiceModel;
using Myrtille.Services.Contracts;

namespace Myrtille.Web
{
    public class RemoteSessionProcessClient : DuplexClientBase<IRemoteSessionProcess>, IRemoteSessionProcess
    {
        private readonly RemoteSessionManager _remoteSessionManager;
        private object _processStartLock;
        private bool _processStarted = false;

        public RemoteSessionProcessClient(RemoteSessionManager remoteSessionManager, InstanceContext callbackContext)
            : base(callbackContext)
        {
            _remoteSessionManager = remoteSessionManager;
            _processStartLock = new object();
        }

        public bool ProcessStarted
        {
            get
            {
                lock (_processStartLock)
                {
                    return _processStarted;
                }
            }
            set
            {
                _processStarted = value;
            }
        }

        public void StartProcess(
            Guid remoteSessionId,
            HostTypeEnum hostType,
            SecurityProtocolEnum securityProtocol,
            string serverAddress,
            string vmGuid,
            string userDomain,
            string userName,
            string startProgram,
            int clientWidth,
            int clientHeight,
            bool allowRemoteClipboard,
            bool allowPrintDownload)
        {
            Trace.TraceInformation("Calling service start process, remote session {0}, server {1}, domain {2}, user {3}, program {4}", remoteSessionId, serverAddress, string.IsNullOrEmpty(userDomain) ? "(none)" : userDomain, userName, string.IsNullOrEmpty(startProgram) ? "(none)" : startProgram);

            lock (_processStartLock)
            {
                try
                {
                    Channel.StartProcess(
                        remoteSessionId,
                        hostType,
                        securityProtocol,
                        serverAddress,
                        vmGuid,
                        userDomain,
                        userName,
                        startProgram,
                        clientWidth,
                        clientHeight,
                        allowRemoteClipboard,
                        allowPrintDownload);

                    _processStarted = true;
                }
                catch (Exception exc)
                {
                    Trace.TraceError("Failed to call service start process, remote session {0} ({1})", _remoteSessionManager.RemoteSession.Id, exc);
                    throw;
                }
            }
        }

        public void StopProcess()
        {
            Trace.TraceInformation("Calling service stop process, remote session {0}", _remoteSessionManager.RemoteSession.Id);

            try
            {
                Channel.StopProcess();
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to call service stop process, remote session {0} ({1})", _remoteSessionManager.RemoteSession.Id, exc);
                throw;
            }
        }

        public string GetProcessIdentity()
        {
            Trace.TraceInformation("Retrieving service process identity, remote session {0}", _remoteSessionManager.RemoteSession.Id);

            try
            {
                return Channel.GetProcessIdentity();
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to retrieve service process identity, remote session {0} ({1})", _remoteSessionManager.RemoteSession.Id, exc);
                throw;
            }
        }
    }

    [CallbackBehavior(UseSynchronizationContext = false)]
    public class RemoteSessionProcessClientCallback : IRemoteSessionProcessCallback
    {
        private readonly RemoteSessionManager _remoteSessionManager;

        public RemoteSessionProcessClientCallback(RemoteSessionManager remoteSessionManager)
        {
            _remoteSessionManager = remoteSessionManager;
        }

        public void ProcessExited(int exitCode)
        {
            Trace.TraceInformation("Received host client process exit notification, remote session {0}", _remoteSessionManager.RemoteSession.Id);

            try
            {
                // remote session is now disconnected
                _remoteSessionManager.RemoteSession.State = RemoteSessionState.Disconnected;

                // process exit code
                _remoteSessionManager.RemoteSession.ExitCode = exitCode;

                // stop monitoring the remote session owner activity, if enabled
                if (_remoteSessionManager.ClientIdleTimeout != null)
                {
                    _remoteSessionManager.ClientIdleTimeout.Cancel();
                }

                // release the communication pipes, if any
                if (_remoteSessionManager.Pipes != null)
                {
                    _remoteSessionManager.Pipes.DeletePipes();
                }

                if (_remoteSessionManager.RemoteSession.Reconnect)
                {
                    _remoteSessionManager.RemoteSession.Reconnect = false;
                    _remoteSessionManager.HostClient.ProcessStarted = false;
                    _remoteSessionManager.RemoteSession.State = RemoteSessionState.Connecting;
                }

                _remoteSessionManager.SendMessage(new RemoteSessionMessage { Type = MessageType.Disconnected, Prefix = "disconnected" });
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to cleanup disconnected session, remote session {0} ({1})", _remoteSessionManager.RemoteSession.Id, exc);
                throw;
            }
        }
    }
}