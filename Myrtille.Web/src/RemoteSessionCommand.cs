/*
    Myrtille: A native HTML4/5 Remote Desktop Protocol client.

    Copyright(c) 2014-2017 Cedric Coste

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

using System.Collections;

namespace Myrtille.Web
{
    public enum RemoteSessionCommand
    {
        // browser
        SendBrowserResize = 0,

        // keyboard
        SendKeyUnicode = 1,
        SendKeyScancode = 2,

        // mouse
        SendMouseMove = 3,
        SendMouseLeftButton = 4,
        SendMouseMiddleButton = 5,
        SendMouseRightButton = 6,
        SendMouseWheelUp = 7,
        SendMouseWheelDown = 8,

        // control
        SetStatMode = 9,
        SetDebugMode = 10,
        SetCompatibilityMode = 11,
        SetScaleDisplay = 12,
        SetImageEncoding = 13,
        SetImageQuality = 14,
        SetImageQuantity = 15,
        RequestFullscreenUpdate = 16,
        RequestRemoteClipboard = 17,
        CloseRdpClient = 18
    }

    /*
    prefixes (3 chars) are used to serialize commands with strings instead of numbers
    they make it easier to read log traces to find out which commands are issued
    they must match the prefixes used client side
    */
    public static class RemoteSessionCommandMapping
    {
        public static Hashtable FromPrefix { get; private set; }
        public static Hashtable ToPrefix { get; private set; }

        static RemoteSessionCommandMapping()
        {
            FromPrefix = new Hashtable();
            FromPrefix["RSZ"] = RemoteSessionCommand.SendBrowserResize;
            FromPrefix["KUC"] = RemoteSessionCommand.SendKeyUnicode;
            FromPrefix["KSC"] = RemoteSessionCommand.SendKeyScancode;
            FromPrefix["MMO"] = RemoteSessionCommand.SendMouseMove;
            FromPrefix["MLB"] = RemoteSessionCommand.SendMouseLeftButton;
            FromPrefix["MMB"] = RemoteSessionCommand.SendMouseMiddleButton;
            FromPrefix["MRB"] = RemoteSessionCommand.SendMouseRightButton;
            FromPrefix["MWU"] = RemoteSessionCommand.SendMouseWheelUp;
            FromPrefix["MWD"] = RemoteSessionCommand.SendMouseWheelDown;
            FromPrefix["STA"] = RemoteSessionCommand.SetStatMode;
            FromPrefix["DBG"] = RemoteSessionCommand.SetDebugMode;
            FromPrefix["CMP"] = RemoteSessionCommand.SetCompatibilityMode;
            FromPrefix["SCA"] = RemoteSessionCommand.SetScaleDisplay;
            FromPrefix["ECD"] = RemoteSessionCommand.SetImageEncoding;
            FromPrefix["QLT"] = RemoteSessionCommand.SetImageQuality;
            FromPrefix["QNT"] = RemoteSessionCommand.SetImageQuantity;
            FromPrefix["FSU"] = RemoteSessionCommand.RequestFullscreenUpdate;
            FromPrefix["CLP"] = RemoteSessionCommand.RequestRemoteClipboard;
            FromPrefix["CLO"] = RemoteSessionCommand.CloseRdpClient;

            ToPrefix = new Hashtable();
            ToPrefix[RemoteSessionCommand.SendBrowserResize] = "RSZ";
            ToPrefix[RemoteSessionCommand.SendKeyUnicode] = "KUC";
            ToPrefix[RemoteSessionCommand.SendKeyScancode] = "KSC";
            ToPrefix[RemoteSessionCommand.SendMouseMove] = "MMO";
            ToPrefix[RemoteSessionCommand.SendMouseLeftButton] = "MLB";
            ToPrefix[RemoteSessionCommand.SendMouseMiddleButton] = "MMB";
            ToPrefix[RemoteSessionCommand.SendMouseRightButton] = "MRB";
            ToPrefix[RemoteSessionCommand.SendMouseWheelUp] = "MWU";
            ToPrefix[RemoteSessionCommand.SendMouseWheelDown] = "MWD";
            ToPrefix[RemoteSessionCommand.SetStatMode] = "STA";
            ToPrefix[RemoteSessionCommand.SetDebugMode] = "DBG";
            ToPrefix[RemoteSessionCommand.SetCompatibilityMode] = "CMP";
            ToPrefix[RemoteSessionCommand.SetScaleDisplay] = "SCA";
            ToPrefix[RemoteSessionCommand.SetImageEncoding] = "ECD";
            ToPrefix[RemoteSessionCommand.SetImageQuality] = "QLT";
            ToPrefix[RemoteSessionCommand.SetImageQuantity] = "QNT";
            ToPrefix[RemoteSessionCommand.RequestFullscreenUpdate] = "FSU";
            ToPrefix[RemoteSessionCommand.RequestRemoteClipboard] = "CLP";
            ToPrefix[RemoteSessionCommand.CloseRdpClient] = "CLO";
        }
    }
}