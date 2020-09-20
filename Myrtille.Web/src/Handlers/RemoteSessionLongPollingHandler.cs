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
using Newtonsoft.Json;

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
            Send(string.Format("<script>parent.lpProcessImage({0});</script>", GetImageText(image)));
        }

        public void SendMessage(RemoteSessionMessage message)
        {
            Send(string.Format("<script>parent.lpProcessMessage('{0}');</script>", JsonConvert.SerializeObject(message).Replace(@"\", @"\\").Replace("\r", @"\r").Replace("\n", @"\n").Replace("'", @"\'")));
        }

        public void Send(string data)
        {
            try
            {
                if (_context.Response.IsClientConnected)
                {
                    _context.Response.Write(data);
                    _context.Response.Flush();
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to send long polling data for the remote session {0}, ({1})", _remoteSession.Id, exc);
            }
        }
    }
}