/*
    Myrtille: A native HTML4/5 Remote Desktop Protocol client.

    Copyright(c) 2014-2016 Cedric Coste

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

        http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.ServiceModel;
using Myrtille.Services.Contracts;

namespace Myrtille.Web
{
    public class LocalFileStorageClient : ClientBase<ILocalFileStorage>, ILocalFileStorage
    {
        public List<string> GetLocalUserDocumentsFolderFiles(string userName, string userPassword)
        {
            try
            {
                return Channel.GetLocalUserDocumentsFolderFiles(userName, userPassword);
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to list file(s) from local user {0} documents folder ({1})", userName, exc);
                throw;
            }
        }

        public void UploadFileToLocalUserDocumentsFolder(UploadRequest uploadRequest)
        {
            try
            {
                Channel.UploadFileToLocalUserDocumentsFolder(uploadRequest);
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to upload file {0} to local user {1} documents folder ({2})", uploadRequest.FileName, uploadRequest.UserName, exc);
                throw;
            }
        }

        public Stream DownloadFileFromLocalUserDocumentsFolder(string userName, string userPassword, string fileName)
        {
            try
            {
                return Channel.DownloadFileFromLocalUserDocumentsFolder(userName, userPassword, fileName);
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to download file {0} from local user {1} documents folder ({2})", fileName, userName, exc);
                throw;
            }
        }
    }
}