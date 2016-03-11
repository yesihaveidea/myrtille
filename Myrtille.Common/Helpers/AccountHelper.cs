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
using System.ComponentModel;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace Myrtille.Helpers
{
    public static class AccountHelper
    {
        #region Windows Groups

        /// <summary>
        /// retrieve the local machine administrators group name
        /// </summary>
        /// <returns>group name</returns>
        public static string GetAdministratorsGroupName()
        {
            var groupName = "Administrators";

            try
            {
                var sid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
                var account = sid.Translate(typeof(NTAccount)) as NTAccount;
                groupName = account.ToString().Substring(account.ToString().LastIndexOf("\\") + 1, account.ToString().Length - account.ToString().LastIndexOf("\\") - 1);
            }
            catch (Exception exc)
            {
                Trace.TraceWarning("Failed to retrieve administrators group ({0})", exc);
            }

            return groupName;
        }

        /// <summary>
        /// retrieve the local machine everyone group name
        /// </summary>
        /// <returns>group name</returns>
        public static string GetEveryoneGroupName()
        {
            var groupName = "Everyone";

            try
            {
                var sid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
                var account = sid.Translate(typeof(NTAccount)) as NTAccount;
                groupName = account.ToString();
            }
            catch (Exception exc)
            {
                Trace.TraceWarning("Failed to retrieve everyone group ({0})", exc);
            }

            return groupName;
        }

        /// <summary>
        /// retrieve the local machine users group name
        /// </summary>
        /// <returns>group name</returns>
        public static string GetUsersGroupName()
        {
            var groupName = "Users";

            try
            {
                var sid = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
                var account = sid.Translate(typeof(NTAccount)) as NTAccount;
                groupName = account.ToString();
            }
            catch (Exception exc)
            {
                Trace.TraceWarning("Failed to retrieve users group ({0})", exc);
            }

            return groupName;
        }

        /// <summary>
        /// retrieve the local machine remote desktop users group name
        /// </summary>
        /// <returns>group name</returns>
        public static string GetRemoteDesktopUsersGroupName()
        {
            var groupName = "Remote Desktop Users";

            try
            {
                var sid = new SecurityIdentifier(WellKnownSidType.BuiltinRemoteDesktopUsersSid, null);
                var account = sid.Translate(typeof(NTAccount)) as NTAccount;
                groupName = account.ToString();
            }
            catch (Exception exc)
            {
                Trace.TraceWarning("Failed to retrieve remote desktop users group ({0})", exc);
            }

            return groupName;
        }

        /// <summary>
        /// retrieve the local machine network service account name
        /// </summary>
        /// <returns>account name</returns>
        public static string GetNetworkServiceAccountName()
        {
            var accountName = "NT AUTHORITY\\NETWORK SERVICE";

            try
            {
                var sid = new SecurityIdentifier(WellKnownSidType.NetworkServiceSid, null);
                var account = sid.Translate(typeof(NTAccount)) as NTAccount;
                accountName = account.Value;
            }
            catch (Exception exc)
            {
                Trace.TraceWarning("Failed to retrieve network service account ({0})", exc);
            }

            return accountName;
        }

        #endregion

        #region Windows Users

        #region Windows Logon

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern int LogonUser(
            string lpszUserName,
            string lpszDomain,
            string lpszPassword,
            int dwLogonType,
            int dwLogonProvider,
            ref IntPtr phToken);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool CloseHandle(
            IntPtr handle);

        private const int LOGON32_LOGON_INTERACTIVE = 2;
        private const int LOGON32_PROVIDER_DEFAULT = 0;

        #endregion

        #region Windows Known Folders

        [DllImport("Shell32.dll", SetLastError = true)]
        private static extern int SHGetKnownFolderPath(
            [MarshalAs(UnmanagedType.LPStruct)]Guid rfid,
            uint dwFlags,
            IntPtr hToken,
            out IntPtr ppszPath);

        private static Guid KNOWNFOLDER_GUID_DOCUMENTS = new Guid("{FDD39AD0-238F-46AF-ADB4-6C85480369C7}");

        private static uint KNOWNFOLDER_FLAG_DEFAULTPATH = 0x00000400;
        private static uint KNOWNFOLDER_FLAG_DONTVERIFY = 0x00004000;

        #endregion

        /// <summary>
        /// retrieve a local user documents folder; also validates the user credentials to prevent unauthorized access to this folder
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns>home directory</returns>
        public static string GetLocalUserDocumentsFolder(
            string userName,
            string password)
        {
            // Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) may be used to retrieve the documents folder for the current user
            // thus, instead of using P/Invoke calls, it's also possible to impersonate the given user then use the environment variables (a bit more code involved)

            var token = IntPtr.Zero;

            try
            {
                if (LogonUser(userName, Environment.MachineName, password, LOGON32_LOGON_INTERACTIVE, LOGON32_PROVIDER_DEFAULT, ref token) != 0)
                {
                    IntPtr outPath;
                    var result = SHGetKnownFolderPath(KNOWNFOLDER_GUID_DOCUMENTS, KNOWNFOLDER_FLAG_DEFAULTPATH | KNOWNFOLDER_FLAG_DONTVERIFY, token, out outPath);
                    if (result == 0)
                    {
                        return Marshal.PtrToStringUni(outPath);
                    }
                    throw new Exception(string.Format("Failed to retrieve documents folder with result code: {0} (did the user already logged in for Windows to create its profile?)", result));
                }
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to retrieve local user {0} documents folder ({1})", userName, exc);
                throw;
            }
            finally
            {
                if (token != IntPtr.Zero)
                {
                    CloseHandle(token);
                }
            }
        }

        /// <summary>
        /// create a local user
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="description"></param>
        /// <param name="password"></param>
        /// <param name="userCannotChangePassword"></param>
        /// <param name="passwordNeverExpires"></param>
        public static void CreateLocalUser(
            string userName,
            string description,
            string password,
            bool userCannotChangePassword,
            bool passwordNeverExpires)
        {
            try
            {
                using (var context = new PrincipalContext(ContextType.Machine))
                {
                    var user = UserPrincipal.FindByIdentity(context, userName);
                    if (user == null)
                    {
                        user = new UserPrincipal(context);
                        user.Name = userName;
                        user.DisplayName = userName;
                        user.Description = description;
                        user.SetPassword(password);
                        user.UserCannotChangePassword = userCannotChangePassword;
                        user.PasswordNeverExpires = passwordNeverExpires;
                        user.Save();
                    }
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to create local user {0} ({1})", userName, exc);
                throw;
            }
        }

        /// <summary>
        /// add a local user to a group
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="groupName"></param>
        public static void AddLocalUserToGroup(
            string userName,
            string groupName)
        {
            try
            {
                using (var context = new PrincipalContext(ContextType.Machine))
                {
                    var user = UserPrincipal.FindByIdentity(context, userName);
                    if (user != null)
                    {
                        var group = GroupPrincipal.FindByIdentity(context, groupName);
                        if (group != null && !group.Members.Contains(user))
                        {
                            group.Members.Add(user);
                            group.Save();
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to add local user {0} to group {1} ({2})", userName, groupName, exc);
                throw;
            }
        }

        /// <summary>
        /// delete a local user
        /// </summary>
        /// <param name="userName"></param>
        public static void DeleteLocalUser(
            string userName)
        {
            try
            {
                using (var context = new PrincipalContext(ContextType.Machine))
                {
                    var user = UserPrincipal.FindByIdentity(context, userName);
                    if (user != null)
                    {
                        user.Delete();
                    }
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to delete local user {0} ({1})", userName, exc);
                throw;
            }
        }

        #endregion
    }
}