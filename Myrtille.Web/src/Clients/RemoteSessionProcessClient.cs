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
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.ServiceModel;
using System.Threading;
using System.Web;
using Myrtille.Helpers;
using Myrtille.Services.Contracts;
using Myrtille.Web.Properties;

namespace Myrtille.Web
{
    public class RemoteSessionProcessClient : DuplexClientBase<IRemoteSessionProcess>, IRemoteSessionProcess
    {
        private RemoteSessionManager _remoteSessionManager;
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

    [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
    public class RemoteSessionProcessClientCallback : IRemoteSessionProcessCallback
    {
        private ConnectionClient _connectionClient = new ConnectionClient(Settings.Default.ConnectionServiceUrl);
        private ApplicationPoolClient _applicationPoolClient = new ApplicationPoolClient();

        private RemoteSessionManager _remoteSessionManager;
        private HttpApplicationState _application;

        public RemoteSessionProcessClientCallback(RemoteSessionManager remoteSessionManager, HttpApplicationState application)
        {
            _remoteSessionManager = remoteSessionManager;
            _application = application;
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
                    _remoteSessionManager.ClientIdleTimeout.Dispose();
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
                    if (_remoteSessionManager.ClientIdleTimeout != null)
                    {
                        _remoteSessionManager.ClientIdleTimeout = new CancellationTokenSource();
                    }
                    _remoteSessionManager.RemoteSession.State = RemoteSessionState.Connecting;
                }

                // send a disconnect notification to the browser
                _remoteSessionManager.SendMessage(new RemoteSessionMessage { Type = MessageType.Disconnected, Prefix = "disconnected" });

                if (_remoteSessionManager.RemoteSession.State == RemoteSessionState.Disconnected)
                {
                    // if using a connection service, send the connection state and exit code
                    if (_remoteSessionManager.RemoteSession.ConnectionService)
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

                    // cleanup
                    try
                    {
                        _application.Lock();

                        // unregister the remote session at the application level
                        var remoteSessions = (IDictionary<Guid, RemoteSession>)_application[HttpApplicationStateVariables.RemoteSessions.ToString()];

                        if (remoteSessions.ContainsKey(_remoteSessionManager.RemoteSession.Id))
                        {
                            remoteSessions.Remove(_remoteSessionManager.RemoteSession.Id);
                        }


                        // recycle the application pool when there is no active remote session
                        bool idleAppPoolRecycling;
                        if (!bool.TryParse(ConfigurationManager.AppSettings["IdleAppPoolRecycling"], out idleAppPoolRecycling))
                        {
                            idleAppPoolRecycling = true;
                        }

                        /*

                        it may seem a bit extreme, but the garbage collector doesn't return the freed memory to the operating system
                        instead, it makes it available to the memory workspace of the application pool process, which in turn uses it for faster memory allocation later
                        while this is fine for most usage, this becomes critical when the OS is under memory pressure
                        if that occurs, the process is meant to return its unused memory to the OS
                        in reality, this is not always true; so the system becomes slow (hdd swap) and unstable

                        memory usage of a process under Windows: https://dzone.com/articles/windows-process-memory-usage-demystified
                        tool: https://technet.microsoft.com/en-us/sysinternals/vmmap.aspx
                        garbage collector: https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/fundamentals
                        reallocation of freed memory: https://stackoverflow.com/questions/28614210/why-doesnt-net-release-unused-memory-back-to-os-when-physical-95

                        */

                        if (idleAppPoolRecycling && remoteSessions.Count == 0)
                        {
                            // give time for the browser to receive the disconnect notification and get back to the login page
                            // the browser window/tab may also be closed, but there is no way to know it at this step
                            Thread.Sleep(2000);

                            // the gateway doesn't have enough rights to recycle the application pool, this is delegated to the myrtille services
                            _applicationPoolClient.RecycleApplicationPool(Environment.UserName);
                        }
                    }
                    catch (Exception exc)
                    {
                        Trace.TraceError("Failed to delete remote session ({0})", exc);
                    }
                    finally
                    {
                        _application.UnLock();
                    }

                    // the remote session is still bound to an http session
                    // if the related page is still active, the page will end the cleanup (unbind the remote session from the http session)
                    // otherwise (the browser window/tab was closed), the cleanup will be done automatically when the http session expires
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