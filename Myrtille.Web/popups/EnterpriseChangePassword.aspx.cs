using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Myrtille.Web
{
    public partial class EnterpriseChangePassword : System.Web.UI.Page
    {
        private EnterpriseServiceClient _enterpriseClient;
        private string _userName = null;
        /// <summary>
        /// page init
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Page_Init(
            object sender,
            EventArgs e)
        {
            _enterpriseClient = new EnterpriseServiceClient();
        }
        
        /// <summary>
        /// page load (postback data is now available)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Page_Load(
            object sender,
            EventArgs e)
        {
            // retrieve the host
            if (Request["userId"] != null)
            {
                userName.Value = Request["userId"];
                _userName = Request["userId"];

                if (!IsPostBack && Request["edit"] == null)
                {
                    //try
                    //{
                    //    var host = _enterpriseClient.GetHost(_hostId.Value, _enterpriseSession.SessionID);
                    //    if (host != null)
                    //    {
                    //        hostName.Value = host.HostName;
                    //        hostAddress.Value = host.HostAddress;
                    //        groupsAccess.Value = host.DirectoryGroups;
                    //        securityProtocol.SelectedIndex = (int)host.Protocol;
                    //    }
                    //}
                    //catch (Exception exc)
                    //{
                    //    System.Diagnostics.Trace.TraceError("Failed to retrieve host {0}, ({1})", _hostId, exc);
                    //}
                }
            }
            else
            {
                changePassword.Disabled = true;
            }
        }

        /// <summary>
        /// change a user password
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void ChangePasswordButtonClick(
            object sender,
            EventArgs e)
        {
            if (_enterpriseClient == null && !string.IsNullOrEmpty(_userName))
                return;

            try
            {
                if(string.IsNullOrEmpty(oldPassword.Value))
                {
                    changeError.InnerText = "Old password must be specified";
                }else
                if (!string.Equals(newPassword.Value, confirmPassword.Value))
                {
                    changeError.InnerText = "New and confirmed passwords do not match";
                }
                else
                {
                    bool bResult = _enterpriseClient.ChangeUserPassword(_userName, oldPassword.Value, newPassword.Value);

                    if (bResult)
                    {
                        Response.Redirect(Request.RawUrl + (Request.RawUrl.Contains("?") ? "&" : "?") + "change=success");
                    }
                    else
                    {
                        changeError.InnerText = "Password change failed";
                    }
                }
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Failed to change user password {0} ({1})", _userName, exc);
                changeError.InnerText = "Password change failed";
            }
        }
    }
}