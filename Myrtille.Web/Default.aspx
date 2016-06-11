<%--
    Myrtille: A native HTML4/5 Remote Desktop Protocol client.

    Copyright (c) 2014-2016 Cedric Coste

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

	    http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.
--%>

<%@ Page Language="C#" Inherits="Myrtille.Web.Default" Codebehind="Default.aspx.cs" AutoEventWireup="true" Culture="auto" UICulture="auto" %>
<%@ OutputCache Location="None" %>
<%@ Import Namespace="System.Globalization" %>
<%@ Import Namespace="Myrtille.Web" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
	
    <head>
        <!-- force IE out of compatibility mode -->
        <meta http-equiv="X-UA-Compatible" content="IE=edge,chrome=1"/>
        <meta name="viewport" content="initial-scale=1.0"/>
        <title>Myrtille</title>
        <link rel="stylesheet" type="text/css" href="css/Default.css"/>
        <script language="javascript" type="text/javascript" src="js/myrtille.js"></script>
        <script language="javascript" type="text/javascript" src="js/config.js"></script>
        <script language="javascript" type="text/javascript" src="js/dialog.js"></script>
        <script language="javascript" type="text/javascript" src="js/display.js"></script>
        <script language="javascript" type="text/javascript" src="js/display/canvas.js"></script>
        <script language="javascript" type="text/javascript" src="js/display/divs.js"></script>
        <script language="javascript" type="text/javascript" src="js/network.js"></script>
        <script language="javascript" type="text/javascript" src="js/network/buffer.js"></script>
        <script language="javascript" type="text/javascript" src="js/network/longpolling.js"></script>
        <script language="javascript" type="text/javascript" src="js/network/websocket.js"></script>
        <script language="javascript" type="text/javascript" src="js/network/xmlhttp.js"></script>
        <script language="javascript" type="text/javascript" src="js/user.js"></script>
        <script language="javascript" type="text/javascript" src="js/user/keyboard.js"></script>
        <script language="javascript" type="text/javascript" src="js/user/mouse.js"></script>
        <script language="javascript" type="text/javascript" src="js/user/touchscreen.js"></script>
	</head>
	
    <body onload="startMyrtille(
       '<%=HttpContext.Current.Session.SessionID%>',
        <%=(RemoteSessionManager != null && (RemoteSessionManager.RemoteSession.State == RemoteSessionState.Connecting || RemoteSessionManager.RemoteSession.State == RemoteSessionState.Connected)).ToString(CultureInfo.InvariantCulture).ToLower()%>,
        <%=HttpContext.Current.Application[HttpApplicationStateVariables.WebSocketServerPort.ToString()]%>,
        <%=(HttpContext.Current.Application[HttpApplicationStateVariables.WebSocketServerPortSecured.ToString()] == null ? "null" : HttpContext.Current.Application[HttpApplicationStateVariables.WebSocketServerPortSecured.ToString()])%>,
        <%=(stat.Value == "Stat enabled").ToString(CultureInfo.InvariantCulture).ToLower()%>,
        <%=(debug.Value == "Debug enabled").ToString(CultureInfo.InvariantCulture).ToLower()%>,
        <%=(render.Value == "HTML4").ToString(CultureInfo.InvariantCulture).ToLower()%>);">

        <form method="get" runat="server" id="mainForm">

            <div>
                <%-- rdp connection settings --%>
                <input type="text" runat="server" id="server" value="localhost" title="server address"/>
                <input type="text" runat="server" id="domain" value="" title="user domain"/>
                <input type="text" runat="server" id="user" value="myrtille" title="user name"/>
                <input type="password" runat="server" id="password" title="user password. default myrtille password is /Passw1rd/"/>
                <select runat="server" id="stat" title="display stats bar"><option selected="selected">Stat disabled</option><option>Stat enabled</option></select>
                <select runat="server" id="debug" title="display (dev) or save session logs"><option selected="selected">Debug disabled</option><option>Debug enabled</option></select>
                <select runat="server" id="render" title="rendering mode"><option>HTML4</option><option selected="selected">HTML5</option></select>
                <input type="hidden" runat="server" id="width"/>
                <input type="hidden" runat="server" id="height"/>
                <input type="button" runat="server" id="connect" value="Connect!" onclick="setClientResolution();" onserverclick="ConnectButtonClick" title="login"/>
                <input type="button" runat="server" id="disconnect" value="Disconnect" disabled="disabled" onserverclick="DisconnectButtonClick" title="logout"/>

                <%-- virtual keyboard. for use on devices without a physical keyboard as it will force the virtual keyboard to pop --%>
                <input type="button" runat="server" id="keyboard" value="Keyboard" disabled="disabled" onclick="openPopup('virtualKeyboardPopup', 'VirtualKeyboard.aspx');" title="force virtual keyboard to pop up (useful when no physical keyboard is available)"/>
            
                <%-- upload/download file(s). only enabled if the connected server is localhost or if a domain is specified (so file(s) can be accessed within the rdp session) --%>
                <input type="button" runat="server" id="files" value="My Documents" disabled="disabled" onclick="openPopup('fileStoragePopup', 'FileStorage.aspx');" title="upload/download files to/from server (localhost only)"/>

                <%-- send ctrl+alt+del to the rdp session. may be useful to change the user password, for example --%>
                <input type="button" runat="server" id="cad" value="Ctrl+Alt+Del" disabled="disabled" onclick="sendCtrlAltDel();" title="send Ctrl+Alt+Del to the remote session"/>
            </div>

            <%-- remote session display --%>
            <div id="displayDiv"></div>

            <%-- remote session helpers --%>
            <div id="statDiv"></div>
		    <div id="debugDiv"></div>
            <div id="msgDiv"></div>
            <div id="kbhDiv"></div>
            <div id="bgfDiv"></div>

        </form>

		<script type="text/javascript" language="javascript" defer="defer">

            var display = new Display();

		    // browser size. default 1024x768
		    function setClientResolution()
		    {
		        document.getElementById('<%=width.ClientID%>').value = display.getBrowserWidth();
		        document.getElementById('<%=height.ClientID%>').value = display.getBrowserHeight();
		    }

		    var popup = null;

            function openPopup(id, src)
            {
                // lock background
                var bgfDiv = document.getElementById('bgfDiv');
                if (bgfDiv != null)
                {
                    bgfDiv.style.visibility = 'visible';
                    bgfDiv.style.display = 'block';
                }

                // add popup
                popup = document.createElement('iframe');
                popup.id = id;
                popup.src = src;
                popup.className = 'modalPopup';
                
                document.body.appendChild(popup);
            }

            function closePopup()
            {
                // unlock background
                var bgfDiv = document.getElementById('bgfDiv');
                if (bgfDiv != null)
                {
                    bgfDiv.style.visibility = 'hidden';
                    bgfDiv.style.display = 'none';
                }

                // remove popup
                if (popup != null)
                {
                    document.body.removeChild(popup);
                }
            }

            function sendCtrlAltDel()
            {
                // ctrl
                sendKey(17, false);
                window.setTimeout(function()
                {
                    // alt
                    sendKey(18, false);
                    window.setTimeout(function()
                    {
                        // del
                        sendKey(46, false);
                    }, 100)
                }, 100);
            }

		</script>

	</body>

</html>