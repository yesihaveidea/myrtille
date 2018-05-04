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
using System.Threading;
using System.Text;
using System.Web;
using System.Web.UI;
using Myrtille.Services.Contracts;
using Myrtille.Helpers;

namespace Myrtille.Web
{
    public partial class ShareSession : Page
    {
        private RemoteSession _remoteSession;

        /// <summary>
        /// page init
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Page_Init(
            object sender,
            EventArgs e)
        {
            
        }

        /// <summary>
        /// page load (postback data is now available)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Page_Load(
            object sender,
            EventArgs e)
        {
            // retrieve the active enterprise session, if any
            if (HttpContext.Current.Session[HttpSessionStateVariables.RemoteSession.ToString()] != null)
            {
                try
                {
                    _remoteSession = (RemoteSession)HttpContext.Current.Session[HttpSessionStateVariables.RemoteSession.ToString()];

                    if(_remoteSession.DisableSessionSharing(HttpContext.Current.Session.SessionID)) Response.Redirect("~/", true); 

                    _remoteSession.SessionKey = Guid.NewGuid().ToString();
                    HttpContext.Current.Session[HttpSessionStateVariables.RemoteSession.ToString()] = _remoteSession;

                    sessionUrl.Value = Request.Url.Scheme + "://" + Request.Url.Host + (Request.Url.Port != 80 && Request.Url.Port != 443 ? ":" + Request.Url.Port : "") + Request.ApplicationPath + "/?SSE=" + RDPCryptoHelper.GetSessionKey(_remoteSession.Id, HttpContext.Current.Session.SessionID, _remoteSession.SessionKey);
                }
                catch (Exception exc)
                {
                    System.Diagnostics.Trace.TraceError("Failed to retrieve the remote session for the http session {0}, ({1})", HttpContext.Current.Session.SessionID, exc);
                }
            }
            else
            {
                Response.Redirect("~/", true);
            }
        }

        
    }
}