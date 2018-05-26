using System;
using System.Diagnostics;
using System.IO;
using Myrtille.Services.Contracts;

namespace Myrtille.Services
{
    public class PrinterService : IPrinterService
    {
        public Stream GetPdfFile(int remoteSessionId, string fileName)
        {
            Stream fileStream = null;

            try
            {
                if (File.Exists(Path.Combine(Path.GetTempPath(), fileName)))
                {
                    fileStream = File.Open(Path.Combine(Path.GetTempPath(), fileName), FileMode.Open, FileAccess.Read, FileShare.Read);
                    Trace.TraceInformation("Downloaded pdf file {0}, remote session {1}", fileName, remoteSessionId);
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to download pdf file {0}, remote session {1} ({2})", fileName, remoteSessionId, exc);
                throw;
            }

            return fileStream;
        }

        public void DeletePdfFile(int remoteSessionId, string fileName)
        {
            try
            {
                if (File.Exists(Path.Combine(Path.GetTempPath(), fileName)))
                {
                    File.Delete(Path.Combine(Path.GetTempPath(), fileName));
                    Trace.TraceInformation("Deleted pdf file {0}, remote session {1}", fileName, remoteSessionId);
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to delete pdf file {0}, remote session {1} ({2})", fileName, remoteSessionId, exc);
                throw;
            }
        }
    }
}