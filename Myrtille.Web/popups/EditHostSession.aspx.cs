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
using System.Web;
using System.Web.UI;
using Myrtille.Services.Contracts;

namespace Myrtille.Web
{
    public partial class EditHostSession : Page
    {
        private EnterpriseServiceClient _enterpriseClient;
        private EnterpriseSession _enterpriseSession;
        private long? _hostId = null;

        /// <summary>
        /// page init
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Page_Init(
            object sender,
            EventArgs e)
        {
            _enterpriseClient = new EnterpriseServiceClient();
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
            if (HttpContext.Current.Session[HttpSessionStateVariables.EnterpriseSession.ToString()] != null)
            {
                try
                {
                    _enterpriseSession = (EnterpriseSession)HttpContext.Current.Session[HttpSessionStateVariables.EnterpriseSession.ToString()];
                }
                catch (Exception exc)
                {
                    System.Diagnostics.Trace.TraceError("Failed to retrieve the enterprise session for the http session {0}, ({1})", HttpContext.Current.Session.SessionID, exc);
                }
            }

            // retrieve the host
            if (Request["hostId"] != null)
            {
                if (long.TryParse(Request["hostId"], out long lResult))
                {
                    _hostId = lResult;
                }

                try
                {
                    var host = _enterpriseClient.GetHost(_hostId.Value, _enterpriseSession.SessionID);
                    if (host != null)
                    {
                        hostName.InnerText = host.HostName;
                    }
                }
                catch (Exception exc)
                {
                    System.Diagnostics.Trace.TraceError("Failed to retrieve host {0}, ({1})", _hostId, exc);
                }
            }
        }

        /// <summary>
        /// create an host session URL
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void CreateSessionUrlButtonClick(
            object sender,
            EventArgs e)
        {
            if (_enterpriseClient == null || _enterpriseSession == null || !_hostId.HasValue || string.IsNullOrEmpty(userName.Value) || string.IsNullOrEmpty(userPassword.Value))
                return;

            try
            {
                var url = _enterpriseClient.CreateUserSession(_enterpriseSession.SessionID, _hostId.Value, userName.Value, userPassword.Value);
                if (!string.IsNullOrEmpty(url))
                {
                    sessionUrl.Value = Request.Url.Scheme + "://" + Request.Url.Host + (Request.Url.Port != 80 && Request.Url.Port != 443 ? ":" + Request.Url.Port : "") + "/" + Request.Url.Segments[1] + url + "&__EVENTTARGET=&__EVENTARGUMENT=&connect=Connect%21";
                }
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Failed to create session url for host {0} ({1})", _hostId, exc);
            }
        }
    }
}