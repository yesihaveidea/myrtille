using System.Web.SessionState;
using Myrtille.Services.Contracts;

namespace Myrtille.Web
{
    public class SharingInfo
    {
        public HttpSessionState HttpSession { get; set; }
        public RemoteSession RemoteSession { get; set; }
        public GuestInfo GuestInfo { get; set; }
    }
}