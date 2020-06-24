/*
    Myrtille: A native HTML4/5 Remote Desktop Protocol client.

    Copyright(c) 2014-2020 Cedric Coste

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
using System.Collections;
using System.Diagnostics;
using System.Web;
using System.Web.SessionState;

namespace Myrtille.Web
{
    public class RemoteSessionLongPollingHandler
    {
        private HttpContext _context;
        public HttpSessionState Session { get; private set; }
        private RemoteSession _remoteSession;

        public RemoteSessionLongPollingHandler(HttpContext context)
        {
            _context = context;
            Session = context.Session;

            try
            {
                if (_context.Session[HttpSessionStateVariables.RemoteSession.ToString()] == null)
                    throw new NullReferenceException();

                // retrieve the remote session for the given http session
                _remoteSession = (RemoteSession)_context.Session[HttpSessionStateVariables.RemoteSession.ToString()];

                // register the handler against the remote session manager
                lock (((ICollection)_remoteSession.Manager.LongPollings).SyncRoot)
                {
                    // search for a previously registered handler
                    RemoteSessionLongPollingHandler oldLongPolling = null;

                    foreach (var longPolling in _remoteSession.Manager.LongPollings)
                    {
                        if (longPolling.Session.SessionID == _context.Session.SessionID)
                        {
                            oldLongPolling = longPolling;
                            break;
                        }
                    }

                    // unregister the previous handler, if any
                    if (oldLongPolling != null)
                    {
                        _remoteSession.Manager.LongPollings.Remove(oldLongPolling);
                    }

                    // register this handler
                    _remoteSession.Manager.LongPollings.Add(this);
                }

                // the handler is ready to push data
                Send("<script>parent.lpInitConnection();</script>");
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to retrieve the remote session for the http session {0}, ({1})", _context.Session.SessionID, exc);
            }
        }

        private string GetImageText(RemoteSessionImage image)
        {
            return
                image.Idx + "," +
                image.PosX + "," +
                image.PosY + "," +
                image.Width + "," +
                image.Height + "," +
                "'" + image.Format.ToString().ToLower() + "'," +
                image.Quality + "," +
                image.Fullscreen.ToString().ToLower() + "," +
                "'" + Convert.ToBase64String(image.Data) + "'";
        }

        public void SendImage(RemoteSessionImage image)
        {
            Send("<script>parent.lpProcessImage(" + GetImageText(image) + ");</script>");
        }

        public void SendMessage(RemoteSessionMessage message)
        {
            switch (message.Type)
            {
                case MessageType.Connected:
                    Send("<script>parent.lpProcessMessage('connected');</script>");
                    break;

                case MessageType.Disconnected:
                    Send("<script>parent.lpProcessMessage('disconnected');</script>");
                    break;

                case MessageType.PageReload:
                    Send("<script>parent.lpProcessMessage('reload');</script>");
                    break;

                case MessageType.RemoteClipboard:
                    Send(string.Format("<script>parent.lpProcessMessage('clipboard|{0}');</script>", message.Text.Replace(@"\", @"\\").Replace("\r", @"\r").Replace("\n", @"\n").Replace("'", @"\'")));
                    break;

                case MessageType.TerminalOutput:
                    Send(string.Format("<script>parent.lpProcessMessage('term|{0}');</script>", message.Text.Replace(@"\", @"\\").Replace("\r", @"\r").Replace("\n", @"\n").Replace("'", @"\'")));
                    break;

                case MessageType.PrintJob:
                    Send(string.Format("<script>parent.lpProcessMessage('printjob|{0}');</script>", message.Text));
                    break;
            }
        }

        public void Send(string message)
        {
            try
            {
                if (_context.Response.IsClientConnected)
                {
                    _context.Response.Write(message);
                    _context.Response.Flush();
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to send long polling message for the remote session {0}, ({1})", _remoteSession.Id, exc);
            }
        }
    }
}