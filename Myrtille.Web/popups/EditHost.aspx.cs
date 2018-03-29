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
using System.Web;
using System.Web.UI;
using Myrtille.Services.Contracts;

namespace Myrtille.Web
{
    public partial class EditHost : Page
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

                if (!IsPostBack && Request["edit"] == null)
                {
                    try
                    {
                        var host = _enterpriseClient.GetHost(_hostId.Value, _enterpriseSession.SessionID);
                        if (host != null)
                        {
                            hostName.Value = host.HostName;
                            hostAddress.Value = host.HostAddress;
                            groupsAccess.Value = host.DirectoryGroups;
                            securityProtocol.SelectedIndex = (int)host.Protocol;
                        }
                    }
                    catch (Exception exc)
                    {
                        System.Diagnostics.Trace.TraceError("Failed to retrieve host {0}, ({1})", _hostId, exc);
                    }
                }

                createSessionUrl.Attributes["onclick"] = string.Format("parent.openPopup('editHostSessionPopup', 'EditHostSession.aspx?hostId={0}');", _hostId);
            }
            else
            {
                createSessionUrl.Disabled = true;
                deleteHost.Disabled = true;
            }
        }

        /// <summary>
        /// create or edit a host
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void SaveHostButtonClick(
            object sender,
            EventArgs e)
        {
            if (_enterpriseClient == null || _enterpriseSession == null || string.IsNullOrEmpty(hostName.Value))
                return;

            try
            {
                if (!_hostId.HasValue)
                {
                    _enterpriseClient.AddHost(new EnterpriseHostEdit
                    {
                        HostID = 0,
                        HostName = hostName.Value,
                        HostAddress = hostAddress.Value,
                        DirectoryGroups = groupsAccess.Value,
                        Protocol = (SecurityProtocolEnum)securityProtocol.SelectedIndex
                    }, _enterpriseSession.SessionID);
                }
                else
                {
                    _enterpriseClient.UpdateHost(new EnterpriseHostEdit
                    {
                        HostID = _hostId.Value,
                        HostName = hostName.Value,
                        HostAddress = hostAddress.Value,
                        DirectoryGroups = groupsAccess.Value,
                        Protocol = (SecurityProtocolEnum)securityProtocol.SelectedIndex
                    }, _enterpriseSession.SessionID);
                }

                // refresh the hosts list
                Response.Redirect(Request.RawUrl + (Request.RawUrl.Contains("?") ? "&" : "?") + "edit=success");
            }
            catch (ThreadAbortException)
            {
                // occurs because the response is ended after redirect
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Failed to save host ({0})", exc);
            }
        }

        /// <summary>
        /// delete a host
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void DeleteHostButtonClick(
            object sender,
            EventArgs e)
        {
            if (_enterpriseClient == null || _enterpriseSession == null || !_hostId.HasValue)
                return;

            try
            {
                _enterpriseClient.DeleteHost(_hostId.Value, _enterpriseSession.SessionID);

                // refresh the hosts list
                Response.Redirect(Request.RawUrl + (Request.RawUrl.Contains("?") ? "&" : "?") + "edit=success");
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Failed to delete host {0} ({1})", _hostId.Value, exc);
            }
        }
    }
}