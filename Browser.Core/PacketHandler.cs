using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using CefSharp;

namespace Browser.Core
{
    public class PacketHandler
    {


        private Socket _socket;

        private Queue<string> _packetQueue;
        private byte[] buffer = new byte[512];
        private bool _connected;
        private readonly Task _handlePackets;

        private Delegates.MouseMove _mouseMove;
        private Delegates.MouseDown _mouseDown;
        private Delegates.MouseUp _mouseUp;
        private Delegates.MouseClick _mouseClick;
        private Delegates.KeyClick _keyClick;
        private Delegates.Show _show;
        private Delegates.Hide _hide;
        private Delegates.BlockUserInput _blockUserInput;
        private Delegates.Log _log;

        private Delegates.SetCookie _setCookie;
        private Delegates.LoadUrl _loadUrl;
        private Delegates.Reload _reload;

        public PacketHandler(Socket socket, Delegates.MouseMove mouseMove, Delegates.MouseDown mouseDown, Delegates.MouseUp mouseUp, Delegates.MouseClick mouseClick, Delegates.KeyClick keyClick, Delegates.Show show, Delegates.Hide hide, Delegates.BlockUserInput blockUserInput, Delegates.Log log, Delegates.SetCookie setCookie, Delegates.LoadUrl loadUrl, Delegates.Reload reload)
        {
            _socket = socket;
            _connected = true;
            _packetQueue = new Queue<string>();

            _handlePackets = new Task(HandlePacketsAsync);
            _handlePackets.Start();

            _socket.BeginReceive(buffer, 0, buffer.Length, 0, ReceivePacketsAsync, this);

            _mouseMove = mouseMove;
            _mouseDown = mouseDown;
            _mouseUp = mouseUp;
            _mouseClick = mouseClick;
            _keyClick = keyClick;
            _show = show;
            _hide = hide;
            _blockUserInput = blockUserInput;
            _log = log;

            _setCookie = setCookie;
            _loadUrl = loadUrl;
            _reload = reload;
        }

        private void ReceivePacketsAsync(IAsyncResult ar)
        {
            try
            {
                if (_socket == null || !_socket.Connected)
                {
                    if (_connected)
                    {
                        _connected = false;
                    }

                    return;
                }

                var bytesRead = _socket.EndReceive(ar);

                if (bytesRead <= 0) return;

                var content = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                var data = content.Split('#');
                foreach (var s in data)
                {
                    if (!string.IsNullOrEmpty(s) && !string.IsNullOrWhiteSpace(s))
                    {
                        _packetQueue.Enqueue(s);
                    }
                }

                _socket.BeginReceive(buffer, 0, buffer.Length, 0, ReceivePacketsAsync, this);
            }
            catch (Exception e)
            {
                if (!IsConnected())
                {
                    _connected = false;
                    _log("Socket disconnected");
                }
                else
                {
                    _log(e.ToString());

                }
            }
        }

        private bool IsConnected()
        {
            if (_socket == null)
            {
                return false;
            }
            try
            {
                return !(_socket.Poll(1, SelectMode.SelectRead) && _socket.Available == 0);
            }
            catch (SocketException) { return false; }
        }

        private async void HandlePacketsAsync()
        {
            while (_connected)
            {
                if (_packetQueue == null || _packetQueue.Count == 0)
                {
                    await Task.Delay(5);
                    continue;
                }

                var message = _packetQueue.Dequeue();
                if (string.IsNullOrEmpty(message))
                {
                    continue;
                }

                message = message.Replace("#", string.Empty);

                if (string.IsNullOrEmpty(message))
                {
                    continue;
                }

                var packet = new PacketParser(message);

                if (packet.Header == PacketParser.PacketHeader.Login)
                {
                    var server = packet.Next;
                    var sid = packet.Next;

                    var cookie = new Cookie
                    {
                        Name = "dosid",
                        Value = sid,
                        Domain = $"{server}.darkorbit.com",
                        Secure = true,
                        Creation = DateTime.Now
                    };

                    _setCookie(cookie, server);
                    _loadUrl($"https://www.{server}.darkorbit.com/indexInternal.es?action=internalStart");
                }
                else if (packet.Header == PacketParser.PacketHeader.Reload)
                {
                    _log(message);
                    _reload();
                }
                else if (packet.Header == PacketParser.PacketHeader.Mouse)
                {
                    var mouseEvent = packet.NextMouseEvent;
                    var x = packet.NextInt;
                    var y = packet.NextInt;

                    if (mouseEvent == PacketParser.MouseEvent.Move)
                    {
                        _mouseMove(x, y);
                    }
                    else if (mouseEvent == PacketParser.MouseEvent.Down)
                    {
                        _mouseDown(x, y);
                    }
                    else if (mouseEvent == PacketParser.MouseEvent.Up)
                    {
                        _mouseUp(x, y);
                    }

                    else if (mouseEvent == PacketParser.MouseEvent.Click)
                    {
                        _log(message);
                        await _mouseClick(x, y);
                    }

                }
                else if (packet.Header == PacketParser.PacketHeader.Keyboard)
                {
                    var keyEvent = packet.NextKeyboardEvent;
                    var key = packet.Next[0];
                    if (keyEvent == PacketParser.KeyboardEvent.Click)
                    {
                        _keyClick(key);
                    }
                }
                else if (packet.Header == PacketParser.PacketHeader.Show)
                {
                    _show();
                }
                else if (packet.Header == PacketParser.PacketHeader.Hide)
                {
                    _hide();
                }
                else if (packet.Header == PacketParser.PacketHeader.BlockInput)
                {
                    var block = packet.NextBool;
                    _blockUserInput(block);
                }
            }
        }
    }
}
