using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
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
        private ChromiumWebBrowser _chromiumWebBrowser;
        private IntPtr _chromiumWebBrowserHandle;
        private CookieManager _cookies;

        private Thread _pipeThread;
        private NamedPipeServer _server;

        private PacketHandler _packetHandler;

        public FrmMain()
        {
            InitializeComponent();
            SetDoubleBuffering(pnlBrowserContainer, true);
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
                MenuHandler = new ContextMenuHandler()
            };

            _chromiumWebBrowser.AddressChanged += ChromiumWebBrowserOnAddressChanged;
            _chromiumWebBrowser.IsBrowserInitializedChanged += ChromiumWebBrowserOnIsBrowserInitializedChanged;
            _chromiumWebBrowser.HandleCreated += ChromiumWebBrowserOnHandleCreated;
            pnlBrowserContainer.Controls.Add(_chromiumWebBrowser);

            SetDoubleBuffering(_chromiumWebBrowser, true);

            _pipeThread = new Thread(CreatePipeConnection);
            _pipeThread.Start();
        }

        public void Log(string text)
        {
            if (rtbLog.InvokeRequired)
            {
                rtbLog.BeginInvoke(new Action(delegate
                {
                    Log(text);
                }));
                return;
            }

            var dt = DateTime.Now.ToString("hh:mm:ss.fff tt") + " - ";


            rtbLog.SelectionStart = rtbLog.Text.Length;
            rtbLog.SelectionColor = Color.Black;

            rtbLog.AppendText(dt + text + Environment.NewLine);
            rtbLog.ScrollToCaret();
        }

        private void SetDoubleBuffering(Control control, bool value)
        {
            var controlProperty = typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (controlProperty != null)
            {
                controlProperty.SetValue(control, value, null);
            }
        }

        private void CreatePipeConnection()
        {
            _server = new NamedPipeServer(Process.GetCurrentProcess().Id.ToString());

            _packetHandler = new PacketHandler(DoMouseMove, DoMouseDown, DoMouseUp, DoKeyClick, Show, Hide,
                (block) => { _browserWindow.BlockUserInput = block; }, Log, SetCookie, _chromiumWebBrowser.Load, _chromiumWebBrowser.Reload, _server.WriteMessage);

            _server.ClientMessage += _packetHandler.ServerOnClientMessage;
            _server.ClientConnected += delegate { Log($"client connected"); };
            _server.PipeClosed += delegate
            {
                Log("client disconnected");
                _server.Close();
            };
            _server.Start();
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
            if (e.IsBrowserInitialized) Task.Run(LoopForHandle);
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
            DoKeyUp(keyCode);
        }
        #endregion

    }
}