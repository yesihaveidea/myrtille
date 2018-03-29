using System;
using System.Diagnostics;
using System.ServiceModel;
using Myrtille.Services.Contracts;

namespace Myrtille.Web
{
    public class MFAAuthenticationClient : ClientBase<IMFAAuthentication>, IMFAAuthentication
    {
        public bool GetState()
        {
            try
            {
                return Channel.GetState();
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to get mfa adapter state ({0})", exc);
                return false;
            }
        }

        public bool Authenticate(string username, string password, string clientIP = null)
        {
            try
            {
                return Channel.Authenticate(username, password, clientIP);
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to mfa authenticate user {0} ({1})", username, exc);
                return false;
            }
        }

        public string GetPromptLabel()
        {
            try
            {
                return Channel.GetPromptLabel();
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to get mfa prompt label ({0})", exc);
                return null;
            }
        }

        public string GetProviderURL()
        {
            try
            {
                return Channel.GetProviderURL();
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to get mfa provider url ({0})", exc);
                return null;
            }
        }
    }
}