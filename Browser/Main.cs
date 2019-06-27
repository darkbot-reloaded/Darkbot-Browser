using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Browser.CefHandler;
using CefSharp;
using CefSharp.WinForms;
using NamedPipeWrapper;

namespace Browser
{
    public partial class Main : Form
    {
        private BrowserWindow _browserWindow;
        private ChromiumWebBrowser _chromiumWebBrowser;
        private IntPtr _chromiumWebBrowserHandle;
        private CookieManager _cookies;
        private NamedPipeServer<string> _server;

        public Main()
        {
            InitializeComponent();
        }

        private void FrmBrowser_Load(object sender, EventArgs e)
        {
            _chromiumWebBrowser?.Dispose();

            _cookies = new CookieManager(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Cef", "cookies"), true, null);

            var browserSettings = new BrowserSettings
            {
                WindowlessFrameRate = 60,
                Plugins = CefState.Enabled,
                WebGl = CefState.Enabled
            };

            _chromiumWebBrowser = new ChromiumWebBrowser("https://darkorbit.com")
            {
                BrowserSettings = browserSettings,
                RequestContext = new RequestContext(new RequestContextHandler(_cookies)),
                RequestHandler = new RequestHandler(),
                MenuHandler = new ContextMenuHandler()
            };

            _chromiumWebBrowser.AddressChanged += ChromiumWebBrowserOnAddressChanged;
            _chromiumWebBrowser.IsBrowserInitializedChanged += ChromiumWebBrowserOnIsBrowserInitializedChanged;
            _chromiumWebBrowser.HandleCreated += ChromiumWebBrowserOnHandleCreated;
            _chromiumWebBrowser.Dock = DockStyle.Fill;
            Controls.Add(_chromiumWebBrowser);

            Task.Run((Action) CreatePipeConnection);
        }

        private void CreatePipeConnection()
        {
            _server = new NamedPipeServer<string>(Process.GetCurrentProcess().Id.ToString());
            _server.ClientMessage += ServerOnClientMessage;
            _server.Start();
        }

        private void ServerOnClientMessage(NamedPipeConnection<string, string> connection, string message)
        {
            var packet = new PacketHandler(message);

            switch (packet.Header)
            {
                case PacketHandler.PacketHeader.Login:
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
                    break;
                case PacketHandler.PacketHeader.Reload:
                    _chromiumWebBrowser.Reload(true);
                    break;
                case PacketHandler.PacketHeader.Mouse:
                    var x = packet.NextInt;
                    var y = packet.NextInt;

                    switch (packet.NextMouse)
                    {
                        case PacketHandler.MouseEvent.Move:
                            _chromiumWebBrowser.GetBrowserHost().SendMouseMoveEvent(x, y, false, CefEventFlags.None);
                            break;
                        case PacketHandler.MouseEvent.Down:
                            DoMouseDown(x, y);
                            break;
                        case PacketHandler.MouseEvent.Up:
                            DoMouseUp(x, y);
                            break;
                        case PacketHandler.MouseEvent.Click:
                            DoMouseDown(x, y);
                            DoMouseUp(x, y);
                            break;
                    }

                    ;
                    break;
                case PacketHandler.PacketHeader.Keyboard:
                    var key = packet.NextInt;

                    switch (packet.NextKey)
                    {
                        case PacketHandler.KeyBoardEvent.Down:
                            DoKeyDown(key);
                            break;
                        case PacketHandler.KeyBoardEvent.Up:
                            DoKeyUp(key);
                            break;
                        case PacketHandler.KeyBoardEvent.Click:
                            DoKeyDown(key);
                            DoKeyUp(key);
                            break;
                    }

                    ;

                    break;
                case PacketHandler.PacketHeader.Show:
                    Show();
                    break;
                case PacketHandler.PacketHeader.Hide:
                    Hide();
                    break;
                case PacketHandler.PacketHeader.BlockInput:
                    _browserWindow.BlockUserInput = packet.NextBool;
                    break;
            }

            ;
        }

        private void ChromiumWebBrowserOnIsBrowserInitializedChanged(object sender,
            IsBrowserInitializedChangedEventArgs e)
        {
            if (e.IsBrowserInitialized) Task.Run(new Action(LoopForHandle));
        }

        private void ChromiumWebBrowserOnHandleCreated(object sender, EventArgs e)
        {
            _chromiumWebBrowserHandle = _chromiumWebBrowser.Handle;
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
        }

        private async void LoopForHandle()
        {
            try
            {
                IntPtr intPtr;
                while (!RenderWidgetHostHandleSearcher.Search(_chromiumWebBrowserHandle, out intPtr))
                    await Task.Delay(10);
                _browserWindow = new BrowserWindow(_chromiumWebBrowser, intPtr);
            }
            catch (Exception e)
            {
                Logger.GetLogger().Error("[LoopForHandle] ", e);
            }
        }

        private void DoMouseDown(int x, int y)
        {
            _chromiumWebBrowser.GetBrowserHost()
                .SendMouseClickEvent(x, y, MouseButtonType.Left, false, 1, CefEventFlags.None);
        }

        private void DoMouseUp(int x, int y)
        {
            _chromiumWebBrowser.GetBrowserHost()
                .SendMouseClickEvent(x, y, MouseButtonType.Left, true, 1, CefEventFlags.None);
        }

        private void DoKeyDown(int chr)
        {
            var keyEvent = new KeyEvent {WindowsKeyCode = chr, IsSystemKey = false, Type = KeyEventType.KeyDown};


            if (chr >= 96 && chr <= 105) keyEvent.Modifiers = CefEventFlags.NumLockOn | CefEventFlags.IsKeyPad;

            _chromiumWebBrowser.GetBrowserHost().SendKeyEvent(keyEvent);
        }

        private void DoKeyUp(int chr)
        {
            var keyEvent = new KeyEvent {WindowsKeyCode = chr, IsSystemKey = false, Type = KeyEventType.KeyUp};


            if (chr >= 96 && chr <= 105) keyEvent.Modifiers = CefEventFlags.NumLockOn | CefEventFlags.IsKeyPad;

            _chromiumWebBrowser.GetBrowserHost().SendKeyEvent(keyEvent);
        }
    }
}