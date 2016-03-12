I worked hard to make Myrtille as straightforward as possible, with commented code, but some points may needs additional information.
I apologize not providing an extensive documentation, diagrams, industry grade architecture models, mockups and unit tests; I wanted to keep things as simple as possible (but I don't exclude doing it in a future release).
If you have any issue, question or suggestion that isn't addressed into this synthetic documentation, FAQ or Wiki, please don't hesitate to contact me (mailto:cedrozor@gmail.com) or ask the community (https://groups.google.com/forum/#!forum/myrtille_rdp).

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

## Code organization
- Myrtille.RDP: C++ code. FreeRDP rdp client; modified to forward the user input(s) and encode the session display into the configured image format(s). The modified code in FreeRDP is identified by region tags "#pragma region Myrtille" and "#pragma endregion".
- Myrtille.Common: C# code. Fleck Websockets library and common helpers.
- Myrtille.Services: C# code. WCF services, hosted by a Windows Service (or a console application in debug build). start/stop the rdp client and upload/download file(s) to/from the connected user documents folder.
- Myrtille.Services.Contracts: C# code. WCF contracts (interfaces).
- Myrtille.Web: C# code. Link between the browser and the rdp client; maintain correlation between http and rdp sessions.
- Myrtille.Setup: MSI installer.

## Build
Myrtille uses C#, C++ and pure Javascript code (no additional libraries). Microsoft Visual Studio Community 2015 was used as primary development environment, using the .NET 4.0 framework.
If you want Visual Studio to load the Myrtille setup project, you have to install the (official and free) Microsoft Visual Studio 2015 Installer Projects extension (https://visualstudiogallery.msdn.microsoft.com/f1cc3f3e-c300-40a7-8797-c509fb8933b9)

The Myrtille build have the two classic solution configurations: "Debug" and "Release", on "Mixed Platforms" ("Win32" for C++ and "Any CPU" for C# projects).

### Startup projects
If you want to run Myrtille with Visual Studio, you should set startup projects on the solution in the following order (multiple startup projects):
- Myrtille.Services (Start)
- Myrtille.Web (Start)

You can choose the browser you want to use by right-clicking an ASPX page into the "Myrtille.Web" project and click "Browse With...".

Hit F5 to start debugging.
	
The FreeRDP executable project is "Myrtille.RDP/FreeRDP.wfreerdp" (others FreeRDP projects under "Myrtille.RDP" are static or dynamic libraries).  
You can debug FreeRDP, while an rdp session is active, by attaching the debugger to the process "FreeRDP.wfreerdp.exe" (native code).

## Communication
### Overview
web browser <-> web gateway <-> wcf services <-> rdp client <-> rdp server

### Protocols
web browser <-HTTP(S),XMLHTTP,WS(S)-> web gateway <-WCF-> wcf services <-SYSTEM-> rdp client <-RDP-> rdp server

"SYSTEM" is simply starting FreeRDP as a local process, and capture the event of it being stopped.

In order to speed up the communication between the web gateway and the rdp client, I decided to bypass the wcf services layer and set a direct communication between the two by using named pipes.
It has 2 advantages: first, it's a bit faster than using TCP and, second, it guarantees a FIFO unstacking to preserve the images order.	It has also a drawback: both the web gateway and the wcf services must be on the same machine.
After weighing pros and cons, I decided to kept it as is because it also has 2 additional benefits: setting up a named pipe in C++ is a bit simpler than a TCP socket (named pipes are handled as simple files) and having a single setup is easier for the end user.
		
web gateway <-IPC-> rdp client
	
However, I'm fully aware that it breaks the distributed architecture pattern. Thus, if you want, feel free to move the named pipes management up to the wcf services layer and proxy the data from/to the web gateway.
This is a thing to consider if you want to isolate the web gateway from your intranet (into a DMZ for instance) and still be able to connect a machine on it.

## Security
If you want to use Myrtille through HTTPS (https://yourserver/myrtille), you have to create a self-signed SSL certificate or import a valid one (server side).
Then, in order to use secure websockets (WSS), export this certificate into the Myrtille "ssl" folder, with private key, name "PKCS12Cert.pfx" and set a password that match the one defined into the Myrtille "Web.Config" file ("myrtille" by default).
If not using Google Chrome (client side), see detailed comments regarding the security configuration into the Myrtille "Web.Config" file. You may have to add an exception for port 8431 (secure websockets) into your browser.
In case of issues, ensure the port 8431 is not blocked by your firewall (or proxy, reverse proxy, VPN, etc.).

## Configuration
Both the gateway and services have their own .NET config files; the gateway also uses XDT transform files to adapt the settings depending on the current solution configuration.  
You may also play with the gateway "js/config.js" file settings to fine tune the configuration depending on your needs.

## Notes and limitations
- On Windows Server 2008, you may have to install (manually) the Microsoft Visual C++ 2008 redistributables, required by OpenSSL (libeay32.dll/ssleay32.dll)
- On Windows Server 2012, you may have issues installing the Microsoft Visual C++ 2015 redistributables (http://stackoverflow.com/questions/31536606/while-installing-vc-redist-x64-exe-getting-error-failed-to-configure-per-machi). To circumvent that, ensure your system is fully updated or try to install the package "Windows8.1-KB2999226-x64.msu" manually.
- Myrtille doesn't support clipboard and printer; they could however be enabled through FreeRDP virtual channels, given some additionnal code.
- Myrtille doesn't support NLA (standard RDP authentication only); this may be due to a misuse of FreeRDP or because it's an old version (0.8.2) which lacks full NLA support.
- In order to keep the installation simple, both the myrtille gateway and services are installed on the same machine. They do however conform to a distributed architecture; if needed, given some additionnal code, myrtille services could acts as a proxy, so the gateway could be installed and operate separately (this could be handy if the gateway should go into a DMZ).
- The installer creates a test user on the local machine named "myrtille", password "/Passw1rd/"; feel free to remove it if unwanted. The user is automatically removed on uninstall.
- The installer configures the RDP server on the local machine according to the Myrtille specifications (see above comment regarding NLA); any subsequent configuration changes may make Myrtille to dysfunction or stop working.

## Troubleshoot
First at all, ensure the Myrtille prerequisites are met (see README, "Installation" part).

- The installation fails
	- Check the Windows events logs ("System", "Application", etc.).
		
- I can't access http://yourserver/myrtille
	- Ensure IIS is started and "Myrtille.Web" application is running on the "MyrtilleAppPool" application pool.
	- Ensure .NET 4.0 is installed and the "MyrtilleAppPool" is running on it.
	- If using HTTPS, ensure a valid SSL certificate is installed on IIS and exported as .PFX into Myrtille "ssl" folder (more details in README, "Security" part).

- Nothing happens when I click "Connect!"
	- Ensure you entered valid connection information (server address, user credentials, etc.).
	- Ensure the network traffic (websockets and xmlhttp in particular) is not blocked by a firewall, proxy, reverse proxy, VPN or whatever.
	- Ensure IIS is started and "Myrtille.Web" application is running on the "MyrtilleAppPool" application pool.
	- Ensure .NET 4.0 is installed and the "MyrtilleAppPool" is running on it.
	- If using HTTPS with HTML5 rendering (hence secure websockets, WSS), ensure the TCP port 8431 is opened (more details in README, "Security" part).
	- Ensure the "Myrtille.Services" Windows service is started.
	- Ensure the Microsoft Visual C++ 2015 redistributables are installed (and also Microsoft Visual C++ 2008 redistributables if on Windows Server 2008); they are required by FreeRDP.
	- Check the RDP server configuration (does the user exists, is it a member of the "Remote Desktop Users" group, are Remote Desktop CALs valid?, etc.).
	- Check the RDP server logs (and also the Windows events logs on the RDP server machine).
	- Check the Windows events logs ("System", "Application", etc.), particulary regarding .NET.
	- Retry with Myrtille logs enabled and check them (Myrtille "log" folder). You can change their verbosity level in config (but be warned it will affect peformance and flood the disk if setted too verbose).

- The RDP session continues to run after clicking "Disconnect"
	- Check the RDP server configuration (session disconnect timeout in particular). You can setup it automatically by importing the Myrtille "RDPSetup.reg" file into registry.

- Myrtille is slow or buggy
	- Enable the stats bar to have detailed information about the current connection. Check latency and bandwith, among other things.
	- Ensure debug is disabled or otherwise logs are not set to "Information" level (Myrtille "Web.Config" file, "<system.diagnostics>" section, default is "Warning"). Check logs, if debug is enabled.
	- If debug is enabled and you are running Myrtille in debug mode under Visual Studio, you will have the FreeRDP window (session display) and console (rdp events) shown to you. It may help to debug.
	- Switch from HTML4 to HTML5 rendering, or inversely (should be faster with HTML5).
	- Check your network configuration (is something filtering the traffic?) and capabilities (high latency or small bandwith?).
	- Maybe the default settings are not adapted to your configuration. You can tweak the "js/config.js" file as you wish (see extensive comments there).
	- Despite my best efforts to produce quality and efficient code, I may have missed/messed something... Please don't hesitate to tell me or add your contribution! Thanks! :)