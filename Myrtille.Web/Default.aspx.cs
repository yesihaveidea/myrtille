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
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Web.SessionState;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Myrtille.Helpers;
using Myrtille.Services.Contracts;

namespace Myrtille.Web
{
    public partial class Default : Page
    {
        private MFAAuthenticationClient _mfaAuthClient;
        private EnterpriseServiceClient _enterpriseClient;

        private EnterpriseSession _enterpriseSession;
        protected RemoteSession RemoteSession;

        /// <summary>
        /// page init
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Page_Init(
            object sender,
            EventArgs e)
        {
            _mfaAuthClient = new MFAAuthenticationClient();
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
            // prevent session fixation or stealing
            SessionFixationHandler();

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

            // retrieve the active remote session, if any
            if (HttpContext.Current.Session[HttpSessionStateVariables.RemoteSession.ToString()] != null)
            {
                try
                {
                    RemoteSession = (RemoteSession)HttpContext.Current.Session[HttpSessionStateVariables.RemoteSession.ToString()];
                }
                catch (Exception exc)
                {
                    System.Diagnostics.Trace.TraceError("Failed to retrieve the remote session for the http session {0}, ({1})", HttpContext.Current.Session.SessionID, exc);
                }
            }

            // postback events may redirect after execution; UI is updated from there
            if (!IsPostBack)
            {
                UpdateControls();
            }

            // disable the browser cache; in addition to a "noCache" dummy param, with current time, on long-polling and xhr requests
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
        }

        /// <summary>
        /// prevent http session fixation attack or stealing by generating a new http session ID upon login
        /// https://www.owasp.org/index.php/Session_Fixation
        /// </summary>
        private void SessionFixationHandler()
        {
            // register the current http session
            if (string.IsNullOrEmpty(HttpContext.Current.Request["oldSID"]))
            {
                try
                {
                    HttpContext.Current.Application.Lock();
                    var httpSessions = (IDictionary<string, HttpSessionState>)HttpContext.Current.Application[HttpApplicationStateVariables.HttpSessions.ToString()];
                    httpSessions[HttpContext.Current.Session.SessionID] = HttpContext.Current.Session;
                }
                catch (Exception exc)
                {
                    System.Diagnostics.Trace.TraceError("Failed to register http session ({0})", exc);
                }
                finally
                {
                    HttpContext.Current.Application.UnLock();
                }
            }
            // generate a new http session
            else
            {
                try
                {
                    HttpContext.Current.Application.Lock();

                    // retrieve the old http session id from url
                    var httpSessions = (IDictionary<string, HttpSessionState>)HttpContext.Current.Application[HttpApplicationStateVariables.HttpSessions.ToString()];
                    var httpSession = httpSessions[HttpContext.Current.Request["oldSID"]];

                    // retrieve the enterprise session bound to the old http session, if any
                    var enterpriseSession = httpSession[HttpSessionStateVariables.EnterpriseSession.ToString()];
                    if (enterpriseSession != null)
                    {
                        // unbind from the old http session
                        httpSession[HttpSessionStateVariables.EnterpriseSession.ToString()] = null;

                        // bind to the new http session
                        HttpContext.Current.Session[HttpSessionStateVariables.EnterpriseSession.ToString()] = enterpriseSession;
                    }

                    // retrieve the remote session bound to the old http session, if any
                    var remoteSession = httpSession[HttpSessionStateVariables.RemoteSession.ToString()];
                    if (remoteSession != null)
                    {
                        // unbind from the old http session
                        httpSession[HttpSessionStateVariables.RemoteSession.ToString()] = null;

                        // bind to the new http session
                        HttpContext.Current.Session[HttpSessionStateVariables.RemoteSession.ToString()] = remoteSession;
                    }

                    // unregister the old http session
                    httpSessions.Remove(httpSession.SessionID);

                    // remove the old http session id from url
                    Response.Redirect("?", true);
                }
                catch (ThreadAbortException)
                {
                    // occurs because the response is ended after redirect
                }
                catch (Exception exc)
                {
                    System.Diagnostics.Trace.TraceError("Failed to generate a new http session upon login ({0})", exc);
                }
                finally
                {
                    HttpContext.Current.Application.UnLock();
                }
            }
        }

        /// <summary>
        /// force remove the .net viewstate hidden fields from page (large bunch of unwanted data in url)
        /// </summary>
        /// <param name="writer"></param>
        protected override void Render(
            HtmlTextWriter writer)
        {
            var sb = new StringBuilder();
            var sw = new StringWriter(sb);
            var tw = new HtmlTextWriter(sw);
            base.Render(tw);
            var html = sb.ToString();
            html = Regex.Replace(html, "<input[^>]*id=\"(__VIEWSTATE)\"[^>]*>", string.Empty, RegexOptions.IgnoreCase);
            html = Regex.Replace(html, "<input[^>]*id=\"(__VIEWSTATEGENERATOR)\"[^>]*>", string.Empty, RegexOptions.IgnoreCase);
            writer.Write(html);
        }

        /// <summary>
        /// update the UI
        /// </summary>
        private void UpdateControls()
        {
            // hosts list
            if (_enterpriseSession != null && (RemoteSession == null || RemoteSession.State == RemoteSessionState.Disconnecting || RemoteSession.State == RemoteSessionState.Disconnected))
            {
                toolbar.Style["visibility"] = "hidden";
                toolbar.Style["display"] = "none";
                hosts.Visible = true;
                newHost.Visible = _enterpriseSession.IsAdmin;
                hostsList.DataSource = _enterpriseClient.GetSessionHosts(_enterpriseSession.SessionID);
                hostsList.DataBind();
            }
            // active remote session
            else if (RemoteSession != null && (RemoteSession.State == RemoteSessionState.Connecting || RemoteSession.State == RemoteSessionState.Connected))
            {
                toolbar.Style["visibility"] = "visible";
                toolbar.Style["display"] = "block";
                serverInfo.Value = RemoteSession.ServerAddress;
                stat.Value = RemoteSession.StatMode ? "Hide Stat" : "Show Stat";
                stat.Disabled = false;
                debug.Value = RemoteSession.DebugMode ? "Hide Debug" : "Show Debug";
                debug.Disabled = false;
                browser.Value = RemoteSession.CompatibilityMode ? "HTML5" : "HTML4";
                browser.Disabled = false;
                scale.Value = RemoteSession.ScaleDisplay ? "Unscale" : "Scale";
                scale.Disabled = false;
                keyboard.Disabled = false;
                clipboard.Disabled = !RemoteSession.AllowRemoteClipboard;
                files.Disabled = RemoteSession.ServerAddress.ToLower() != "localhost" && RemoteSession.ServerAddress != "127.0.0.1" && RemoteSession.ServerAddress != "[::1]" && RemoteSession.ServerAddress != HttpContext.Current.Request.Url.Host && string.IsNullOrEmpty(RemoteSession.UserDomain);
                cad.Disabled = false;
                mrc.Disabled = false;
                disconnect.Disabled = false;
            }
            // login screen
            else
            {
                login.Visible = true;

                // MFA
                if (_mfaAuthClient.GetState())
                {
                    mfaDiv.Visible = true;
                    mfaProvider.InnerText = _mfaAuthClient.GetPromptLabel();
                    mfaProvider.HRef = _mfaAuthClient.GetProviderURL();
                }

                // enterprise mode
                if (_enterpriseClient.GetState())
                {
                    domainServerDiv.Visible = false;
                }
                // standard mode
                else
                {
                    connect.Attributes["onclick"] = "showToolbar();";
                }
            }
        }

        /// <summary>
        /// enterprise mode from url: load the enterprise session (from querystring param) and proceed to connection; the user is non admin and the url is only usable once
        /// enterprise mode from login: authenticate the user against the enterprise active directory and list the servers available to the user; the user is admin if member of the "EnterpriseAdminGroup" defined into myrtille services config
        /// standard mode: connect the specified server; the rdp authentication is delegated to the rdp server or connection broker (if applicable)
        /// if MFA is enabled and not already processed, authenticate the user against the configured MFA provider (OTP preferred)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void ConnectButtonClick(
        object sender,
        EventArgs e)
        {
            // one time usage enterprise session url
            if (Request["SI"] != null && Request["SD"] != null && Request["SK"] != null)
            {
                CreateEnterpriseSessionFromUrl();
            }

            // MFA (OTP passcode)
            if (_enterpriseSession == null && _mfaAuthClient.GetState())
            {
                var clientIP = ClientIPHelper.ClientIPFromRequest(new HttpContextWrapper(HttpContext.Current).Request, true, new string[] { });
                if (!_mfaAuthClient.Authenticate(user.Value, mfaPassword.Value, clientIP))
                {
                    connectError.InnerText = "MFA Authentication failed!";
                    UpdateControls();
                    return;
                }
            }

            // enterprise mode from login
            if (_enterpriseSession == null && _enterpriseClient.GetState())
            {
                CreateEnterpriseSessionFromLogin();
            }
            // connection from:
            // > standard mode
            // > enterprise mode: hosts list
            // > enterprise mode: one time session url
            else
            {
                // the display size is required to start a remote session
                // if missing, the client will provide it automatically
                if (string.IsNullOrEmpty(width.Value) || string.IsNullOrEmpty(height.Value))
                {
                    return;
                }

                // connect
                if (ConnectRemoteServer())
                {
                    try
                    {
                        // in enterprise mode from login, a new http session was already generated (no need to do it each time an host is connected!)
                        // in standard mode or enterprise mode from url, a new http session must be generated
                        if (_enterpriseSession == null || Request["SI"] != null)
                        {
                            // cancel the current http session
                            HttpContext.Current.Session.Abandon();

                            // prevent session fixation attack by generating a new session ID upon login
                            // also, using http get method to prevent the browser asking for http post data confirmation if the page is reloaded
                            // https://www.owasp.org/index.php/Session_Fixation
                            Response.Redirect(string.Format("?oldSID={0}", HttpContext.Current.Session.SessionID), true);
                        }
                        // remove the host id from url
                        else
                        {
                            Response.Redirect("?", true);
                        }
                    }
                    catch (ThreadAbortException)
                    {
                        // occurs because the response is ended after redirect
                    }
                }
                // connection failed from the hosts list or from a one time session url
                else if (_enterpriseSession != null && Request["SD"] != null)
                {
                    try
                    {
                        // remove the host id from url
                        Response.Redirect("?", true);
                    }
                    catch (ThreadAbortException)
                    {
                        // occurs because the response is ended after redirect
                    }
                }
            }
        }

        /// <summary>
        /// connect the rdp server
        /// </summary>
        /// <remarks>
        /// the rdp authentication is delegated to the rdp server or connection broker (if applicable)
        /// </remarks>
        private bool ConnectRemoteServer()
        {
            // connection parameters
            var loginServer = string.IsNullOrEmpty(server.Value) ? "localhost" : server.Value;
            var loginDomain = domain.Value;
            var loginUser = user.Value;
            var loginPassword = string.IsNullOrEmpty(passwordHash.Value) ? password.Value : RDPCryptoHelper.DecryptPassword(passwordHash.Value);
            var loginProtocol = SecurityProtocolEnum.auto;

            // connect an host from the hosts list or from a one time session url
            if (_enterpriseSession != null && Request["SD"] != null)
            {
                long hostId = 0;
                if (long.TryParse(Request["SD"], out long lResult))
                {
                    hostId = lResult;
                }

                try
                {
                    // retrieve the host connection details
                    var connection = _enterpriseClient.GetSessionConnectionDetails(_enterpriseSession.SessionID, hostId, _enterpriseSession.SessionKey);
                    if (connection == null)
                    {
                        System.Diagnostics.Trace.TraceInformation("Unable to retrieve host {0} connection details (invalid host or one time session url already used?)", hostId);
                        return false;
                    }

                    loginServer = !string.IsNullOrEmpty(connection.HostAddress) ? connection.HostAddress : connection.HostName;
                    loginDomain = string.Empty;     // domain is defined into myrtille services config
                    loginUser = connection.Username;
                    loginPassword = RDPCryptoHelper.DecryptPassword(connection.Password);
                    loginProtocol = connection.Protocol;
                }
                catch (Exception exc)
                {
                    System.Diagnostics.Trace.TraceError("Failed to retrieve host {0} connection details ({1})", hostId, exc);
                    return false;
                }
            }

            // remote clipboard access
            var allowRemoteClipboard = true;
            if (bool.TryParse(ConfigurationManager.AppSettings["allowRemoteClipboard"], out bool bResult))
            {
                allowRemoteClipboard = bResult;
            }

            // remove any active remote session (disconnected?)
            if (RemoteSession != null)
            {
                // unset the remote session for the current http session
                HttpContext.Current.Session[HttpSessionStateVariables.RemoteSession.ToString()] = null;
                RemoteSession = null;
            }

            // create a new remote session
            try
            {
                HttpContext.Current.Application.Lock();

                // auto-increment the remote sessions counter
                // note that it doesn't really count the active remote sessions... it's just an auto-increment for the remote session id, ensuring it's unique...
                var remoteSessionsCounter = (int)HttpContext.Current.Application[HttpApplicationStateVariables.RemoteSessionsCounter.ToString()];
                remoteSessionsCounter++;

                // create the remote session
                RemoteSession = new RemoteSession
                {
                    Id = remoteSessionsCounter,
                    State = RemoteSessionState.NotConnected,
                    ServerAddress = loginServer,
                    UserDomain = loginDomain,
                    UserName = loginUser,
                    UserPassword = loginPassword,
                    ClientWidth = int.Parse(width.Value),
                    ClientHeight = int.Parse(height.Value),
                    StartProgram = program.Value,
                    AllowRemoteClipboard = allowRemoteClipboard,
                    SecurityProtocol = loginProtocol
                };

                // bind the remote session to the current http session
                HttpContext.Current.Session[HttpSessionStateVariables.RemoteSession.ToString()] = RemoteSession;

                // update the remote sessions auto-increment counter
                HttpContext.Current.Application[HttpApplicationStateVariables.RemoteSessionsCounter.ToString()] = remoteSessionsCounter;
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Failed to create remote session ({0})", exc);
                RemoteSession = null;
            }
            finally
            {
                HttpContext.Current.Application.UnLock();
            }

            // connect it
            if (RemoteSession != null)
            {
                try
                {
                    // update the remote session state
                    RemoteSession.State = RemoteSessionState.Connecting;

                    // create pipes for the web gateway and the rdp client to talk
                    RemoteSession.Manager.Pipes.CreatePipes();

                    // the rdp client does connect the pipes when it starts; when it stops (either because it was closed, crashed or because the rdp session had ended), pipes are released
                    // as the process command line can be displayed into the task manager / process explorer, the connection settings (including user credentials) are now passed to the rdp client through the inputs pipe
                    // use http://technet.microsoft.com/en-us/sysinternals/dd581625 to track the existing pipes
                    RemoteSession.Manager.Client.StartProcess(
                        RemoteSession.Id,
                        RemoteSession.ServerAddress,
                        RemoteSession.UserDomain,
                        RemoteSession.UserName,
                        RemoteSession.StartProgram,
                        RemoteSession.ClientWidth,
                        RemoteSession.ClientHeight,
                        RemoteSession.AllowRemoteClipboard,
                        RemoteSession.SecurityProtocol);
                }
                catch (Exception exc)
                {
                    System.Diagnostics.Trace.TraceError("Failed to connect the remote session {0} ({1})", RemoteSession.Id, exc);
                    connectError.InnerText = "Failed to connect! ensure myrtille services are running";
                    return false;
                }
            }
            else
            {
                connectError.InnerText = "Failed to create remote session!";
                return false;
            }

            return true;
        }

        /// <summary>
        /// disconnect the rdp server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void DisconnectButtonClick(
            object sender,
            EventArgs e)
        {
            // disconnect the active remote session, if any and connecting/connected
            if (RemoteSession != null && (RemoteSession.State == RemoteSessionState.Connecting || RemoteSession.State == RemoteSessionState.Connected))
            {
                try
                {
                    // update the remote session state
                    RemoteSession.State = RemoteSessionState.Disconnecting;

                    // send a disconnect command to the rdp client
                    RemoteSession.Manager.SendCommand(RemoteSessionCommand.CloseRdpClient);

                    // if running in enterprise mode, redirect to the hosts list
                    // otherwise, redirect to the login screen
                    Response.Redirect("?", true);
                }
                catch (ThreadAbortException)
                {
                    // occurs because the response is ended after redirect
                }
                catch (Exception exc)
                {
                    System.Diagnostics.Trace.TraceError("Failed to disconnect the remote session {0} ({1})", RemoteSession.Id, exc);
                }
            }
        }

        #region enterprise mode

        /// <summary>
        /// create an enterprise session from a one time url
        /// </summary>
        private void CreateEnterpriseSessionFromUrl()
        {
            try
            {
                // create enterprise session from querystring params
                _enterpriseSession = new EnterpriseSession
                {
                    IsAdmin = false,    // simple host connection only (no hosts management)
                    SessionID = Request["SI"],
                    SessionKey = Request["SK"]
                };

                // bind the enterprise session to the current http session
                HttpContext.Current.Session[HttpSessionStateVariables.EnterpriseSession.ToString()] = _enterpriseSession;
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Failed to create enterprise session from url ({0})", exc);
            }
        }

        /// <summary>
        /// authenticate the user against the enterprise active directory and list the servers available to the user
        /// </summary>
        private void CreateEnterpriseSessionFromLogin()
        {
            try
            {
                // authenticate the user against the enterprise active directory
                _enterpriseSession = _enterpriseClient.Authenticate(user.Value, password.Value);
                if (_enterpriseSession == null)
                {
                    connectError.InnerText = "Active Directory Authentication failed!";
                    UpdateControls();
                    return;
                }

                // bind the enterprise session to the current http session
                HttpContext.Current.Session[HttpSessionStateVariables.EnterpriseSession.ToString()] = _enterpriseSession;

                // cancel the current http session
                HttpContext.Current.Session.Abandon();

                // prevent session fixation attack by generating a new session ID upon login
                // also, using http get method to prevent the browser asking for http post data confirmation if the page is reloaded
                // https://www.owasp.org/index.php/Session_Fixation
                Response.Redirect(string.Format("?oldSID={0}", HttpContext.Current.Session.SessionID), true);
            }
            catch (ThreadAbortException)
            {
                // occurs because the response is ended after redirect
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Failed to create enterprise session from login ({0})", exc);
            }
        }

        /// <summary>
        /// populate the enterprise session hosts list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void hostsList_ItemDataBound(
            object sender,
            RepeaterItemEventArgs e)
        {
            try
            {
                var host = e.Item.DataItem as EnterpriseHost;

                var hostLink = e.Item.FindControl("hostLink") as HtmlAnchor;
                hostLink.HRef = string.Format("?SD={0}&__EVENTTARGET=&__EVENTARGUMENT=&connect=Connect%21", host.HostID);
                hostLink.Attributes["class"] = "hostLink";

                var hostName = e.Item.FindControl("hostName") as HtmlGenericControl;
                hostName.InnerText = host.HostName;
                if (_enterpriseSession.IsAdmin)
                {
                    hostName.Attributes["class"] = "hostName";
                    hostName.Attributes["onclick"] = string.Format("openPopup('editHostPopup', 'EditHost.aspx?hostId={0}');", host.HostID);
                }
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Failed to populate hosts for the enterprise session {0} ({1})", _enterpriseSession.SessionID, exc);
            }
        }

        /// <summary>
        /// logout the enterprise session
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void LogoutButtonClick(
            object sender,
            EventArgs e)
        {
            if (_enterpriseSession == null)
                return;

            try
            {
                // logout the enterprise session
                _enterpriseClient.Logout(_enterpriseSession.SessionID);

                // unbind the enterprise session from the current http session
                HttpContext.Current.Session[HttpSessionStateVariables.EnterpriseSession.ToString()] = null;

                // redirect to the login screen
                Response.Redirect("?", true);
            }
            catch (ThreadAbortException)
            {
                // occurs because the response is ended after redirect
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Failed to logout the enterprise session {0} ({1})", _enterpriseSession.SessionID, exc);
            }
        }

        #endregion
    }
}