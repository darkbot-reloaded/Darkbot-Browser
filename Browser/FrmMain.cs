using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Browser.Core;
using Browser.Core.CefHandler;
using CefSharp;
using CefSharp.WinForms;

namespace Browser
{
    public partial class FrmMain : Form
    {
        private BrowserWindow _browserWindow;
        private BrowserWindow _browserWidgetWindow;
        private ChromiumWebBrowser _chromiumWebBrowser;
        private IntPtr _chromiumWebBrowserHandle;
        private CookieManager _cookies;

        private Thread _pipeThread;
        private TcpServer _server;

        private PacketHandler _packetHandler;

        public FrmMain()
        {
            InitializeComponent();
            SetDoubleBuffering(this, true);
        }

        private void FrmMain_Load(object sender, EventArgs e)
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
                MenuHandler = new ContextMenuHandler(),
                LifeSpanHandler = new LifeSpanHandler()
                
            };

            _chromiumWebBrowser.AddressChanged += ChromiumWebBrowserOnAddressChanged;
            _chromiumWebBrowser.IsBrowserInitializedChanged += ChromiumWebBrowserOnIsBrowserInitializedChanged;
            _chromiumWebBrowser.HandleCreated += ChromiumWebBrowserOnHandleCreated;
            Controls.Add(_chromiumWebBrowser);

            SetDoubleBuffering(_chromiumWebBrowser, true);

            _pipeThread = new Thread(CreateTcpServer);
            _pipeThread.Start();
        }

        public void Log(string text)
        {
            var dt = DateTime.Now.ToString("hh:mm:ss.fff tt") + " - ";

            var logText = dt + " " + text + Environment.NewLine;
            Logger.GetLogger().Debug(text);
        }

        private void SetDoubleBuffering(Control control, bool value)
        {
            var controlProperty = typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (controlProperty != null)
            {
                controlProperty.SetValue(control, value, null);
            }
        }

        private void CreateTcpServer()
        {
            var arguments = Environment.GetCommandLineArgs();
            Log("GetCommandLineArgs: " + string.Join(", ", arguments));

            var port = 8080;

            if (arguments.Length == 1)
            {
                if(int.TryParse(arguments[0], out var tryPort))
                {
                    port = tryPort;
                }
                
            }
            Log("Assigned port: " + port);
            _server = new TcpServer(port, SocketConnected, Log);
            _server.Start();
        }

        private void SocketConnected(Socket socket)
        {
            Log("client connected");
            _packetHandler = new PacketHandler(socket, DoMouseMove, DoMouseDown, DoMouseUp, DoMouseClick, DoKeyClick, Show, Hide,
                (block) => { _browserWindow.BlockUserInput = block; }, Log, SetCookie, _chromiumWebBrowser.Load, _chromiumWebBrowser.Reload);

        }

        #region Chromium browser
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
        private void SetCookie(Cookie cookie, string server)
        {
            _cookies.DeleteCookies($"https://www.{server}.darkorbit.com/", "dosid", null);
            _cookies.SetCookie($"https://www.{server}.darkorbit.com", cookie, null);
        }
        #endregion

        #region Flash hwnd 
        private void ChromiumWebBrowserOnIsBrowserInitializedChanged(object sender, IsBrowserInitializedChangedEventArgs e)
        {
            if (e.IsBrowserInitialized)
            {
                Task.Run(LoopForRenderHandle);
                Task.Run(LoopForWidgetHandle);
            }
        }

        private async void LoopForRenderHandle()
        {
            try
            {
                var searcher = new WindowClassSearcher("Chrome_RenderWidgetHostHWND");
                IntPtr intPtr;
                while (!searcher.Search(_chromiumWebBrowserHandle, out intPtr))
                    await Task.Delay(10);
                _browserWindow = new BrowserWindow(_chromiumWebBrowser, intPtr);
            }
            catch (Exception e)
            {
                Logger.GetLogger().Error("[LoopForRenderHandle] ", e);
            }
        }

        private async void LoopForWidgetHandle()
        {
            try
            {
                var searcher = new WindowClassSearcher("Chrome_WidgetWin_0");
                IntPtr intPtr;
                while (!searcher.Search(_chromiumWebBrowserHandle, out intPtr))
                    await Task.Delay(10);
                _browserWidgetWindow = new BrowserWindow(_chromiumWebBrowser,intPtr);
            }
            catch (Exception e)
            {
                Logger.GetLogger().Error("[LoopForWidgetHandle] ", e);
            }
        }
        private void ChromiumWebBrowserOnHandleCreated(object sender, EventArgs e)
        {
            _chromiumWebBrowserHandle = _chromiumWebBrowser.Handle;
        }
        #endregion

        #region Mouse/Keyboard action
        private void DoMouseMove(int x, int y)
        {
            if (!_chromiumWebBrowser.IsBrowserInitialized)
            {
                return;
            }
            _chromiumWebBrowser.GetBrowserHost().SendMouseMoveEvent(new MouseEvent(x,y, CefEventFlags.None), false );
        }

        private void DoMouseDown(int x, int y)
        {
            if (!_chromiumWebBrowser.IsBrowserInitialized)
            {
                return;
            }
            _chromiumWebBrowser.GetBrowserHost().SendMouseClickEvent(new MouseEvent(x, y, CefEventFlags.None),MouseButtonType.Left, false, 1);
        }

        private void DoMouseUp(int x, int y)
        {
            if (!_chromiumWebBrowser.IsBrowserInitialized)
            {
                return;
            }
            _chromiumWebBrowser.GetBrowserHost().SendMouseClickEvent(new MouseEvent(x, y, CefEventFlags.None), MouseButtonType.Left, true, 1);
        }

        private async Task DoMouseClick(int x, int y)
        {
            if (!_chromiumWebBrowser.IsBrowserInitialized)
            {
                return;
            }

            WindowsMouseApi.MouseDown(_browserWidgetWindow.Handle, x, y);

            await Task.Delay(5);

            WindowsMouseApi.MouseUp(_browserWidgetWindow.Handle, x, y);

            await Task.Delay(5);
        }

        private void DoKeyDown(int chr)
        {
            if (!_chromiumWebBrowser.IsBrowserInitialized)
            {
                return;
            }
            var keyEvent = new KeyEvent { WindowsKeyCode = chr, IsSystemKey = false, Type = KeyEventType.KeyDown };


            if (chr >= 96 && chr <= 105) keyEvent.Modifiers = CefEventFlags.NumLockOn | CefEventFlags.IsKeyPad;
            _chromiumWebBrowser.GetBrowserHost().SendKeyEvent(keyEvent);

        }

        private void DoKeyUp(int chr)
        {
            if (!_chromiumWebBrowser.IsBrowserInitialized)
            {
                return;
            }
            var keyEvent = new KeyEvent { WindowsKeyCode = chr, IsSystemKey = false, Type = KeyEventType.KeyUp, };


            if (chr >= 96 && chr <= 105) keyEvent.Modifiers = CefEventFlags.NumLockOn | CefEventFlags.IsKeyPad;

            _chromiumWebBrowser.GetBrowserHost().SendKeyEvent(keyEvent);
        }

        private void DoKeyClick(char chr)
        {
            var keyCode = (int)((Keys)char.ToUpper(chr));

            Log($"keyclick {chr}/{keyCode}");

            DoKeyDown(keyCode);
            Log("DoKeyDown done");
            DoKeyUp(keyCode);
            Log($"DoKeyUp done");
        }
        #endregion

        private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            _server.Stop();
        }
    }
}