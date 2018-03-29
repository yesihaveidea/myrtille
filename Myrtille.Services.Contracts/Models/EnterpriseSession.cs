namespace Myrtille.Services.Contracts
{
    public class EnterpriseSession
    {
        public bool IsAdmin { get; set; }
        public string SessionID { get; set; }
        public string SessionKey { get; set; }
    }
}