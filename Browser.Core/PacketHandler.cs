using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CefSharp;

namespace Browser.Core
{
    public class PacketHandler
    {
        public delegate void MouseMove(int x, int y);
        public delegate void MouseDown(int x, int y);
        public delegate void MouseUp(int x, int y);
        public delegate void KeyClick(char chr);
        public delegate void Show();
        public delegate void Hide();
        public delegate void BlockUserInput(bool block);
        public delegate void Log(string message);

        public delegate void SetCookie(Cookie cookies, string server);
        public delegate void LoadUrl(string url);
        public delegate void Reload();

        public delegate Task SendPipeMessage(string message);

        private MouseMove _mouseMove;
        private MouseDown _mouseDown;
        private MouseUp _mouseUp;
        private KeyClick _keyClick;
        private Show _show;
        private Hide _hide;
        private BlockUserInput _blockUserInput;
        private Log _log;

        private SetCookie _setCookie;
        private LoadUrl _loadUrl;
        private Reload _reload;

        private SendPipeMessage _sendPipeMessage;

        public PacketHandler(MouseMove mouseMove, MouseDown mouseDown, MouseUp mouseUp, KeyClick keyClick, Show show, Hide hide, BlockUserInput blockUserInput, Log log, SetCookie setCookie, LoadUrl loadUrl, Reload reload, SendPipeMessage sendPipeMessage)
        {
            _mouseMove = mouseMove;
            _mouseDown = mouseDown;
            _mouseUp = mouseUp;
            _keyClick = keyClick;
            _show = show;
            _hide = hide;
            _blockUserInput = blockUserInput;
            _log = log;

            _setCookie = setCookie;
            _loadUrl = loadUrl;
            _reload = reload;

            _sendPipeMessage = sendPipeMessage;
        }

        public void ServerOnClientMessage(object sender, MessageReceivedEventArgs e)
        {
            var message = Encoding.UTF8.GetString(e.Data, 0, e.BytesRead);
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
                    _mouseDown(x, y);
                    _mouseUp(x, y);
                }

            }
            else if (packet.Header == PacketParser.PacketHeader.Keyboard)
            {
                var keyEvent = packet.NextKeyboardEvent;
                var key = packet.Next[0];
                if (keyEvent == PacketParser.KeyboardEvent.Down)
                {
                    _log($"key down not implemented");

                }
                else if (keyEvent == PacketParser.KeyboardEvent.Up)
                {
                    _log($"key up not implemented");
                 
                }
                else if (keyEvent == PacketParser.KeyboardEvent.Click)
                {
                    _log($"keyclick start");
                    _keyClick(key);
                    _log($"keyclick end");
                    _sendPipeMessage("1");
                    _log($"keyclick sent");
                }
            }
            else if (packet.Header == PacketParser.PacketHeader.Show)
            {
                _log("show");
                _show();
            }
            else if (packet.Header == PacketParser.PacketHeader.Hide)
            {
                _log("hide");
                _hide();
            }
            else if (packet.Header == PacketParser.PacketHeader.BlockInput)
            {
                var block = packet.NextBool;
                _log($"block input {block}");
                _blockUserInput(block);
            }
        }
    }
}
