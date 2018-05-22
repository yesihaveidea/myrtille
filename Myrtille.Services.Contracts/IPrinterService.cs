using System.IO;
using System.ServiceModel;

namespace Myrtille.Services.Contracts
{
    [ServiceContract]
    public interface IPrinterService
    {
        [OperationContract]
        Stream GetPdfFile(int remoteSessionId, string fileName);

        [OperationContract]
        void DeletePdfFile(int remoteSessionId, string fileName);
    }
}