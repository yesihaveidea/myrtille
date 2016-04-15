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
using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Windows.Forms;
using Myrtille.Helpers;

namespace Myrtille.Web
{
    [RunInstaller(true)]
    public class WebInstaller : Installer
    {
        public override void Install(
            IDictionary stateSaver)
        {
            // enable the line below to debug this installer; disable otherwise
            //MessageBox.Show("Attach the .NET debugger to the 'MSI Debug' msiexec.exe process now for debug. Click OK when ready...", "MSI Debug");

            // if the installer is running in repair mode, it will try to re-install Myrtille... which is fine
            // problem is, it won't uninstall it first... which is not fine because some components can't be installed twice!
            // thus, prior to any install, try to uninstall first

            Trace.TraceInformation("Myrtille.Web is being installed, cleaning first");

            try
            {
                Uninstall(null);
            }
            catch (Exception exc)
            {
                Trace.TraceInformation("Failed to clean Myrtille.Web ({0})", exc);
            }

            base.Install(stateSaver);

            Trace.TraceInformation("Installing Myrtille.Web");

            try
            {
                Process process = null;

                // register Myrtille.Web to local IIS

                if (!IISHelper.IsIISApplicationPoolExists("MyrtilleAppPool"))
                {
                    IISHelper.CreateIISApplicationPool("MyrtilleAppPool", "v4.0");
                }

                if (!IISHelper.IsIISApplicationExists("/Myrtille"))
                {
                    IISHelper.CreateIISApplication("/Myrtille", Path.GetFullPath(Context.Parameters["targetdir"]), "MyrtilleAppPool");
                }

                // add write permission to the targetdir "log" folder for MyrtilleAppPool, so that Myrtille.Web can save logs into it

                PermissionsHelper.AddDirectorySecurity(
                    Path.Combine(Path.GetFullPath(Context.Parameters["targetdir"]), "log"),
                    "IIS AppPool\\MyrtilleAppPool",
                    FileSystemRights.Write,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow);

                // create a default rdp user (myrtille) on the local server

                AccountHelper.CreateLocalUser(
                    "Myrtille",
                    "Myrtille User",
                    "/Passw1rd/",
                    true,
                    true);

                // add myrtille to the windows users group

                AccountHelper.AddLocalUserToGroup(
                    "Myrtille",
                    AccountHelper.GetUsersGroupName());

                // add myrtille to the remote desktop users group

                AccountHelper.AddLocalUserToGroup(
                    "Myrtille",
                    AccountHelper.GetRemoteDesktopUsersGroupName());

                // import the rdp registry keys required by myrtille on the local server

                process = new Process();

                #if !DEBUG
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                #endif

                process.StartInfo.FileName = "regedit.exe";
                process.StartInfo.Arguments = "/s \"" + Path.Combine(Path.GetFullPath(Context.Parameters["targetdir"]), "RDPSetup.reg") + "\"";
                process.Start();

                Trace.TraceInformation("Installed Myrtille.Web");
            }
            catch (Exception exc)
            {
                Context.LogMessage(exc.InnerException != null ? exc.InnerException.Message: exc.Message);
                Trace.TraceError("Failed to install Myrtille.Web ({0})", exc);
                throw;
            }
        }

        public override void Commit(
            IDictionary savedState)
        {
            base.Commit(savedState);
            // insert code as needed
        }

        public override void Rollback(
            IDictionary savedState)
        {
            base.Rollback(savedState);
            DoUninstall();
        }

        public override void Uninstall(
            IDictionary savedState)
        {
            base.Uninstall(savedState);
            DoUninstall();
        }

        private void DoUninstall()
        {
            // enable the line below to debug this installer; disable otherwise
            //MessageBox.Show("Attach the .NET debugger to the 'MSI Debug' msiexec.exe process now for debug. Click OK when ready...", "MSI Debug");

            Trace.TraceInformation("Uninstalling Myrtille.Web");

            try
            {
                // unregister Myrtille.Web from local IIS

                if (IISHelper.IsIISApplicationExists("/Myrtille"))
                {
                    IISHelper.DeleteIISApplication("/Myrtille");
                }

                if (IISHelper.IsIISApplicationPoolExists("MyrtilleAppPool"))
                {
                    IISHelper.DeleteIISApplicationPool("MyrtilleAppPool");
                }

                // delete myrtille user

                AccountHelper.DeleteLocalUser("Myrtille");

                Trace.TraceInformation("Uninstalled Myrtille.Web");
            }
            catch (Exception exc)
            {
                Context.LogMessage(exc.InnerException != null ? exc.InnerException.Message : exc.Message);
                Trace.TraceError("Failed to uninstall Myrtille.Web ({0})", exc);
                throw;
            }
        }
    }
}