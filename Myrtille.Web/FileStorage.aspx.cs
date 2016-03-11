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
using System.Threading;
using System.Web;
using System.Web.UI;
using Myrtille.Helpers;
using Myrtille.Services.Contracts;

namespace Myrtille.Web
{
    public partial class FileStorage : Page
    {
        private RemoteSessionManager _remoteSessionManager;
        private LocalFileStorageClient _localFileStorageClient;

        /// <summary>
        /// initialization
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void Page_Init(
            object sender,
            EventArgs e)
        {
            try
            {
                // retrieve the active remote session, if any
                if (HttpContext.Current.Session[HttpSessionStateVariables.RemoteSessionManager.ToString()] != null)
                {
                    try
                    {
                        _remoteSessionManager = (RemoteSessionManager)HttpContext.Current.Session[HttpSessionStateVariables.RemoteSessionManager.ToString()];
                    }
                    catch (Exception exc)
                    {
                        System.Diagnostics.Trace.TraceError("Failed to retrieve remote session manager ({0})", exc);
                    }
                }

                // ensure there is an active remote session, localhost connection
                // as it's a local file storage only, it excludes domain users
                // user credentials will be checked against the local machine prior to any file operation
                // if possible, use SSL to communicate with the service
                if (_remoteSessionManager != null && (_remoteSessionManager.RemoteSession.State == RemoteSessionState.Connecting || _remoteSessionManager.RemoteSession.State == RemoteSessionState.Connected) &&
                   (_remoteSessionManager.RemoteSession.ServerAddress.ToLower() == "localhost" || _remoteSessionManager.RemoteSession.ServerAddress == "127.0.0.1" || _remoteSessionManager.RemoteSession.ServerAddress == HttpContext.Current.Request.Url.Host) &&
                   string.IsNullOrEmpty(_remoteSessionManager.RemoteSession.UserDomain) && !string.IsNullOrEmpty(_remoteSessionManager.RemoteSession.UserName) && !string.IsNullOrEmpty(_remoteSessionManager.RemoteSession.UserPassword))
                {
                    _localFileStorageClient = new LocalFileStorageClient();

                    var files = _localFileStorageClient.GetLocalUserDocumentsFolderFiles(
                        _remoteSessionManager.RemoteSession.UserName,
                        _remoteSessionManager.RemoteSession.UserPassword);

                    if (files.Count > 0)
                    {
                        fileToDownloadSelect.DataSource = files;
                        fileToDownloadSelect.DataBind();
                        downloadFileButton.Disabled = false;
                    }
                }
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Failed to init file storage ({0})", exc);
            }
        }

        /// <summary>
        /// upload a file to "My documents" folder
        /// the file will be available to the rdp session ONLY if the rdp server is on the same machine as the rdp client...
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void UploadFileButtonClick(
            object sender,
            EventArgs e)
        {
            try
            {
                if (_localFileStorageClient == null)
                    return;

                if (fileToUploadText.PostedFile == null)
                {
                    System.Diagnostics.Trace.TraceInformation("File to upload is missing");
                }
                else if (fileToUploadText.PostedFile.ContentLength == 0)
                {
                    System.Diagnostics.Trace.TraceInformation("File to upload is empty");
                }
                else
                {
                    _localFileStorageClient.UploadFileToLocalUserDocumentsFolder(
                        new UploadRequest
                        {
                            UserName = _remoteSessionManager.RemoteSession.UserName,
                            UserPassword = _remoteSessionManager.RemoteSession.UserPassword,
                            FileName = fileToUploadText.PostedFile.FileName,
                            Stream = fileToUploadText.PostedFile.InputStream
                        });

                    // reload the page to have the newly uploaded file available for download
                    Response.Redirect(Request.RawUrl + (Request.RawUrl.Contains("?") ? "&" : "?") + "upload=success");
                }
            }
            catch (ThreadAbortException)
            {
                // occurs because the response is ended after reloading the page
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Failed to upload file ({0})", exc);
            }
        }

        /// <summary>
        /// download a file from "My documents" folder
        /// the file will be available to the rdp session ONLY if the rdp server is on the same machine as the rdp client...
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void DownloadFileButtonClick(
            object sender,
            EventArgs e)
        {
            try
            {
                if (_localFileStorageClient == null)
                    return;

                if (!string.IsNullOrEmpty(fileToDownloadSelect.Value))
                {
                    var fileStream = _localFileStorageClient.DownloadFileFromLocalUserDocumentsFolder(
                        _remoteSessionManager.RemoteSession.UserName,
                        _remoteSessionManager.RemoteSession.UserPassword,
                        fileToDownloadSelect.Value);

                    FileHelper.DownloadFile(Response, fileStream, fileToDownloadSelect.Value, true);
                }
            }
            catch (ThreadAbortException)
            {
                // occurs because the response is ended after sending the file content
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.TraceError("Failed to download file ({0})", exc);
            }
        }
    }
}