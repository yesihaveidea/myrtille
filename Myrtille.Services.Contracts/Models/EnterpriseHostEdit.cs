namespace Myrtille.Services.Contracts
{
    public class EnterpriseHostEdit
    {
        public long HostID { get; set; }
        public string HostName { get; set; }
        public string HostAddress { get; set; }
        public string DirectoryGroups { get; set; }
        public SecurityProtocolEnum Protocol { get; set; }
    }
}