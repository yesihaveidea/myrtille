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

## Security

If you want to use Myrtille through HTTPS (https://yourserver/myrtille), you have to create a self-signed SSL certificate or import a valid one (server side).
Then, in order to use secure websockets (WSS), export this certificate into the Myrtille "ssl" folder, with private key, name "PKCS12Cert.pfx" and set a password that match the one defined into the Myrtille "Web.Config" file ("myrtille" by default).
If not using Google Chrome (client side), see detailed comments regarding the security configuration into the Myrtille "Web.Config" file. You may have to add an exception for port 8431 (secure websockets) into your browser.
In case of issues, ensure the port 8431 is not blocked by your firewall (or proxy, reverse proxy, VPN, etc.).

## Notes and limitations

- On Windows Server 2008, you may have to install (manually) the Microsoft Visual C++ 2008 redistributables, required by OpenSSL (libeay32.dll/ssleay32.dll)
- On Windows Server 2012, you may have issues installing the Microsoft Visual C++ 2015 redistributables (http://stackoverflow.com/questions/31536606/while-installing-vc-redist-x64-exe-getting-error-failed-to-configure-per-machi). To circumvent that, ensure your system is fully updated or try to install the package "Windows8.1-KB2999226-x64.msu" manually.
- Myrtille doesn't support clipboard and printer; they could however be enabled through FreeRDP virtual channels, given some additionnal code.
- Myrtille doesn't support NLA (standard RDP authentication only); this may be due to a misuse of FreeRDP or because it's an old version (0.8.2) which lacks full NLA support.
- In order to keep the installation simple, both the myrtille gateway and services are installed on the same machine. They do however conform to a distributed architecture; if needed, given some additionnal code, myrtille services could acts as a proxy, so the gateway could be installed and operate separately (this could be handy if the gateway should go into a DMZ).
- The installer creates a test user on the local machine named "myrtille", password "/Passw1rd/"; feel free to remove it if unwanted. The user is automatically removed on uninstall.
- The installer configures the RDP server on the local machine according to the Myrtille specifications (see above comment regarding NLA); any subsequent configuration changes may make Myrtille to dysfunction or stop working.

## Build

Myrtille uses C#, C++ and pure Javascript code (no additional libraries). Microsoft Visual Studio Community 2015 was used as primary development environment, using the .NET 4.0 framework.
If you want Visual Studio to load the Myrtille setup project, you have to install the (official and free) Microsoft Visual Studio 2015 Installer Projects extension (https://visualstudiogallery.msdn.microsoft.com/f1cc3f3e-c300-40a7-8797-c509fb8933b9)

The Myrtille build have the two classic solution configurations: "Debug" and "Release", on "Mixed Platforms" ("Win32" for C++ and "Any CPU" for C# projects).

### Startup projects
If you want to run Myrtille with Visual Studio, you should set startup projects on the solution in the following order (multiple startup projects):
- Myrtille.Services (Start)
- Myrtille.Web (Start)

See DOCUMENTATION file for code organization.

You can choose the browser you want to use by right-clicking an ASPX page into the "Myrtille.Web" project and click "Browse With...".

Hit F5 to start debugging.
	
The FreeRDP executable project is "Myrtille.RDP/FreeRDP.wfreerdp" (others FreeRDP projects under "Myrtille.RDP" are static or dynamic libraries).
You can debug FreeRDP, while an rdp session is active, by attaching the debugger to the process "FreeRDP.wfreerdp.exe" (native code).

## Third-party

Myrtille uses the following licensed software:
- RDP client: FreeRDP 0.8.2 (https://github.com/FreeRDP/FreeRDP), licensed under Apache 2.0 license.
- OpenSSL toolkit: 1.0.1f (https://github.com/openssl/openssl), licensed under BSD-style Open Source licenses.
- WebP encoding: libWebP 0.5.0 (https://github.com/webmproject/libwebp), licensed under BSD-style Open Source license. Copyright (c) 2010, Google Inc. All rights reserved.
- HTML5 websockets: Fleck 0.14.0 (https://github.com/statianzo/Fleck), licensed under MIT license. Copyright (c) 2010-2014 Jason Staten.
- Logging: Log4net 1.2.13.0 (https://logging.apache.org/log4net/), licensed under Apache 2.0 license.

See DISCLAIMERS file.

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

## History

Myrtille started back in 2007 as a PoC, with coworkers on our spare time, under the name AppliKr. I (Cedric Coste) was in charge of the websites (admin and frontal), business and database layers.
The objective was to demonstrate it was possible to virtualize desktops and applications into a web browser only using native web technologies (no plugin).
HTML5 was in early stage and it was a real challenge to do it only with HTML4. We were pioneers on that matter, along with Guacamole (HTML5/VNC), and wanted to take part on the emerging SaaS market.
It was quite a success because, at that time, the zero plugin concept was innovative and unlike solutions using Java, activeX or Flex; the ease of use and security aspects were also improved. But it was also slow and buggy and was left aside.
In 2011, I had the opportunity to create a company, named Steemind, with Catalin Trifanescu (a former coworker). I rewrote everything and used FreeRDP as rdp client (AppliKr was using RDesktop) and speed and stability were way better.
The company failed in 2012 because we weren't able to achieve a decent fund raising and our customers were moving to Citrix and other major corporate companies in the VDI business.
I had recently some spare time and, while I was taking my breakfast with some blueberry jam, I decided to extract and improve the Steemind core technology and open source it.
	
I hope you will enjoy Myrtille! :)

Special thanks to Catalin Trifanescu for its support.

## Resources

- Website:	http://cedrozor.github.io/myrtille
- Sources:	https://github.com/cedrozor/myrtille
- Tracker:	https://github.com/cedrozor/myrtille/issues
- Wiki:		https://github.com/cedrozor/myrtille/wiki
- Support:	https://groups.google.com/forum/#!forum/myrtille_rdp