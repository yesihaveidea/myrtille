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
using System.Web.UI;

namespace Myrtille.Web
{
    public partial class SendInputs : Page
    {
        /// <summary>
        /// send user input(s) (mouse, keyboard) to the rdp session
        /// if long-polling is disabled (xhr only), also returns image data within the response
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Page_Load(
            object sender,
            EventArgs e)
        {
            // if cookies are enabled, the http session id is added to the http request headers; otherwise, it's added to the http request url
            // in both cases, the given http session is automatically bound to the current http context

            RemoteSession remoteSession = null;

            try
            {
                if (Session[HttpSessionStateVariables.RemoteSession.ToString()] == null)
                    throw new NullReferenceException();

                // retrieve the remote session for the current http session
                remoteSession = (RemoteSession)Session[HttpSessionStateVariables.RemoteSession.ToString()];

                try
                {
                    // retrieve params
                    var data = Request.QueryString["data"];
                    var imgIdx = int.Parse(Request.QueryString["imgIdx"]);
                    var imgReturn = int.Parse(Request.QueryString["imgReturn"]) == 1;

                    // process input(s)
                    if (!string.IsNullOrEmpty(data))
                    {
                        remoteSession.Manager.ProcessInputs(Session, data);
                    }

                    // xhr only
                    if (imgReturn)
                    {
                        // reload page
                        if (remoteSession.Manager.ReloadPage)
                        {
                            Response.Write("reload");
                            remoteSession.Manager.ReloadPage = false;
                        }
                        // remote clipboard
                        else if (remoteSession.Manager.ClipboardAvailable)
                        {
                            Response.Write(string.Format("clipboard|{0}", remoteSession.Manager.ClipboardText));
                            remoteSession.Manager.ClipboardAvailable = false;
                        }
                        // print job
                        else if (remoteSession.Manager.PrintJobAvailable)
                        {
                            Response.Write(string.Format("printjob|{0}", remoteSession.Manager.PrintJobName));
                            remoteSession.Manager.PrintJobAvailable = false;
                        }
                        // disconnected session
                        else if (remoteSession.State == RemoteSessionState.Disconnected)
                        {
                            Response.Write("disconnected");
                        }
                        // next image
                        else
                        {
                            var image = remoteSession.Manager.GetNextUpdate(imgIdx);
                            if (image != null)
                            {
                                System.Diagnostics.Trace.TraceInformation("Returning image {0} ({1}), remote session {2}", image.Idx, (image.Fullscreen ? "screen" : "region"), remoteSession.Id);

                                var imgData =
                                    image.Idx + "," +
                                    image.PosX + "," +
                                    image.PosY + "," +
                                    image.Width + "," +
                                    image.Height + "," +
                                    image.Format.ToString().ToLower() + "," +
                                    image.Quality + "," +
                                    image.Fullscreen.ToString().ToLower() + "," +
                                    Convert.ToBase64String(image.Data);

                                // write the output
                                Response.Write(imgData);
                            }
                        }
                    }
                }
                catch (Exception exc)
                {
                    System.Diagnostics.Trace.TraceError("Failed to send user input(s), remote session {0} ({1})", remoteSession.Id, exc);
                }
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Failed to retrieve the active remote session ({0})", exc);
            }
        }
    }
}