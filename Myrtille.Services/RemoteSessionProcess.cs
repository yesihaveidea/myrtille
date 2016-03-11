/*
    Myrtille: A native HTML4/5 Remote Desktop Protocol client.

    Copyright(c) 2014-2016 Cedric Coste

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
using System.IO;
using System.Runtime.InteropServices;
using System.ServiceModel;
using Myrtille.Services.Contracts;

namespace Myrtille.Services
{
    public class RemoteSessionProcess : IRemoteSessionProcess, IDisposable
    {
        #region process

        private int _remoteSessionId;
        private Process _process;
        private IRemoteSessionProcessCallback _callback;

        //FreeRDP - A Free Remote Desktop Protocol Client
        //See http://freerdp.sourceforge.net for more information
        //Usage: xfreerdp [options] server:port
        //-a: color depth (8, 15, 16, 24 or 32)
        //-u: username
        //-p: password
        //-d: domain
        //-s: shell
        //-c: directory
        //-g: geometry, using format WxH or X%, default is 800x600
        //-t: alternative port number, default is 3389
        //-n: hostname
        //-o: console audio
        //-0: console session
        //-f: fullscreen mode
        //-z: enable bulk compression
        //-x: performance flags (m, b or l for modem, broadband or lan)
        //-i: remote session id
        //--no-rdp: disable Standard RDP encryption
        //--no-tls: disable TLS encryption
        //--no-nla: disable network level authentication
        //--sec: force protocol security (rdp, tls or nla)
        //--no-osb: disable off screen bitmaps, default on
        //--no-window: don't open a standard window for user interaction (freerdp runs as process only); also enforces no console window
        //--no-console: don't open a console window for debug output
        //--debug-log: write debug output to freerdp.log
        //--debug-log-process: write debug output to FreeRDP.wfreerdp.log; locate it into the parent "log" folder (among others myrtille logs) and stamp it with the FreeRDP.wfreerdp.exe process id
        //--version: Print out the version and exit
        //-h: show this help
        public void StartProcess(
            int remoteSessionId,
            string serverAddress,
            string userDomain,
            string userName,
            string userPassword,
            string clientWidth,
            string clientHeight,
            bool debug)
        {
            try
            {
                // set the remote session id
                // the wcf service binding "wsDualHttpBinding" is "perSession" by default (maintain 1 service instance per client)
                // as there is 1 client per remote session, the remote session id is set for the current service instance
                _remoteSessionId = remoteSessionId;

                // as the rdp server uses the client numlock state, ensure it's off
                // server side, ensure that HKEY_USERS\.DEFAULT\Control Panel\Keyboard: InitialKeyboardIndicators is set to 0 (numlock off)
                SetNumLock(false);

                _process = new Process();

                _process.StartInfo.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FreeRDP.wfreerdp.exe");

                _process.StartInfo.Arguments =
                    "-i " + _remoteSessionId +
                    " -z"+
                    " -x m" +
                    " -g " + (string.IsNullOrEmpty(clientWidth) ? "1024" : clientWidth) + "x" + (string.IsNullOrEmpty(clientHeight) ? "768" : clientHeight) +
                    " -a 16" +
                    (string.IsNullOrEmpty(userDomain) ? string.Empty : " -d " + userDomain) +
                    (string.IsNullOrEmpty(userName) ? string.Empty : " -u " + userName) +
                    (string.IsNullOrEmpty(userPassword) ? string.Empty : " -p " + userPassword) +
                    (string.IsNullOrEmpty(serverAddress) ? " localhost" : " " + serverAddress) +
                    " --no-tls --no-nla --sec-rdp --no-osb" +
                    (!debug ? " --no-window --no-console" : (!Environment.UserInteractive ? " --no-window --no-console --debug-log-process" : string.Empty));

                if (!debug || !Environment.UserInteractive)
                {
                    _process.StartInfo.UseShellExecute = false;
                    _process.StartInfo.RedirectStandardError = true;
                    _process.StartInfo.RedirectStandardInput = true;
                    _process.StartInfo.RedirectStandardOutput = true;
                    _process.StartInfo.CreateNoWindow = true;
                    _process.StartInfo.ErrorDialog = false;
                    _process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                }

                _process.EnableRaisingEvents = true;
                _process.Exited += ProcessExited;

                // set the callback instance
                // the wcf service binding "wsDualHttpBinding" allows for duplex communication
                _callback = OperationContext.Current.GetCallbackChannel<IRemoteSessionProcessCallback>();

                _process.Start();

                Trace.TraceInformation("Started rdp client process, remote session {0}", _remoteSessionId);
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to start rdp client process, remote session {0} ({1})", _remoteSessionId, exc);
            }
        }

        public void StopProcess()
        {
            // after closing the client, the rdp session does remains active on the server and is resumed on a subsequent connection of the same user...
            // to avoid this, we set the rdp session disconnect timeout to a low value (ie: 1 sec)
            // it can be done in the registry: HKLM\SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp, "MaxDisconnectionTime" (DWORD, value in msecs)
            // see http://ts.veranoest.net/ts_faq_configuration.htm

            if (_process != null && !_process.HasExited)
            {
                try
                {
                    _process.Kill();

                    Trace.TraceInformation("Stopped (killed) rdp client process, remote session {0}", _remoteSessionId);
                }
                catch (Exception exc)
                {
                    Trace.TraceError("Failed to stop (kill) rdp client process, remote session {0} ({1})", _remoteSessionId, exc);
                }
            }
        }

        /// <summary>
        /// the rdp client process has exited
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProcessExited(
            object sender,
            EventArgs e)
        {
            // rdp session close cases:
            // > user standard logout (using the windows "start" menu) or rdp session is killed server side: the rdp session is closed immediately, the rdp client process exits automatically
            // > user click the "Disconnect" button or rdp session is disconnected for any reason (i.e.: rdp server down, rdp connection lost, rdp client closed, etc.): rdp client process is closed, rdp session is closed 1 sec after (registry: HKLM\SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp, "MaxDisconnectionTime=1000" (DWORD, value in msecs))
            // > user is inactive: after a given idle timeout (registry: HKLM\SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp, "MaxIdleTime" (DWORD, value in msecs)), the user gets a 2mn warning before disconnection; if it aknowledges it, the session is resumed... otherwise it's closed 2mn after; the rdp client process exits automatically
            // > user close its browser/tab, goes offline or switch off its computer: the rdp session is closed after a given idle timeout (plus 2mn, because the user won't be able to acknowledge the warning message as mentioned above); the rdp client process exits automatically

            // also interesting to note, it's possible to set a MaxConnectionTime for the rdp session (registry: HKLM\SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp, "MaxConnectionTime" (DWORD, value in msecs))
            // an alternative to alter the registry directly (which impact the whole server) is to define group policies strategies (GPOs) into the Active Directory; it's a bit more complicated to handle, but proper...

            Trace.TraceInformation("Stopped rdp client process, remote session {0}", _remoteSessionId);

            try
            {
                // notify the remote session manager of the process exit
                _callback.ProcessExited();
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to notify rdp client process exit (MyrtilleAppPool down?), remote session {0} ({1})", _remoteSessionId, exc);
            }
            finally
            {
                if (_process != null)
                {
                    _process.Dispose();
                    _process = null;
                }
            }
        }

        #endregion

        #region NumLock

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
        private static extern short GetKeyState(int keyCode);

        [DllImport("user32.dll", EntryPoint = "keybd_event")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        private const byte VK_NUMLOCK = 0x90;
        private const uint KEYEVENTF_EXTENDEDKEY = 1;
        private const int KEYEVENTF_KEYDOWN = 0x0;
        private const int KEYEVENTF_KEYUP = 0x2;

        private static bool GetNumLock()
        {
            return (((ushort)GetKeyState(0x90)) & 0xffff) != 0;
        }

        private static void SetNumLock(bool state)
        {
            if (GetNumLock() != state)
            {
                keybd_event(VK_NUMLOCK, 0x45, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYDOWN, 0);
                keybd_event(VK_NUMLOCK, 0x45, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
            }
        }

        #endregion

        #region IDisposable

        ~RemoteSessionProcess()
        {
            Dispose();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            StopProcess();
        }

        #endregion
    }
}