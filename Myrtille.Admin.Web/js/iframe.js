/*
    Myrtille: A native HTML4/5 Remote Desktop Protocol client.

    Copyright(c) 2014-2019 Cedric Coste

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

function switchIframe(iframeId)
{
	try
    {
        var iframe = document.getElementById(iframeId);
		//alert('leaving iframe: ' + iframe.id);
				
		var iframeInfo = iframe.id.split('_');
		var nextIframeId = iframeInfo[0] + '_' + (parseInt(iframeInfo[1]) + 1);
		//alert('next iframe: ' + nextIframeId);
				
		var nextIframe = document.getElementById(nextIframeId);
		if (nextIframe == null)
		{
			//alert('last iframe is reached, cycling to first');
			nextIframe = document.getElementById(iframeInfo[0] + '_1');
		}
		if (nextIframe != null)
		{
			//alert('active iframe: ' + nextIframe.id);
            nextIframe.contentWindow.setKeyCombination();
            nextIframe.contentWindow.focus();
		}
	}
	catch (exc)
	{
		alert('switchIframe error: ' + exc.message);
	}
}

function checkIframeFocus()
{
    try
    {
        var iframes = document.getElementsByTagName('iframe');
        for (var i = 0; i < iframes.length; i++)
        {
            var iframe = document.getElementById(iframes[i].id);
            if (iframe.id != document.activeElement.id)
            {
                iframe.className = 'iframeNoBorder';
            }
            else
            {
                iframe.className = 'iframeColorBorder';
            }
        }
    }
	catch (exc)
	{
        alert('checkIframeFocus error: ' + exc.message);
	}
}
		
// http://www.quirksmode.org/js/cookies.html
function setCookie(name,value,days) {
	var expires = "";
	if (days) {
		var date = new Date();
		date.setTime(date.getTime() + (days*24*60*60*1000));
		expires = "; expires=" + date.toUTCString();
	}
	document.cookie = name + "=" + (value || "")  + expires + "; path=/";
}
function getCookie(name) {
	var nameEQ = name + "=";
	var ca = document.cookie.split(';');
	for(var i=0;i < ca.length;i++) {
		var c = ca[i];
		while (c.charAt(0)==' ') c = c.substring(1,c.length);
		if (c.indexOf(nameEQ) == 0) return c.substring(nameEQ.length,c.length);
	}
	return null;
}
function eraseCookie(name) {   
    document.cookie = name + '=; Max-Age=-99999999; path=/';
}