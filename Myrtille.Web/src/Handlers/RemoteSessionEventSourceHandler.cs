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
    public class RemoteSessionEventSourceHandler
    {
        private HttpContext _context;
        public HttpSessionState Session { get; private set; }
        private RemoteSession _remoteSession;

        public RemoteSessionEventSourceHandler(HttpContext context)
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
                lock (((ICollection)_remoteSession.Manager.EventSources).SyncRoot)
                {
                    // search for a previously registered handler
                    RemoteSessionEventSourceHandler oldEventSource = null;

                    foreach (var eventSource in _remoteSession.Manager.EventSources)
                    {
                        if (eventSource.Session.SessionID == _context.Session.SessionID)
                        {
                            oldEventSource = eventSource;
                            break;
                        }
                    }

                    // unregister the previous handler, if any
                    if (oldEventSource != null)
                    {
                        _remoteSession.Manager.EventSources.Remove(oldEventSource);
                    }

                    // register this handler
                    _remoteSession.Manager.EventSources.Add(this);
                }

                // mime type for event source
                _context.Response.ContentType = "text/event-stream";
                _context.Response.Headers.Add("Content-Type", "text/event-stream\n\n");
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
                image.Format.ToString().ToLower() + "," +
                image.Quality + "," +
                image.Fullscreen.ToString().ToLower() + "," +
                Convert.ToBase64String(image.Data);
        }

        public void SendImage(RemoteSessionImage image)
        {
            Send(GetImageText(image) + ";");
        }

        public void SendMessage(RemoteSessionMessage message)
        {
            Send(string.Concat(message.Prefix, message.Text));
        }

        public void Send(string message)
        {
            try
            {
                if (_context.Response.IsClientConnected)
                {
                    _context.Response.Write("data: " + message + "\n\n");
                    _context.Response.Flush();
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to send event source message for the remote session {0}, ({1})", _remoteSession.Id, exc);
            }
        }
    }
}