using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Browser
{
    public class NamedPipeServer
    {
        private const int BufferLength = 1024 * 4;
        public string PipeName { get; set; }

        public event EventHandler ClientConnected;
        public event EventHandler<MessageReceivedEventArgs> ClientMessage;
        public event EventHandler PipeClosed;

        private NamedPipeServerStream _namedPipeServerStream;
        public NamedPipeServer(string pipeName)
        {
            PipeName = pipeName;
            _namedPipeServerStream = new NamedPipeServerStream(
                pipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous | PipeOptions.WriteThrough);
        }

        public void Start()
        {
            _namedPipeServerStream.BeginWaitForConnection(GotPipeConnection, this);
        }

        private void GotPipeConnection(IAsyncResult pAsyncResult)
        {
            _namedPipeServerStream.EndWaitForConnection(pAsyncResult);

            ClientConnected?.Invoke(this, new EventArgs());

            StartReadingAsync();
        }

        public void Close()
        {
            _namedPipeServerStream.WaitForPipeDrain();
            _namedPipeServerStream.Close();
            _namedPipeServerStream.Dispose();
            _namedPipeServerStream = null;
        }

        private void StartReadingAsync()
        {
            var buffer = new byte[BufferLength];
            _namedPipeServerStream.ReadAsync(buffer, 0, BufferLength).ContinueWith(t =>
            {

                var bytesRead = t.Result;
                if (bytesRead == 0)
                {
                    PipeClosed?.Invoke(this, null);
                    return;
                }

                ClientMessage?.Invoke(this, new MessageReceivedEventArgs(buffer, bytesRead));

                StartReadingAsync();
            });
        }

        public Task WriteMessage(string msg)
        {
            var bytes = Encoding.UTF8.GetBytes(msg);
            var t = _namedPipeServerStream.WriteAsync(bytes, 0, bytes.Length);
            return t;
        }
    }

    public class MessageReceivedEventArgs : EventArgs
    {
        public byte[] Data { get; set; }
        public int BytesRead { get; set; }

        public MessageReceivedEventArgs(byte[] data, int bytesRead)
        {
            Data = data;
            BytesRead = bytesRead;
        }
    }
}
