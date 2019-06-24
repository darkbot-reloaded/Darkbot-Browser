using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
using DarkBotBrowser.CefHandler;
using DarkBotBrowser.Communication.In;
using DarkBotBrowser.Communication.Out;
using NamedPipeWrapper;

namespace DarkBotBrowser
{
    public partial class FrmBrowser : Form
    {
        private ChromiumWebBrowser _chromiumWebBrowser;
        private IntPtr _chromiumWebBrowserHandle;
        private CookieManager _cookies;

        private BrowserWindow _browserWindow;
        private Process _flashProcess;

        private NamedPipeClient<string> _client;

        public FrmBrowser()
        {
            InitializeComponent();
        }

        private void FrmBrowser_Load(object sender, EventArgs e)
        {
            _chromiumWebBrowser?.Dispose();

            _cookies = new CookieManager(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", "cookies"), true, null);

            var browserSettings = new BrowserSettings
            {
                WindowlessFrameRate = 30,
                Plugins = CefState.Enabled,
                WebGl = CefState.Enabled
            };


            _chromiumWebBrowser = new ChromiumWebBrowser("https://darkorbit.com")
            {
                BrowserSettings = browserSettings,
                RequestContext = new RequestContext(new RequestContextHandler(_cookies)),
                RequestHandler = new RequestHandler(),
                MenuHandler = new ContextMenuHandler(),
                
            };

            _chromiumWebBrowser.AddressChanged += ChromiumWebBrowserOnAddressChanged;
            _chromiumWebBrowser.IsBrowserInitializedChanged += ChromiumWebBrowserOnIsBrowserInitializedChanged;
            _chromiumWebBrowser.HandleCreated += ChromiumWebBrowserOnHandleCreated;
            _chromiumWebBrowser.Dock = DockStyle.Fill;
            panelBrowser.Controls.Add(_chromiumWebBrowser);

            Task.Run((Action)CreatePipeConnection);
        }

        private void CreatePipeConnection()
        {
            _client = new NamedPipeClient<string>("DarkBot");

            _client.ServerMessage += ClientOnServerMessage;

            Log("Starting client...");
            _client.Start();
            Log("Client started!");
        }

        private void ClientOnServerMessage(NamedPipeConnection<string, string> connection, string message)
        {
            HandlePacket(message);
        }

        private void HandlePacket(string message)
        {
            var packet = new IncomingPacket(message);

            if (packet.Header == IncomingPacketIds.INIT)
            {
                SendMessage(PacketComposer.Compose(OutgoingPacketIds.FLASH_PID, _flashProcess.Id));
                Log($"Sent flash pid {_flashProcess.Id}");
            }
            else if (packet.Header == IncomingPacketIds.LOGIN)
            {
                var server = packet.Next;
                var sid = packet.Next;
                Log($"Received login: {server} {sid}");
                var cookie = new Cookie
                {
                    Name = "dosid",
                    Value = sid,
                    Domain = $"{server}.darkorbit.com",
                    Secure = true,
                    Creation = DateTime.Now
                };

                Log($"Setting cookie...");
                _cookies.DeleteCookies($"https://www.{server}.darkorbit.com/", "dosid", null);
                _cookies.SetCookie($"https://www.{server}.darkorbit.com", cookie, null);
                Log($"Redirecting browser...");
                _chromiumWebBrowser.Load($"https://www.{server}.darkorbit.com/indexInternal.es?action=internalStart");
            }
            else if (packet.Header == IncomingPacketIds.RELOAD)
            {
                Log("Received reload...");
                _chromiumWebBrowser.Reload(true);
            }
            else if (packet.Header == IncomingPacketIds.MOUSE)
            {
                var x = packet.NextInt;
                var y = packet.NextInt;
                if (packet.Next == IncomingPacketIds.CLICK)
                {
                    DoMouseClick(x, y);
                    Log($"Mouse clicked at {x}/{y}");
                }
                else if (packet.Next == IncomingPacketIds.MOVE)
                {
                    DoMouseMove(x, y);
                    Log($"Mouse moved to {x}/{y}");
                }
                else if (packet.Next == IncomingPacketIds.DOWN)
                {
                    DoMouseDown(x, y);
                    Log($"Mouse down at {x}/{y}");
                }
                else if (packet.Next == IncomingPacketIds.UP)
                {
                    DoMouseUp(x, y);
                    Log($"Mouse up at {x}/{y}");
                }
            }
            else if (packet.Header == IncomingPacketIds.KEY)
            {
                var k = packet.NextInt;
                if (packet.Next == IncomingPacketIds.CLICK)
                {
                    DoKeyboardClick(k);
                    Log($"Keyboard clicked {k}");
                }
                else if (packet.Next == IncomingPacketIds.DOWN)
                {
                    DoKeyboardDown(k);
                    Log($"Keyboard down {k}");
                }
                else if (packet.Next == IncomingPacketIds.UP)
                {
                   
                    DoKeyboardUp(k);
                    Log($"Keyboard up {k}");
                }
            }
            else if (packet.Header == IncomingPacketIds.BLOCK_INPUT)
            {
                _browserWindow.BlockUserInput = packet.NextBool;
                Log($"Blocked user input: {_browserWindow.BlockUserInput}");
            }
            else if (packet.Header == IncomingPacketIds.SHOW)
            {
                Show();
            }
            else if (packet.Header == IncomingPacketIds.HIDE)
            {
                Hide();
            }
            else
            {
                Log($"Received unknown packet... {packet}");
            }
        }

        private void SendMessage(string message)
        {
            _client.PushMessage(message);
        }

        private void ChromiumWebBrowserOnIsBrowserInitializedChanged(object sender, IsBrowserInitializedChangedEventArgs e)
        {
            if (e.IsBrowserInitialized)
            {
                Task.Run(new Action(LoopForHandle));
            }
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
                {
                    _chromiumWebBrowser.Load("https://" + match.Groups[1].Value + ".darkorbit.com/indexInternal.es?action=internalMapRevolution");
                }
            }
            else if (e.Address.Contains("/indexInternal.es?action=internalMapRevolution"))
            {
                Log("Map detected...");
                Task.Run(GetAndSendFlashProcessId);
            }
        }

        private async Task GetAndSendFlashProcessId()
        {
            Log("Trying to get flash process id...");
            try
            {
                Process proc = null;
                while ((proc = Process.GetCurrentProcess().GetFlashProcess()) == null)
                {
                    Log("Waiting 500ms...");
                    await Task.Delay(500);
                }
                Log("Got flash process: " + proc.Id);
                _flashProcess = proc;
                SendMessage(PacketComposer.Compose(OutgoingPacketIds.FLASH_PID, _flashProcess.Id));
                Log($"Sent flash pid {_flashProcess.Id}");
            }
            catch
            {
            }
        }

        private async void LoopForHandle()
        {
            try
            {
                IntPtr intPtr;
                while (!RenderWidgetHostHandleSearcher.Search(_chromiumWebBrowserHandle, out intPtr))
                {
                    await Task.Delay(10);
                }
                _browserWindow = new BrowserWindow(_chromiumWebBrowser, intPtr);
            }
            catch
            {
            }
        }

        private void DoMouseDown(int x, int y)
        {
            _chromiumWebBrowser.GetBrowserHost().SendMouseClickEvent(x, y, MouseButtonType.Left, false,1, CefEventFlags.None);
        }

        private void DoMouseUp(int x, int y)
        {
            _chromiumWebBrowser.GetBrowserHost().SendMouseClickEvent(x, y, MouseButtonType.Left, true, 1, CefEventFlags.None);
        }

        private void DoMouseClick(int x, int y)
        {
            DoMouseDown(x, y);
            DoMouseUp(x, y);
        }

        private void DoMouseMove(int x, int y)
        {
            _chromiumWebBrowser.GetBrowserHost().SendMouseMoveEvent(x, y, false, CefEventFlags.None);
        }

        private void DoKeyboardDown(int chr)
        {
            var keyEvent = new KeyEvent();
            keyEvent.WindowsKeyCode = chr;
            keyEvent.IsSystemKey = false;
            keyEvent.Type = KeyEventType.KeyDown;
            if (chr >= 96 && chr <= 105)
            {
                keyEvent.Modifiers = (CefEventFlags.NumLockOn | CefEventFlags.IsKeyPad);
            }
           _chromiumWebBrowser.GetBrowserHost().SendKeyEvent(keyEvent);
        }

        private void DoKeyboardUp(int chr)
        {
            var keyEvent = new KeyEvent();
            keyEvent.WindowsKeyCode = chr;
            keyEvent.IsSystemKey = false;
            keyEvent.Type = KeyEventType.KeyUp;
            if (chr >= 96 && chr <= 105)
            {
                keyEvent.Modifiers = (CefEventFlags.NumLockOn | CefEventFlags.IsKeyPad);
            }
            _chromiumWebBrowser.GetBrowserHost().SendKeyEvent(keyEvent);
        }

        private void DoKeyboardClick(int chr)
        {
            DoKeyboardDown(chr);
            DoKeyboardUp(chr);
        }

        public void Log(string text, Color color = new Color())
        {
            if (rtbLog.InvokeRequired)
            {
                rtbLog.BeginInvoke(new Action(delegate {
                    Log(text, color);
                }));
                return;
            }

            var nDateTime = DateTime.Now.ToString("hh:mm:ss tt") + " - ";
            
            rtbLog.SelectionStart = rtbLog.Text.Length;
            rtbLog.SelectionColor = color;
            
            if (rtbLog.Lines.Length == 0)
            {
                rtbLog.AppendText(nDateTime + text);
                rtbLog.ScrollToCaret();
                rtbLog.AppendText(System.Environment.NewLine);
            }
            else
            {
                rtbLog.AppendText(nDateTime + text + System.Environment.NewLine);
                rtbLog.ScrollToCaret();
            }
        }
    }
}
