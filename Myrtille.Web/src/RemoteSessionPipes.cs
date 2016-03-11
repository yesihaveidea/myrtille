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
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using Myrtille.Helpers;

namespace Myrtille.Web
{
    public class RemoteSessionPipes
    {
        public RemoteSession RemoteSession { get; private set; }

        // it's possible to have 2 ways pipes (duplex, using overlapped I/O), but it proven difficult to setup and raised concurrency access issues...
        // to keep things simple, using separate pipes...
        private NamedPipeServerStream _inputsPipe;
        public NamedPipeServerStream InputsPipe { get { return _inputsPipe; } }
        
        private NamedPipeServerStream _imagesPipe;
        public NamedPipeServerStream ImagesPipe { get { return _imagesPipe; } }

        // the pipes buffers sizes must match the ones defined in the remote session process
        // in order to avoid overloading both the bandwidth and the browser, images are limited to 1024 KB each
        private const int _inputsPipeBufferSize = 131072; // 128 KB
        public int InputsPipeBufferSize { get { return _inputsPipeBufferSize; } }

        private const int _imagesPipeBufferSize = 1048576; // 1024 KB
        public int ImagesPipeBufferSize { get { return _imagesPipeBufferSize; } }

        public delegate void ProcessImagesPipeMessageDelegate(byte[] msg);
        public ProcessImagesPipeMessageDelegate ProcessImagesPipeMessage { get; set; }

        public RemoteSessionPipes(RemoteSession remoteSession)
        {
            RemoteSession = remoteSession;
        }

        public void CreatePipes()
        {
            try
            {
                // close the pipes if already exist; they will be re-created below
                DeletePipes();

                // set the pipes access rights
                var pipeSecurity = new PipeSecurity();
                var pipeAccessRule = new PipeAccessRule(AccountHelper.GetEveryoneGroupName(), PipeAccessRights.ReadWrite, AccessControlType.Allow);
                pipeSecurity.AddAccessRule(pipeAccessRule);

                // create the pipes
                _inputsPipe = new NamedPipeServerStream(
                    "remotesession_" + RemoteSession.Id + "_inputs",
                    PipeDirection.InOut,
                    1,
                    PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous,
                    InputsPipeBufferSize,
                    InputsPipeBufferSize,
                    pipeSecurity);

                _imagesPipe = new NamedPipeServerStream(
                    "remotesession_" + RemoteSession.Id + "_outputs",
                    PipeDirection.InOut,
                    1,
                    PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous,
                    ImagesPipeBufferSize,
                    ImagesPipeBufferSize,
                    pipeSecurity);

                // wait for client connection
                InputsPipe.BeginWaitForConnection(InputsPipeConnected, InputsPipe.GetHashCode());
                ImagesPipe.BeginWaitForConnection(ImagesPipeConnected, ImagesPipe.GetHashCode());
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to create pipes, remote session {0} ({1})", RemoteSession.Id, exc);
            }
        }

        public void DeletePipes()
        {
            DisposePipe("remoteSession_" + RemoteSession.Id + "_inputs", ref _inputsPipe);
            DisposePipe("remoteSession_" + RemoteSession.Id + "_outputs", ref _imagesPipe);
        }

        public bool PipesConnected
        {
            get
            {
                return ((InputsPipe != null) && (InputsPipe.IsConnected) &&
                        (ImagesPipe != null) && (ImagesPipe.IsConnected));
            }
        }

        private void InputsPipeConnected(IAsyncResult e)
        {
            try
            {
                if (InputsPipe != null)
                {
                    InputsPipe.EndWaitForConnection(e);
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to wait for connection on inputs pipe, remote session {0} ({1})", RemoteSession.Id, exc);
            }
        }

        private void ImagesPipeConnected(IAsyncResult e)
        {
            try
            {
                if (ImagesPipe != null)
                {
                    ImagesPipe.EndWaitForConnection(e);
                    ReadImagesPipe();
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to wait for connection on images pipe, remote session {0} ({1})", RemoteSession.Id, exc);
            }
        }

        private void ReadImagesPipe()
        {
            try
            {
                while (ImagesPipe != null && ImagesPipe.IsConnected && ImagesPipe.CanRead)
                {
                    var msg = ReadImagesPipeMessage();
                    if (msg != null && msg.Length > 0)
                    {
                        ProcessImagesPipeMessage(msg);
                    }
                }
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to read images pipe, remote session {0} ({1})", RemoteSession.Id, exc);
            }
        }

        private byte[] ReadImagesPipeMessage()
        {
            try
            {
                var memoryStream = new MemoryStream();
                var buffer = new byte[ImagesPipeBufferSize];

                do
                {
                    memoryStream.Write(buffer, 0, ImagesPipe.Read(buffer, 0, buffer.Length));
                } while (ImagesPipe != null && ImagesPipe.IsConnected && ImagesPipe.CanRead && !ImagesPipe.IsMessageComplete);

                return memoryStream.ToArray();
            }
            catch (Exception exc)
            {
                Trace.TraceError("Failed to read images pipe message, remote session {0} ({1})", RemoteSession.Id, exc);
                return null;
            }
        }

        private void DisposePipe(string pipeName, ref NamedPipeServerStream pipe)
        {
            if (pipe != null)
            {
                try
                {
                    // CAUTION! closing a pipe in use can make .NET to crash! disconnect it first...
                    if (pipe.IsConnected)
                    {
                        pipe.WaitForPipeDrain();
                        pipe.Disconnect();
                    }

                    pipe.Close();
                }
                catch (Exception exc)
                {
                    Trace.TraceError("Failed to close pipe {0}, remote session {1} ({2})", pipeName, RemoteSession.Id, exc);
                }
                finally
                {
                    pipe.Dispose();
                    pipe = null;
                }
            }
        }
    }
}