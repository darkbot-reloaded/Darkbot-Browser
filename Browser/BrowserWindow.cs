using System;
using System.Windows.Forms;

namespace Browser
{
    public class BrowserWindow : NativeWindow
    {
        public bool BlockUserInput { get; set; }
        public BrowserWindow(Control browser, IntPtr handle)
        {
            AssignHandle(handle);
            browser.HandleDestroyed += BrowserOnHandleDestroyed;
        }

        private void BrowserOnHandleDestroyed(object sender, EventArgs e)
        {
            ReleaseHandle();
            ((Control) sender).HandleDestroyed -= BrowserOnHandleDestroyed;
        }


        protected override void WndProc(ref Message m)
        {
            var allow = OnInput(m);
            if (allow)
            {
                base.WndProc(ref m);
            }
        }

        private bool OnInput(Message message)
        {
            if (BlockUserInput)
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
    }

}
