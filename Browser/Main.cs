using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Browser.CefHandler;
using CefSharp;
using CefSharp.OffScreen;
using NamedPipeWrapper;

namespace Browser
{
    public partial class Main : Form
    {
        private ChromiumWebBrowser _chromiumWebBrowser;
        private RenderHandler _renderHandler;
        private bool _blockUserInput;
        private bool _userOnMap;
        private CookieManager _cookies;
        private NamedPipeServer<string> _server;

        public void Log(string text)
        {
            if (richTextBox1.InvokeRequired)
            {
                richTextBox1.BeginInvoke(new Action(delegate {
                    Log(text);
                }));
                return;
            }

            var nDateTime = DateTime.Now.ToString("hh:mm:ss tt") + " - ";


            richTextBox1.SelectionStart = richTextBox1.Text.Length;
            richTextBox1.SelectionColor = Color.Black;

            if (richTextBox1.Lines.Length == 0)
            {
                richTextBox1.AppendText(nDateTime + text);
                richTextBox1.ScrollToCaret();
                richTextBox1.AppendText(System.Environment.NewLine);
            }
            else
            {
                richTextBox1.AppendText(nDateTime + text + System.Environment.NewLine);
                richTextBox1.ScrollToCaret();
            }
        }
        public Main()
        {
            InitializeComponent();
            KeyPreview = true;
        }

        private void main_Load(object sender, EventArgs e)
        {
            _chromiumWebBrowser?.Dispose();

            _cookies = new CookieManager(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Cef", "cookies"), true, null);

            var browserSettings = new BrowserSettings
            {
                WindowlessFrameRate = 60,
                Plugins = CefState.Enabled,
                WebGl = CefState.Enabled,
            };

            _chromiumWebBrowser = new ChromiumWebBrowser("https://darkorbit.com", browserSettings, new RequestContext(new RequestContextHandler(_cookies)))
            {
                Size = pbBrowser.Size,
                RequestHandler = new RequestHandler(),
                MenuHandler = new ContextMenuHandler()
            };
            _renderHandler = new RenderHandler(_chromiumWebBrowser);
            _renderHandler.BrowserPaint += (bitmap) =>
            {
                pbBrowser.Image = bitmap;
            };

            _chromiumWebBrowser.RenderHandler = _renderHandler;

            _chromiumWebBrowser.AddressChanged += ChromiumWebBrowserOnAddressChanged;


            Task.Run(CreatePipeConnection);
        }

        private void CreatePipeConnection()
        {
            _server = new NamedPipeServer<string>(Process.GetCurrentProcess().Id.ToString());
            _server.ClientMessage += ServerOnClientMessage;
            _server.ClientConnected += connection => { Log($"client connected {connection.Name}"); };
            _server.ClientDisconnected += connection => { Log($"client disconnected {connection.Name}"); };
            _server.Error += exception => { Log(exception.ToString()); };
            _server.Start();
        }

        private void ServerOnClientMessage(NamedPipeConnection<string, string> connection, string message)
        {
            Log(message);
            var packet = new PacketHandler(message);

            if (packet.Header == PacketHandler.PacketHeader.Login)
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

                _cookies.DeleteCookies($"https://www.{server}.darkorbit.com/", "dosid", null);
                _cookies.SetCookie($"https://www.{server}.darkorbit.com", cookie, null);
                _chromiumWebBrowser.Load(
                    $"https://www.{server}.darkorbit.com/indexInternal.es?action=internalStart");
            }
            else if (packet.Header == PacketHandler.PacketHeader.Reload)
            {
                _chromiumWebBrowser.Reload(true);
            }
            else if (packet.Header == PacketHandler.PacketHeader.Mouse)
            {
                var x = packet.NextInt;
                var y = packet.NextInt;

                if (packet.NextMouseEvent == PacketHandler.MouseEvent.Move)
                {
                    Log($"Mouse move {x} {y}");
                    _chromiumWebBrowser.GetBrowserHost().SendMouseMoveEvent(x, y, false, CefEventFlags.None);
                }
                else if (packet.NextMouseEvent == PacketHandler.MouseEvent.Down)
                {
                    Log($"Mouse down {x} {y}");
                    DoMouseDown(x, y);
                }
                else if (packet.NextMouseEvent == PacketHandler.MouseEvent.Up)
                {
                    Log($"Mouse up {x} {y}");
                    DoMouseUp(x, y);
                }
                else if (packet.NextMouseEvent == PacketHandler.MouseEvent.Click)
                {
                    Log($"Mouse click {x} {y}");
                    DoMouseDown(x, y);
                    DoMouseUp(x, y);
                }

            }
            else if (packet.Header == PacketHandler.PacketHeader.Keyboard)
            {
                var e = packet.NextKeyboardEvent;
                var key = packet.NextInt;
                if (e == PacketHandler.KeyboardEvent.Down)
                {
                    Log($"Key down {key}");
                    DoKeyDown(key);
                }
                else if (e == PacketHandler.KeyboardEvent.Up)
                {
                    Log($"Key up {key}");
                    DoKeyUp(key);
                }
                else if (e == PacketHandler.KeyboardEvent.Click)
                {
                    Log($"Key click {key}");
                    if (_userOnMap)
                    {
                        DoKeyDown(key);
                        DoKeyUp(key);
                        DoKeyPress(key);
                    }
                    else
                    {
                        DoKeyPress(key);
                    }
                }
            }
            else if (packet.Header == PacketHandler.PacketHeader.Show)
            {
                Log($"show");
                Invoke(new Action(Show));
                _renderHandler.Render = true;
            }
            else if (packet.Header == PacketHandler.PacketHeader.Hide)
            {
                Log($"hide");
                Invoke(new Action(Hide));
                _renderHandler.Render = false;
            }
            else if (packet.Header == PacketHandler.PacketHeader.BlockInput)
            {
                Log($"block input");
                _blockUserInput = packet.NextBool;
            }
        }

        private void ChromiumWebBrowserOnAddressChanged(object sender, AddressChangedEventArgs e)
        {
            if (e.Address.Contains("/indexInternal.es?action=internalStart"))
            {
                var match = Regex.Match(e.Address, "https://(.*?).darkorbit");

                if (match.Success)
                    _chromiumWebBrowser.Load("https://" + match.Groups[1].Value +
                                             ".darkorbit.com/indexInternal.es?action=internalMapRevolution");
            }

            if (e.Address.Contains("/indexInternal.es?action=internalMapRevolution"))
            {
                _userOnMap = true;
            }
            else
            {
                _userOnMap = false;
            }
        }

        private void DoMouseMove(int x, int y)
        {
            if (!_chromiumWebBrowser.IsBrowserInitialized)
            {
                return;
            }
            _chromiumWebBrowser.GetBrowserHost().SendMouseMoveEvent(x, y, false, CefEventFlags.None);
        }
        private void DoMouseDown(int x, int y)
        {
            if (!_chromiumWebBrowser.IsBrowserInitialized)
            {
                return;
            }
            _chromiumWebBrowser.GetBrowserHost()
                .SendMouseClickEvent(x, y, MouseButtonType.Left, false, 1, CefEventFlags.None);
        }

        private void DoMouseUp(int x, int y)
        {
            if (!_chromiumWebBrowser.IsBrowserInitialized)
            {
                return;
            }
            _chromiumWebBrowser.GetBrowserHost()
                .SendMouseClickEvent(x, y, MouseButtonType.Left, true, 1, CefEventFlags.None);
        }

        private void DoKeyDown(int chr)
        {
            if (!_chromiumWebBrowser.IsBrowserInitialized)
            {
                return;
            }
            var keyEvent = new KeyEvent
            {
                FocusOnEditableField = !_userOnMap,
                WindowsKeyCode = chr,
                Modifiers = CefEventFlags.None,
                Type = KeyEventType.KeyDown,
                IsSystemKey = false,
            };

            if (chr >= 0x00 && chr <= 0x2F)
            {
                keyEvent.Type = KeyEventType.KeyDown;
            }


            _chromiumWebBrowser.GetBrowserHost().SendKeyEvent(keyEvent);
        }

        private void DoKeyUp(int chr)
        {
            if (!_chromiumWebBrowser.IsBrowserInitialized)
            {
                return;
            }
            var keyEvent = new KeyEvent
            {
                FocusOnEditableField = !_userOnMap,
                WindowsKeyCode = chr,
                Modifiers = CefEventFlags.None,
                Type = KeyEventType.KeyUp,
                IsSystemKey = false,
            };

            if (chr >= 0x00 && chr <= 0x2F)
            {
                keyEvent.Type = KeyEventType.KeyDown;
            }


            _chromiumWebBrowser.GetBrowserHost().SendKeyEvent(keyEvent);
        }

        private void DoKeyPress(int chr)
        {
            if (!_chromiumWebBrowser.IsBrowserInitialized)
            {
                return;
            }
            var keyEvent = new KeyEvent
            {
                FocusOnEditableField = !_userOnMap,
                WindowsKeyCode = chr,
                Modifiers = CefEventFlags.None,
                Type = KeyEventType.Char,
                IsSystemKey = false,
            };

            if (chr >= 0x00 && chr <= 0xC)
            {
                keyEvent.Type = KeyEventType.KeyDown;
            }


            _chromiumWebBrowser.GetBrowserHost().SendKeyEvent(keyEvent);
        }

        private void pbBrowser_MouseDown(object sender, MouseEventArgs e)
        {
            if (!_blockUserInput && e.Button == MouseButtons.Left)
            {
                DoMouseDown(e.X, e.Y);
            }
        }

        private void pbBrowser_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_blockUserInput)
            {
                DoMouseMove(e.X, e.Y);
            }
        }

        private void pbBrowser_MouseUp(object sender, MouseEventArgs e)
        {
            if (!_blockUserInput && e.Button == MouseButtons.Left)
            {
                DoMouseUp(e.X, e.Y);
            }
        }

        private void main_Resize(object sender, EventArgs e)
        {
            if (_chromiumWebBrowser != null)
            {
                _chromiumWebBrowser.Size = pbBrowser.Size;
            }
        }

        private async void main_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (pbBrowser.Bounds.Contains(PointToClient(Cursor.Position)))
            {
                if (!_blockUserInput)
                {
                    if (_userOnMap)
                    {
                        DoKeyDown(e.KeyChar);
                        await Task.Delay(50);
                        DoKeyUp(e.KeyChar);
                        await Task.Delay(50);
                        DoKeyPress(e.KeyChar);
                    }
                    else
                    {
                        DoKeyPress(e.KeyChar);
                    }
                }
            }
            e.Handled = true;
        }
    }
}