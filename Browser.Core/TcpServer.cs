using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Browser.Core
{
    public class TcpServer
    {
        public delegate void SocketConnected(Socket socket);
        private SocketConnected _socketConnected;

        private Delegates.Log _log;
        private int _port;
        private Socket _listener;
        private ManualResetEvent _allDone = new ManualResetEvent(false);

        public TcpServer(int port, SocketConnected socketConnected, Delegates.Log log)
        {
            _port = port;
            _socketConnected = socketConnected;
            _log = log;
        }
        public void Start()
        {
            _listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            try
            {
                _listener.Bind(new IPEndPoint(IPAddress.Any, _port));
                _listener.Listen(100);
                _log("Server running at port " + _port);
                while (true)
                {
                    _allDone.Reset();

                    _listener.BeginAccept(AcceptCallback, _listener);

                    _allDone.WaitOne();
                }

            }
            catch (Exception e)
            {
                _log(e.ToString());
            }
        }

        public void Stop()
        {
            _listener?.Dispose();
            _listener?.Close();
        }

        private void AcceptCallback(IAsyncResult ar)
        {

            try
            {
                var listener = (Socket) ar.AsyncState;
                var socket = listener.EndAccept(ar);

                _socketConnected?.Invoke(socket);
            }
            catch (Exception)
            {
               
            }
            finally
            {
                _allDone.Set();
            }
           
        }
    }
}
