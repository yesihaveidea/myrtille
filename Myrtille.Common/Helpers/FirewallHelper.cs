using System;
using System.Diagnostics;
using NetFwTypeLib;

namespace Myrtille.Helpers
{
    public static class FirewallHelper
    {
        public static void OpenFirewallPort(int port, string description)
        {
            try
            {
                // firewall manager
                var TicfMgr = Type.GetTypeFromProgID("HNetCfg.FwMgr");
                var icfMgr = (INetFwMgr)Activator.CreateInstance(TicfMgr);

                // open port
                var TportClass = Type.GetTypeFromProgID("HNetCfg.FWOpenPort");
                var portClass = (INetFwOpenPort)Activator.CreateInstance(TportClass);

                // current profile
                var profile = icfMgr.LocalPolicy.CurrentProfile;

                // set properties
                portClass.Scope = NET_FW_SCOPE_.NET_FW_SCOPE_ALL;
                portClass.Protocol = NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP;
                portClass.Port = port;
                portClass.Name = description;
                portClass.Enabled = true;

                // add the port to the ICF permissions list
                profile.GloballyOpenPorts.Add(portClass);
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to open firewall port ({0})", exc);
            }
        }

        public static void CloseFirewallPort(int port)
        {
            try
            {
                // firewall manager
                var TicfMgr = Type.GetTypeFromProgID("HNetCfg.FwMgr");
                var icfMgr = (INetFwMgr)Activator.CreateInstance(TicfMgr);

                // current profile
                var profile = icfMgr.LocalPolicy.CurrentProfile;

                // add the port to the ICF permissions list
                profile.GloballyOpenPorts.Remove(port, NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP);
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to remove firewall port ({0})", exc);
            }
        }
    }
}