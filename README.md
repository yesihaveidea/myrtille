# Myrtille

Myrtille provides a simple and fast access to remote desktops and applications through a web browser.

## How does it works?

It works by forwarding the user inputs (keyboard, mouse, touchscreen) from a web browser to an HTTP(S) gateway, then up to an RDP client which maintain a session with an RDP server.
The display resulting, or not, of such actions is streamed back to the browser, from the rdp client and through the gateway.
The implementation is quite straightforward in order to maintain the best speed and stability as possible.
Some optimizations, such as inputs buffering and display quality tweaking, help to mitigate with latency and bandwidth issues.
More information into the DOCUMENTATION file.

## Requirements

- Client: a HTML4 or HTML5 browser
- Server: a RDP enabled computer, IIS 7.0+ and .NET 4.0+ (see related roles and features below)

## Installation

### Prerequisites
Ensure the following Windows Server Roles and Features are installed on the machine on which you want to install Myrtille:
- Remote Destop Services role (formerly Terminal Services). Myrtille only requires the Remote Desktop Session Host. You can either setup it manually (see notes and limitations below) or double click the Myrtille "RDPSetup.reg" file for automatic configuration (import registry keys).
- Web Server role (IIS). Myrtille also requires .NET 4.0, which can be installed separately (using the Myrtille setup bootstrapper or a standalone installation package) or as a IIS feature.
- Applications Server role. Myrtille requires the Windows Processes activation service support, through HTTP, TCP and named pipes.
- Files Storage Service role. Should be installed automatically if the above roles are installed. Myrtille requires the files server feature in order to allow to upload/download file(s) to the connected users documents folders.

### Network
Add the following rules to the machine firewall:
- "Myrtille Websockets": allow both directions TCP port 8181
- "Myrtille Websockets Secured": allow both directions TCP port 8431

### Installation
- Setup.exe (preferred installation method): setup bootstrapper; automatically download and install .NET 4.0 and Microsoft Visual C++ 2015 redistributables (if not already installed), then install the Myrtille MSI package
- Myrtille.msi: Myrtille MSI package (x86)

If you have several RDP servers, you don't have to install Myrtille on each of them; you only have to configure them to be accessed by a Myrtille installation.
You can either do it manually (see notes and limitations below) or copy and import the Myrtille "RDPSetup.reg" file over the servers.

## Usage

Once Myrtille is installed on your server, you can use it at http://yourserver/myrtille. Set the rdp server address, user domain (if defined), name and password then click "Connect!" to log in. "Disconnect" to log out.
If you want connection information, you can enable stat (displayed on screen or browser console). If you want debug information, you can enable debug (logs are saved under the Myrtille "log" folder).
You can also choose the rendering mode, HTML4 or HTML5 (HTML4 may be useful, for example, if websockets are blocked by a proxy or firewall).
On touchscreen devices, you can pop the device keyboard with the "Keyboard" button. Then enter some text and click "Send".
You can also upload/download file(s) to/from the user documents folder with the "My documents" button. Note that it requires the rdp server to be localhost (same machine as the http server).

## Configuration

Both the gateway and services have their own .NET config files; the gateway also uses XDT transform files to adapt the settings depending on the current solution configuration.
You may also play with the gateway "js/config.js" file settings to fine tune the configuration depending on your needs.

## Third-party

Myrtille uses the following licensed software:
- RDP client: FreeRDP 0.8.2 (https://github.com/FreeRDP/FreeRDP), licensed under Apache 2.0 license.
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

Cedric Coste (mailto:cedrozor@gmail.com)
https://fr.linkedin.com/in/cedric-coste-a1b9194b

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