/*
    Myrtille: A native HTML4/5 Remote Desktop Protocol client.

    Copyright(c) 2014-2019 Cedric Coste

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
using Myrtille.Helpers;
using Myrtille.Services.Contracts;
using Myrtille.Web.Properties;

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
            HostType hostType,
            SecurityProtocol securityProtocol,
            string serverAddress,
            string vmGuid,
            string userDomain,
            string userName,
            string startProgram,
            int clientWidth,
            int clientHeight,
            bool allowRemoteClipboard,
            bool allowPrintDownload,
            bool allowAudioPlayback)
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
                        allowPrintDownload,
                        allowAudioPlayback);

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
        private static ConnectionClient _connectionClient = new ConnectionClient(Settings.Default.ConnectionServiceUrl);

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

                // if the remote session is marked for reconnection, reconnect it
                if (_remoteSessionManager.RemoteSession.Reconnect)
                {
                    _remoteSessionManager.RemoteSession.Reconnect = false;
                    _remoteSessionManager.RemoteSession.BrowserResize = null;
                    _remoteSessionManager.HostClient.ProcessStarted = false;
                    _remoteSessionManager.RemoteSession.State = RemoteSessionState.Connecting;
                }

                // send a disconnect notification to the browser
                _remoteSessionManager.SendMessage(new RemoteSessionMessage { Type = MessageType.Disconnected, Prefix = "disconnected" });

                // if using a connection service, send the connection state and exit code
                if (_remoteSessionManager.RemoteSession.State == RemoteSessionState.Disconnected &&
                    _remoteSessionManager.RemoteSession.ConnectionService)
                {
                    _connectionClient.SetConnectionState(
                        _remoteSessionManager.RemoteSession.Id,
                        string.IsNullOrEmpty(_remoteSessionManager.RemoteSession.VMAddress) ? _remoteSessionManager.RemoteSession.ServerAddress : _remoteSessionManager.RemoteSession.VMAddress,
                        GuidHelper.ConvertFromString(_remoteSessionManager.RemoteSession.VMGuid),
                        _remoteSessionManager.RemoteSession.State);

                    // CAUTION! exit code list is not exhaustive (that's why RemoteSession.ExitCode is an int)
                    RemoteSessionExitCode _exitCode;
                    if (!Enum.TryParse(_remoteSessionManager.RemoteSession.ExitCode.ToString(), out _exitCode))
                    {
                        _exitCode = RemoteSessionExitCode.Unknown;
                    }

                    _connectionClient.SetConnectionExitCode(
                        _remoteSessionManager.RemoteSession.Id,
                        string.IsNullOrEmpty(_remoteSessionManager.RemoteSession.VMAddress) ? _remoteSessionManager.RemoteSession.ServerAddress : _remoteSessionManager.RemoteSession.VMAddress,
                        GuidHelper.ConvertFromString(_remoteSessionManager.RemoteSession.VMGuid),
                        _exitCode);
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to cleanup disconnected session, remote session {0} ({1})", _remoteSessionManager.RemoteSession.Id, exc);
                throw;
            }
        }
    }
}