# Myrtille
Myrtille provides a simple and fast access to remote desktops and applications through a web browser, without any plugin, extension or configuration.

Technically, Myrtille is an HTTP(S) to RDP gateway.

## How does it works?
It works by forwarding the user inputs (keyboard, mouse, touchscreen) from a web browser to an HTTP(S) gateway, then up to an RDP client which maintain a session with an RDP server.

The display resulting, or not, of such actions is streamed back to the browser, from the rdp client and through the gateway.

The implementation is quite straightforward in order to maintain the best speed and stability as possible. Some optimizations, such as inputs buffering and display quality tweaking, help to mitigate with latency and bandwidth issues.

More information into the DOCUMENTATION.md file.

## Features
- HTML4 and HTML5 support
- HTTP(S) to RDP gateway
- File transfer
- WebP compression

## Requirements
- HTTP(S) client: any HTML4 or HTML5 browser (starting from IE6!)
- HTTP(S) to RDP gateway: IIS 7.0+ and .NET 4.0+ (see DOCUMENTATION.md for gateway prerequisites)
- RDP server: any RDP enabled computer (preferably Windows Server but can also be Windows XP, 7, 8, 10. see DOCUMENTATION.md for configuration)

## Build
See DOCUMENTATION.md.

## Installation
All releases here: https://github.com/cedrozor/myrtille/releases

See DOCUMENTATION.md for more details.

## Usage
Once Myrtille is installed on your server, you can use it at http://yourserver/myrtille. Set the rdp server address, user domain (if defined), name and password then click "Connect!" to log in. "Disconnect" to log out.

The installer creates a self-signed certificate for https://yourserver/myrtille. Like for all self-signed certificates, you will have to add a security exception into your browser (just ignore the warning message and proceed to the website).
Of course, you can avoid that by installing a certificate provided by a trusted Certification Authority (see DOCUMENTATION.md).

If you want connection information, you can enable stat (displayed on screen or browser console). If you want debug information, you can enable debug (logs are saved under the Myrtille "log" folder).

You can also choose the rendering mode, HTML4 or HTML5 (HTML4 may be useful, for example, if websockets are blocked by a proxy or firewall).

On touchscreen devices, you can pop the device keyboard with the "Keyboard" button. Then enter some text and click "Send".

You can also upload/download file(s) to/from the user documents folder with the "My documents" button. Note that it requires the rdp server to be localhost (same machine as the http server) or a domain to be specified.

Myrtille doesn't support the mouse pointer shadow. If enabled for the user, you have to disable it (not much difference anyway and ensure best performance). See "Notes and limitations" into DOCUMENTATION.md.

## Third-party
Myrtille uses the following licensed software:
- RDP client: FreeRDP 0.8.2 (https://github.com/FreeRDP/FreeRDP-old), licensed under Apache 2.0 license.
- OpenSSL toolkit: 1.0.1f (https://github.com/openssl/openssl), licensed under BSD-style Open Source licenses.
- WebP encoding: libWebP 0.5.0 (https://github.com/webmproject/libwebp), licensed under BSD-style Open Source license. Copyright (c) 2010, Google Inc. All rights reserved.
- HTML5 websockets: Fleck 0.14.0 (https://github.com/statianzo/Fleck), licensed under MIT license. Copyright (c) 2010-2014 Jason Staten.
- Logging: Log4net 1.2.13.0 (https://logging.apache.org/log4net/), licensed under Apache 2.0 license.

See DISCLAIMERS.md file.

The Myrtille code in FreeRDP is surrounded by region tags "#pragma region Myrtille" and "#pragma endregion".

libWebP are official Google's WebP precompiled binaries, and are left unmodified. Same for Fleck websockets.

## License
Myrtille is licensed under Apache 2.0 license.
See LICENSE file.

## Author
Cedric Coste (cedrozor@gmail.com).
- LinkedIn:	https://fr.linkedin.com/in/cedric-coste-a1b9194b
- Twitter:	https://twitter.com/cedrozor
- Google+:	https://plus.google.com/111659262235573941837
- Facebook:	https://www.facebook.com/profile.php?id=100011710352840

## Contributors
- Catalin Trifanescu (AppliKr developer: application server. Steemind cofounder)
- Fabien Janvier (AppliKr developer: website css, clipping algorithm, websocket server)
- UltraSam (AppliKr developer: rdp client, http gateway)

## Resources
- Website:	http://cedrozor.github.io/myrtille
- Sources:	https://github.com/cedrozor/myrtille
- Tracker:	https://github.com/cedrozor/myrtille/issues
- Wiki:		https://github.com/cedrozor/myrtille/wiki
- Support:	https://groups.google.com/forum/#!forum/myrtille_rdp
- Demo:		https://www.youtube.com/watch?v=l4CL8h0KfQ8