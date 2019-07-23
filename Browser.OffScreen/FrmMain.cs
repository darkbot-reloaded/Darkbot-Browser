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
using Browser.OffScreen.CefHandler;
using CefSharp;
using CefSharp.OffScreen;

namespace Browser.OffScreen
{
    public partial class FrmMain : Form
    {
        private ChromiumWebBrowser _chromiumWebBrowser;
        private RenderHandler _renderHandler;
        private bool _blockUserInput;
        private bool _userOnMap;
        private CookieManager _cookies;

        private Thread _pipeThread;
        private NamedPipeServer _server;

        private PacketHandler _packetHandler;

        private bool _dragging;

        private int _sleepTime;
        private DateTime _nextFrame = DateTime.Now;

        public FrmMain()
        {
            InitializeComponent();
            KeyPreview = true;
            SetDoubleBuffering(pbBrowser, true);
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
                WebGl = CefState.Enabled,
            };

            _chromiumWebBrowser = new ChromiumWebBrowser("https://darkorbit.com", browserSettings, new RequestContext(new RequestContextHandler(_cookies)))
            {
                Size = pbBrowser.Size,
                RequestHandler = new RequestHandler(),
                MenuHandler = new ContextMenuHandler()
            };
            _renderHandler = new RenderHandler(_chromiumWebBrowser);
            _renderHandler.BrowserPaint += RenderHandlerOnBrowserPaint;

            _chromiumWebBrowser.RenderHandler = _renderHandler;

            _chromiumWebBrowser.AddressChanged += ChromiumWebBrowserOnAddressChanged;


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
                (block) => { _blockUserInput = block; }, Log, SetCookie, _chromiumWebBrowser.Load, _chromiumWebBrowser.Reload, _server.WriteMessage);

            _server.ClientMessage += _packetHandler.ServerOnClientMessage;
            _server.ClientConnected += delegate(object sender, EventArgs args) { Log($"client connected"); };
            _server.PipeClosed += delegate(object sender, EventArgs args) { _server.Close();  };
            _server.Start();
        }

        #region Chromium Browser
        private async void RenderHandlerOnBrowserPaint(Bitmap bitmap)
        {
            if (!pbBrowser.Disposing && !pbBrowser.IsDisposed && !Disposing && !IsDisposed)
            {
                if (DateTime.Now <= _nextFrame)
                {
                    await Task.Delay(_nextFrame.Subtract(DateTime.Now));
                    return;
                }
                lock (pbBrowser)
                {
                    try
                    {
                        Invoke(new Action(() =>
                        {
                            pbBrowser.Image?.Dispose();
                            pbBrowser.Image = null;
                        }));
                    }
                    catch (Exception)
                    {
                    }


                    pbBrowser.Image = bitmap;
                    _nextFrame = DateTime.Now.AddMilliseconds(_sleepTime);
                }
            }
        }

        private void SetCookie(Cookie cookie, string server)
        {
            _cookies.DeleteCookies($"https://www.{server}.darkorbit.com/", "dosid", null);
            _cookies.SetCookie($"https://www.{server}.darkorbit.com", cookie, null);
        }

        private void ChromiumWebBrowserOnAddressChanged(object sender, AddressChangedEventArgs e)
        {
            if (e.Address.Contains("/indexInternal.es?action=internalStart"))
            {
                var match = Regex.Match(e.Address, "https://(.*?).darkorbit");

                if (match.Success)
                    _chromiumWebBrowser.Load("https://" + match.Groups[1].Value + ".darkorbit.com/indexInternal.es?action=internalMapRevolution");
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

        #endregion

        #region Mouse/Keyboard actions
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

            _chromiumWebBrowser.GetBrowserHost().SendKeyEvent(keyEvent);
        }
        private void DoKeyClick(char chr)
        {
            var keyCode = (int)((Keys)char.ToUpper(chr));
            DoKeyDown(keyCode);
            DoKeyUp(keyCode);
        }
        #endregion

        #region PictureBox & Form events to perform clicks
        private void pbBrowser_MouseDown(object sender, MouseEventArgs e)
        {
            if (!_blockUserInput && e.Button == MouseButtons.Left)
            {
                DoMouseDown(e.X, e.Y);
            }

            _dragging = true;
        }
        private void pbBrowser_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_blockUserInput && _dragging)
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

            _dragging = false;
        }
        private void FrmMain_Resize(object sender, EventArgs e)
        {
            if (_chromiumWebBrowser != null)
            {
                _chromiumWebBrowser.Size = pbBrowser.Size;
            }
        }
        private void FrmMain_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (pbBrowser.Bounds.Contains(PointToClient(Cursor.Position)))
            {
                if (!_blockUserInput)
                {
                    DoKeyClick(e.KeyChar);
                }
            }
            e.Handled = true;
        }

        private void nudFps_ValueChanged(object sender, EventArgs e)
        {
            _sleepTime = 1000 / (int)nudFps.Value;
        }

        #endregion
    }
}