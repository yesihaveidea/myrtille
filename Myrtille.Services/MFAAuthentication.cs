using Myrtille.Services.Contracts;

namespace Myrtille.Services
{
    public class MFAAuthentication : IMFAAuthentication
    {
        public bool GetState()
        {
            return Program._multifactorAdapter != null;
        }

        public bool Authenticate(string username, string password, string clientIP = null)
        {
            return Program._multifactorAdapter.Authenticate(username, password, clientIP);
        }

        public string GetPromptLabel()
        {
            return Program._multifactorAdapter.PromptLabel;
        }

        public string GetProviderURL()
        {
            return Program._multifactorAdapter.ProviderURL;
        }
    }
}