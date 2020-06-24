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
using System.Diagnostics;
using System.Threading;
using System.Web;
using System.Web.SessionState;

namespace Myrtille.Web
{
    public class EventSourceHandler : IHttpHandler, IReadOnlySessionState
    {
        public void ProcessRequest(HttpContext context)
        {
            var handler = new RemoteSessionEventSourceHandler(context);

            try
            {
                // keep the http context open as long as the http client is connected
                while (context.Response.IsClientConnected)
                {
                    Thread.Sleep(1000);
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("event source error for http session {0} ({1})", context.Session.SessionID, exc);
            }

            Trace.TraceInformation("event source closed for http session {0}", context.Session.SessionID);
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}