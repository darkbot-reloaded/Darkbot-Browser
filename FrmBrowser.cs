using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
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
using DarkBrowser.CefHandler;
using NamedPipeWrapper;

namespace DarkBrowser
{
    public partial class FrmBrowser : Form
    {
        private ChromiumWebBrowser _chromiumWebBrowser;
        private IntPtr _chromiumWebBrowserHandle;

        private BrowserWindow _browserWindow;
        private bool _blockUserInput = false;
        private Process _flashProcess;

        private NamedPipeClient<string> _client;

        public FrmBrowser()
        {
            InitializeComponent();
        }

        private void FrmBrowser_Load(object sender, EventArgs e)
        {
            _chromiumWebBrowser?.Dispose();

            var browserSettings = new BrowserSettings
            {
                WindowlessFrameRate = 30,
                Plugins = CefState.Enabled,
                WebGl = CefState.Enabled
            };

            _chromiumWebBrowser = new ChromiumWebBrowser("https://www.darkorbit.com/")
            {
                BrowserSettings = browserSettings,
                RequestContext = new RequestContext(new RequestContextHandler()),
                RequestHandler = new RequestHandler(),
                MenuHandler = new ContextMenuHandler(),
                
            };

            _chromiumWebBrowser.AddressChanged += ChromiumWebBrowserOnAddressChanged;
            _chromiumWebBrowser.IsBrowserInitializedChanged += ChromiumWebBrowserOnIsBrowserInitializedChanged;
            _chromiumWebBrowser.HandleCreated += ChromiumWebBrowserOnHandleCreated;
            _chromiumWebBrowser.Dock = DockStyle.Fill;
            panelBrowser.Controls.Add(_chromiumWebBrowser);

            Task.Run((Action) CreateConnection);
        }

        private void CreateConnection()
        {
            _client = new NamedPipeClient<string>("DarkBot");

            _client.ServerMessage += ClientOnServerMessage;

            Log("Starting client...");
            _client.Start();
            Log("Client started!");
        }

        private void ClientOnServerMessage(NamedPipeConnection<string, string> connection, string message)
        {
            Log($"Message from server: {message}");
            var parts = message.Split('|');

            if (parts[0] == "init")
            {
                _client.PushMessage($"PID|{_flashProcess.Id}");
                Log($"Sent flash pid {_flashProcess.Id}");
            }
            else if (parts[0] == "move")
            {
                var x = int.Parse(parts[1]);
                var y = int.Parse(parts[2]);
                DoMouseMove(x, y);
                Log($"Moved mouse to {x}/{y}");
            }
            else if (parts[0] == "click")
            {
                var x = int.Parse(parts[1]);
                var y = int.Parse(parts[2]);
                DoMouseClick(x, y);
                Log($"Clicked at {x}/{y}");
            }
            else if(parts[0] == "key")
            {
                var k = int.Parse(parts[1]);
                DoKeyboardClick(k);
                Log($"Pressed key {k}");
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
            }
            catch
            {
            }
        }

        private void ChromiumWebBrowserOnIsBrowserInitializedChanged(object sender, IsBrowserInitializedChangedEventArgs e)
        {
            if (e.IsBrowserInitialized)
            {
                Task.Run(new Action(LoopForHandle));
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
                _browserWindow = new BrowserWindow(_chromiumWebBrowser, intPtr, OnInput);
            }
            catch
            {
            }
        }

        private bool OnInput(Message message)
        {
            if (_blockUserInput)
            {
                int msg = message.Msg;
                if (msg <= 161)
                {
                    if (msg != 33 && msg != 161)
                    {
                        return true;
                    }
                }
                else if (msg - 512 > 10)
                {
                    if (msg != 526)
                    {
                        if (msg != 675)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            return true;
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
