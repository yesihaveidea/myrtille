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

/*****************************************************************************************************************************************************************************************************/
/*** Websocket                                                                                                                                                                                     ***/
/*****************************************************************************************************************************************************************************************************/

function Websocket(base, config, dialog, display, network)
{
    // up (inputs, display and notifications if duplex)
    var ws = null;
    this.getWs = function() { return ws; };

    var wsOpened = false;
    this.getWsOpened = function() { return wsOpened; };

    var wsError = false;

    // down (display and notifications)
    var ws2 = null;
    this.getWs2 = function() { return ws2; };

    var ws2Opened = false;
    this.getWs2Opened = function() { return ws2Opened; };

    var ws2Error = false;

    this.init = function()
    {
        try
        {
            // using the IIS 8+ websockets support, the websocket server url is the same as http (there is just a protocol scheme change and a specific handler; standard and secured ports are the same)
            var wsUrl = config.getHttpServerUrl().replace('http', 'ws') + 'handlers/SocketHandler.ashx?binary=' + (config.getNetworkMode() == config.getNetworkModeEnum().WEBSOCKET && config.getImageMode() == config.getImageModeEnum().BINARY ? 'true' : 'false') + '&direction=' + (config.getWebsocketDuplex() ? 'duplex' : 'up');

            //dialog.showDebug('websocket server url: ' + wsUrl);

            var wsImpl = window.WebSocket || window.MozWebSocket;
            ws = new wsImpl(wsUrl);

            // if websocket is enabled and isn't duplex, create a 2nd websocket to receive the updates
            if (config.getNetworkMode() == config.getNetworkModeEnum().WEBSOCKET && !config.getWebsocketDuplex())
            {
                var ws2Url = config.getHttpServerUrl().replace('http', 'ws') + 'handlers/SocketHandler.ashx?binary=' + (config.getImageMode() == config.getImageModeEnum().BINARY ? 'true' : 'false') + '&direction=down';
                ws2 = new wsImpl(ws2Url);
            }
        }
        catch (exc)
        {
            dialog.showDebug('websocket init error: ' + exc.message + ', falling back to long-polling');
            config.setNetworkMode(config.getNetworkModeEnum().LONGPOLLING);
            ws = null;
            ws2 = null;
            return;
        }

        // websocket binary transfer, instead of text, removes the base64 33% bandwidth overhead (preferred)
        if (config.getImageMode() == config.getImageModeEnum().BINARY)
        {
            try
            {
                // the another possible value is 'blob'; but it involves asynchronous processing whereas we need images to be displayed sequentially
                ws.binaryType = 'arraybuffer';

                if (config.getNetworkMode() == config.getNetworkModeEnum().WEBSOCKET && !config.getWebsocketDuplex())
                {
                    ws2.binaryType = 'arraybuffer';
                }
            }
            catch (exc)
            {
                dialog.showDebug('websocket binary init error: ' + exc.message + ', falling back to base64 (or roundtrip if not available)');
                config.setImageMode(!display.isBase64Available() ? config.getImageModeEnum().ROUNDTRIP : config.getImageModeEnum().BASE64);
                dialog.showStat(dialog.getShowStatEnum().IMAGE_MODE, config.getImageMode());
            }
        }

        //dialog.showDebug('using ' + (config.getImageMode() != config.getImageModeEnum().BINARY ? 'text' : 'binary') + ' websocket');

        try
        {
            ws.onopen = function() { open(config.getWebsocketDuplex() ? 'duplex' : 'up'); };
            ws.onmessage = function(e) { message(e); };
            ws.onerror = function() { error(config.getWebsocketDuplex() ? 'duplex' : 'up'); };
            ws.onclose = function(e) { close(e, config.getWebsocketDuplex() ? 'duplex' : 'up'); };
            
            if (config.getNetworkMode() == config.getNetworkModeEnum().WEBSOCKET && !config.getWebsocketDuplex())
            {
                ws2.onopen = function() { open('down'); };
                ws2.onmessage = function(e) { message(e); };
                ws2.onerror = function() { error('down'); };
                ws2.onclose = function(e) { close(e, 'down'); };
            }
        }
        catch (exc)
        {
            dialog.showDebug('websocket events init error: ' + exc.message + ', falling back to long-polling');
            config.setNetworkMode(config.getNetworkModeEnum().LONGPOLLING);
            ws = null;
            ws2 = null;
        }
    };

    function open(direction)
    {
        dialog.showDebug('websocket ' + direction + ' opened');

        var init;

        if (direction != 'down')
        {
            wsOpened = true;
            init = config.getNetworkMode() != config.getNetworkModeEnum().WEBSOCKET || config.getWebsocketDuplex() || ws2Opened;
        }
        else
        {
            ws2Opened = true;
            init = wsOpened;
        }

        if (init)
        {
            // as websockets don't involve any standard http communication, the http session will timeout after a given time (default 20mn)
            // below is a periodical dummy call, using xhr, to keep it alive
            window.setInterval(function()
            {
                //dialog.showDebug('http session keep alive');
                network.getXmlhttp().send(null, new Date().getTime());
            },
            config.getHttpSessionKeepAliveInterval());

            // if connecting, the gateway will automatically connect the remote server when the socket is opened server side
            // if connected, send the client settings and request a fullscreen update
            if (config.getConnectionState() == 'CONNECTED')
            {
                base.initClient();
            }
        }
    }

    function message(e)
    {
        // if the websocket isn't duplex, it's not meant to receive data
        // however, it's just a logical limitation; the websocket is still technically duplex, so let's handle any received data
        // it happens with event source for example, because server acks are retuned on the websocket

        if (config.getAdditionalLatency() > 0)
        {
            window.setTimeout(function() { receive(e.data); }, Math.round(config.getAdditionalLatency() / 2));
        }
        else
        {
            receive(e.data);
        }
    }

    function error(direction)
    {
        dialog.showDebug('websocket ' + direction + ' error');

        if (direction != 'down')
        {
            wsError = true;
        }
        else
        {
            ws2Error = true;
        }
    }

    function close(e, direction)
    {
        var error;

        // the websocket may not be duplex; handle error on both directions
        if (direction != 'down')
        {
            error = !wsOpened || wsError;
        }
        else
        {
            error = !ws2Opened || ws2Error;
        }

        if (!error)
        {
            dialog.showDebug('websocket ' + direction + ' closed');
        }
        else
        {
            // the websocket failed, fallback to long-polling
            dialog.showDebug('websocket ' + direction + ' closed with error code ' + e.code + ' (if you have IIS 8 or greater, ensure the websocket protocol is enabled), falling back to long-polling');
            config.setNetworkMode(config.getNetworkModeEnum().LONGPOLLING);
            network.init();
        }

        if (direction != 'down')
        {
            wsOpened = false;
        }
        else
        {
            ws2Opened = false;
        }
    }

    function wsSend(text)
    {
        if (config.getImageMode() != config.getImageModeEnum().BINARY)
        {
            ws.send(text);
        }
        else
        {
            ws.send(strToBytes(text));
        }
    }

    this.send = function(data, startTime)
    {
        try
        {
            // in case of ws issue, the event data is not sent
            // if that occurs and a buffer is used (not mandatory but will help to lower the network stress), the buffer must not be cleared... the event data can still be sent on its next flush tick!
            // this can't be done if no buffer is used (the unsent event data is lost...)
            var buffer = network.getBuffer();
            if (buffer != null)
            {
                buffer.setClearBuffer(false);
            }

            if (ws == null)
            {
                //dialog.showDebug('ws is null');
                return;
            }

            if (!wsOpened)
            {
                //dialog.showDebug('ws is not ready');
                return;
            }

            wsSend(
                (data == null ? '' : encodeURIComponent(data)) +
                '&' + display.getImgIdx() +
                '&' + network.getRoundtripDurationAvg() +
                '&' + startTime);

            if (buffer != null)
            {
                buffer.setClearBuffer(true);
            }
        }
        catch (exc)
        {
            dialog.showDebug('websocket send error: ' + exc.message);
        }
    };

    function receive(data)
    {
        try
        {
            //dialog.showDebug('received websocket data: ' + data + ', length: ' + data.length + ', byteLength: ' + data.byteLength);

            if (data != null && data != '')
            {
                var text = '';
                var imgText = '';
                var dataView = null;

                if (config.getImageMode() != config.getImageModeEnum().BINARY)
                {
                    text = data;

                    if (config.getHostType() == config.getHostTypeEnum().RDP)
                    {
                        if (text.indexOf(';') == -1)
                        {
                            //dialog.showDebug('message data: ' + text);
                        }
                        else
                        {
                            var image = text.split(';');
                            imgText = image[0];
                            text = '';
                            //dialog.showDebug('image data: ' + imgText);
                        }
                    }
                    else
                    {
                        //dialog.showDebug('terminal data: ' + text);
                    }
                }
                else
                {
                    dataView = new DataView(data);

                    var imgTag = dataView.getUint32(0, true);
                    //dialog.showDebug('image tag: ' + imgTag);

                    if (imgTag != 0)
                    {
                        var bytes = new Uint8Array(data, 0, data.byteLength);

                        if ('TextDecoder' in window)
                        {
                            var utf8Decoder = new TextDecoder();
                            text = utf8Decoder.decode(bytes);
                        }
                        else
                        {
                            text = decodeUtf8(bytes);
                        }

                        if (config.getHostType() == config.getHostTypeEnum().RDP)
                        {
                            //dialog.showDebug('message data: ' + text);
                        }
                        else
                        {
                            //dialog.showDebug('terminal data: ' + text);
                        }
                    }
                    else
                    {
                        //dialog.showDebug('binary image');
                    }
                }

                // message
                if (text != '')
                {
                    processMessage(text);
                }
                // image
                else
                {
                    var imgInfo, idx, posX, posY, width, height, format, quality, fullscreen, imgData;

                    if (config.getImageMode() != config.getImageModeEnum().BINARY)
                    {
                        imgInfo = imgText.split(',');

                        idx = parseInt(imgInfo[0]);
                        posX = parseInt(imgInfo[1]);
                        posY = parseInt(imgInfo[2]);
                        width = parseInt(imgInfo[3]);
                        height = parseInt(imgInfo[4]);
                        format = imgInfo[5];
                        quality = parseInt(imgInfo[6]);
                        fullscreen = imgInfo[7] == 'true';
                        imgData = imgInfo[8];
                    }
                    else
                    {
                        idx = dataView.getUint32(4, true);
                        posX = dataView.getUint32(8, true);
                        posY = dataView.getUint32(12, true);
                        width = dataView.getUint32(16, true);
                        height = dataView.getUint32(20, true);
                        format = display.getFormatText(dataView.getUint32(24, true));
                        quality = dataView.getUint32(28, true);
                        fullscreen = dataView.getUint32(32, true) == 1;
                        imgData = new Uint8Array(data, 36, data.byteLength - 36);
                    }

                    processImage(idx, posX, posY, width, height, format, quality, fullscreen, imgData);
                }
            }
        }
        catch (exc)
        {
            dialog.showDebug('websocket receive error: ' + exc.message);
        }
    }
}