using System.ServiceModel;

namespace Myrtille.Services.Contracts
{
    [ServiceContract]
    public interface IMFAAuthentication
    {
        [OperationContract]
        bool GetState();

        [OperationContract]
        bool Authenticate(string username, string password, string clientIP = null);

        [OperationContract]
        string GetPromptLabel();

        [OperationContract]
        string GetProviderURL();
    }
}