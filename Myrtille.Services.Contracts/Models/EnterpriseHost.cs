namespace Myrtille.Services.Contracts
{
    public class EnterpriseHost
    {
        public long HostID { get; set; }
        public string HostName { get; set; }
        public string HostAddress { get; set; }
        public SecurityProtocolEnum Protocol { get; set; }
    }
}