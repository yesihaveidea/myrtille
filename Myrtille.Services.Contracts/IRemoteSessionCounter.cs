using System.ServiceModel;

namespace Myrtille.Services.Contracts
{
    [ServiceContract]
    public interface IRemoteSessionCounter
    {
        [OperationContract]
        int GetRemoteSessionId();
    }
}