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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;

namespace Myrtille.Web
{
    public partial class Default : Page
    {
        protected RemoteSessionManager RemoteSessionManager;

        /// <summary>
        /// initialization
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Page_Load(
            object sender,
            EventArgs e)
        {
            try
            {
                // retrieve the active remote session, if any
                if (HttpContext.Current.Session[HttpSessionStateVariables.RemoteSessionManager.ToString()] != null)
                {
                    try
                    {
                        RemoteSessionManager = (RemoteSessionManager)HttpContext.Current.Session[HttpSessionStateVariables.RemoteSessionManager.ToString()];
                    }
                    catch (Exception exc)
                    {
                        System.Diagnostics.Trace.TraceError("Failed to retrieve remote session manager ({0})", exc);
                    }
                }

                // update controls
                UpateControls();

                // disable the browser cache; in addition to a "noCache" dummy param, with current time, on long-polling and xhr requests
                Response.Cache.SetCacheability(HttpCacheability.NoCache);
                Response.Cache.SetNoStore();
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Failed to load myrtille ({0})", exc);
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
        private void UpateControls()
        {
            if (RemoteSessionManager != null)
            {
                // control div
                controlDiv.Attributes["class"] = RemoteSessionManager.RemoteSession.State != RemoteSessionState.Connecting && RemoteSessionManager.RemoteSession.State != RemoteSessionState.Connected ? "controlDiv" : null;

                // rdp settings
                serverLabel.Visible = RemoteSessionManager.RemoteSession.State != RemoteSessionState.Connecting && RemoteSessionManager.RemoteSession.State != RemoteSessionState.Connected;
                serverText.Disabled = RemoteSessionManager.RemoteSession.State == RemoteSessionState.Connecting || RemoteSessionManager.RemoteSession.State == RemoteSessionState.Connected;
                serverText.Attributes["class"] = serverLabel.Visible ? "controlText" : null;
                if (serverText.Disabled)
                    serverText.Value = RemoteSessionManager.RemoteSession.ServerAddress;

                domainLabel.Visible = RemoteSessionManager.RemoteSession.State != RemoteSessionState.Connecting && RemoteSessionManager.RemoteSession.State != RemoteSessionState.Connected;
                domainText.Disabled = RemoteSessionManager.RemoteSession.State == RemoteSessionState.Connecting || RemoteSessionManager.RemoteSession.State == RemoteSessionState.Connected;
                domainText.Attributes["class"] = domainLabel.Visible ? "controlText" : null;
                if (domainText.Disabled)
                    domainText.Value = RemoteSessionManager.RemoteSession.UserDomain;

                userLabel.Visible = RemoteSessionManager.RemoteSession.State != RemoteSessionState.Connecting && RemoteSessionManager.RemoteSession.State != RemoteSessionState.Connected;
                userText.Disabled = RemoteSessionManager.RemoteSession.State == RemoteSessionState.Connecting || RemoteSessionManager.RemoteSession.State == RemoteSessionState.Connected;
                userText.Attributes["class"] = userLabel.Visible ? "controlText" : null;
                if (userText.Disabled)
                    userText.Value = RemoteSessionManager.RemoteSession.UserName;

                passwordLabel.Visible = RemoteSessionManager.RemoteSession.State != RemoteSessionState.Connecting && RemoteSessionManager.RemoteSession.State != RemoteSessionState.Connected;
                passwordText.Disabled = RemoteSessionManager.RemoteSession.State == RemoteSessionState.Connecting || RemoteSessionManager.RemoteSession.State == RemoteSessionState.Connected;
                passwordText.Attributes["class"] = passwordLabel.Visible ? "controlText" : null;
                if (passwordText.Disabled)
                    passwordText.Value = RemoteSessionManager.RemoteSession.UserPassword;

                statsLabel.Visible = RemoteSessionManager.RemoteSession.State != RemoteSessionState.Connecting && RemoteSessionManager.RemoteSession.State != RemoteSessionState.Connected;
                statSelect.Disabled = RemoteSessionManager.RemoteSession.State == RemoteSessionState.Connecting || RemoteSessionManager.RemoteSession.State == RemoteSessionState.Connected;
                statSelect.Attributes["class"] = statsLabel.Visible ? "controlSelect" : null;
                if (statSelect.Disabled)
                    statSelect.Value = RemoteSessionManager.RemoteSession.StatMode ? "Stat enabled" : "Stat disabled";

                debugLabel.Visible = RemoteSessionManager.RemoteSession.State != RemoteSessionState.Connecting && RemoteSessionManager.RemoteSession.State != RemoteSessionState.Connected;
                debugSelect.Disabled = RemoteSessionManager.RemoteSession.State == RemoteSessionState.Connecting || RemoteSessionManager.RemoteSession.State == RemoteSessionState.Connected;
                debugSelect.Attributes["class"] = debugLabel.Visible ? "controlSelect" : null;
                if (debugSelect.Disabled)
                    debugSelect.Value = RemoteSessionManager.RemoteSession.DebugMode ? "Debug enabled" : "Debug disabled";

                browserLabel.Visible = RemoteSessionManager.RemoteSession.State != RemoteSessionState.Connecting && RemoteSessionManager.RemoteSession.State != RemoteSessionState.Connected;
                browserSelect.Disabled = RemoteSessionManager.RemoteSession.State == RemoteSessionState.Connecting || RemoteSessionManager.RemoteSession.State == RemoteSessionState.Connected;
                browserSelect.Attributes["class"] = browserLabel.Visible ? "controlSelect" : null;
                if (browserSelect.Disabled)
                    browserSelect.Value = RemoteSessionManager.RemoteSession.CompatibilityMode ? "HTML4" : "HTML5";

                // connect/disconnect
                connect.Visible = RemoteSessionManager.RemoteSession.State != RemoteSessionState.Connecting && RemoteSessionManager.RemoteSession.State != RemoteSessionState.Connected;
                disconnect.Visible = RemoteSessionManager.RemoteSession.State == RemoteSessionState.Connecting || RemoteSessionManager.RemoteSession.State == RemoteSessionState.Connected;

                // virtual keyboard
                keyboard.Visible = RemoteSessionManager.RemoteSession.State == RemoteSessionState.Connecting || RemoteSessionManager.RemoteSession.State == RemoteSessionState.Connected;

                // remote clipboard
                clipboard.Visible = RemoteSessionManager.RemoteSession.State == RemoteSessionState.Connecting || RemoteSessionManager.RemoteSession.State == RemoteSessionState.Connected;

                // file storage
                files.Visible = (RemoteSessionManager.RemoteSession.State == RemoteSessionState.Connecting || RemoteSessionManager.RemoteSession.State == RemoteSessionState.Connected) &&
                    (RemoteSessionManager.RemoteSession.ServerAddress.ToLower() == "localhost" || RemoteSessionManager.RemoteSession.ServerAddress == "127.0.0.1" || RemoteSessionManager.RemoteSession.ServerAddress == HttpContext.Current.Request.Url.Host || !string.IsNullOrEmpty(RemoteSessionManager.RemoteSession.UserDomain)) &&
                    !string.IsNullOrEmpty(RemoteSessionManager.RemoteSession.UserName) && !string.IsNullOrEmpty(RemoteSessionManager.RemoteSession.UserPassword);

                // ctrl+alt+del
                cad.Visible = RemoteSessionManager.RemoteSession.State == RemoteSessionState.Connecting || RemoteSessionManager.RemoteSession.State == RemoteSessionState.Connected;
            }
        }

        /// <summary>
        /// start the rdp session
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void ConnectButtonClick(
            object sender,
            EventArgs e)
        {
            // remove a previously disconnected remote session
            if (RemoteSessionManager != null && RemoteSessionManager.RemoteSession.State == RemoteSessionState.Disconnected)
            {
                try
                {
                    HttpContext.Current.Application.Lock();

                    // unset the remote session manager for the current http session
                    HttpContext.Current.Session[HttpSessionStateVariables.RemoteSessionManager.ToString()] = null;

                    // unregister it at application level; used when there is no http context (i.e.: websockets)
                    var remoteSessionsManagers = (Dictionary<string, RemoteSessionManager>)HttpContext.Current.Application[HttpApplicationStateVariables.RemoteSessionsManagers.ToString()];
                    if (remoteSessionsManagers.ContainsKey(HttpContext.Current.Session.SessionID))
                    {
                        remoteSessionsManagers.Remove(HttpContext.Current.Session.SessionID);
                    }
                }
                catch (Exception exc)
                {
                    System.Diagnostics.Trace.TraceError("Failed to remove remote session ({0})", exc);
                }
                finally
                {
                    RemoteSessionManager = null;
                    HttpContext.Current.Application.UnLock();
                }
            }

            // create a new remote session, if none active
            if (RemoteSessionManager == null)
            {
                try
                {
                    HttpContext.Current.Application.Lock();

                    // auto-increment the remote sessions counter
                    // note that it doesn't really count the active remote sessions... it's just an auto-increment for the remote session id, ensuring it's unique...
                    // the active remote sessions are registered in HttpContext.Current.Application[HttpApplicationStateVariables.RemoteSessionsManagers.ToString()]; count can be retrieved from there
                    var remoteSessionsCounter = (int)HttpContext.Current.Application[HttpApplicationStateVariables.RemoteSessionsCounter.ToString()];
                    remoteSessionsCounter++;

                    // create the remote session manager
                    RemoteSessionManager = new RemoteSessionManager(
                        new RemoteSession
                        {
                            Id = remoteSessionsCounter,
                            State = RemoteSessionState.NotConnected,
                            ServerAddress = serverText.Value,
                            UserDomain = domainText.Value,
                            UserName = userText.Value,
                            UserPassword = passwordText.Value,
                            ClientWidth = width.Value,
                            ClientHeight = height.Value,
                            StatMode = statSelect.Value == "Stat enabled",
                            DebugMode = debugSelect.Value == "Debug enabled",
                            CompatibilityMode = browserSelect.Value == "HTML4"
                        }
                    );

                    // set the remote session manager for the current http session
                    HttpContext.Current.Session[HttpSessionStateVariables.RemoteSessionManager.ToString()] = RemoteSessionManager;

                    // register it at application level; used when there is no http context (i.e.: websockets)
                    var remoteSessionsManagers = (Dictionary<string, RemoteSessionManager>)HttpContext.Current.Application[HttpApplicationStateVariables.RemoteSessionsManagers.ToString()];
                    remoteSessionsManagers[HttpContext.Current.Session.SessionID] = RemoteSessionManager;

                    // update the remote sessions auto-increment counter
                    HttpContext.Current.Application[HttpApplicationStateVariables.RemoteSessionsCounter.ToString()] = remoteSessionsCounter;
                }
                catch (Exception exc)
                {
                    System.Diagnostics.Trace.TraceError("Failed to create remote session ({0})", exc);
                    RemoteSessionManager = null;
                }
                finally
                {
                    HttpContext.Current.Application.UnLock();
                }
            }

            // connect it
            if (RemoteSessionManager != null && RemoteSessionManager.RemoteSession.State != RemoteSessionState.Connecting && RemoteSessionManager.RemoteSession.State != RemoteSessionState.Connected)
            {
                try
                {
                    // update the remote session state
                    RemoteSessionManager.RemoteSession.State = RemoteSessionState.Connecting;

                    // create pipes for this web gateway and the rdp client to talk
                    RemoteSessionManager.Pipes.CreatePipes();

                    // the rdp client does connect the pipes when it starts; when it stops (either because it was closed, crashed or because the rdp session had ended), pipes are released
                    // use http://technet.microsoft.com/en-us/sysinternals/dd581625 to track the existing pipes
                    RemoteSessionManager.Client.StartProcess(
                        RemoteSessionManager.RemoteSession.Id,
                        RemoteSessionManager.RemoteSession.ServerAddress,
                        RemoteSessionManager.RemoteSession.UserDomain,
                        RemoteSessionManager.RemoteSession.UserName,
                        RemoteSessionManager.RemoteSession.UserPassword,
                        RemoteSessionManager.RemoteSession.ClientWidth,
                        RemoteSessionManager.RemoteSession.ClientHeight,
                        RemoteSessionManager.RemoteSession.DebugMode);

                    // update controls
                    UpateControls();
                }
                catch (Exception exc)
                {
                    System.Diagnostics.Trace.TraceError("Failed to connect remote session {0} ({1})", RemoteSessionManager.RemoteSession.Id, exc);
                }
            }
        }

        /// <summary>
        /// stop the rdp session
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void DisconnectButtonClick(
            object sender,
            EventArgs e)
        {
            // disconnect the active remote session, if any and connected
            if (RemoteSessionManager != null && (RemoteSessionManager.RemoteSession.State == RemoteSessionState.Connecting || RemoteSessionManager.RemoteSession.State == RemoteSessionState.Connected))
            {
                try
                {
                    // update the remote session state
                    RemoteSessionManager.RemoteSession.State = RemoteSessionState.Disconnecting;

                    // send a disconnect command to the rdp client
                    RemoteSessionManager.SendCommand(RemoteSessionCommand.CloseRdpClient);

                    // update controls
                    UpateControls();
                }
                catch (Exception exc)
                {
                    System.Diagnostics.Trace.TraceError("Failed to disconnect remote session {0} ({1})", RemoteSessionManager.RemoteSession.Id, exc);
                }
            }
        }
    }
}