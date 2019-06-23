using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DarkBrowser
{
    public class BrowserWindow : NativeWindow
    {
        internal BrowserWindow(Control browser, IntPtr handle, Func<Message, bool> onInput)
        {
            AssignHandle(handle);
            browser.HandleDestroyed += BrowserOnHandleDestroyed;
            _onInput = onInput;
        }

        private void BrowserOnHandleDestroyed(object sender, EventArgs e)
        {
            ReleaseHandle();
            ((Control) sender).HandleDestroyed -= BrowserOnHandleDestroyed;
            _onInput = null;
        }


        protected override void WndProc(ref Message m)
        {
            var flag = true;
            if (_onInput != null)
            {
                flag = _onInput(m);
            }
            if (flag)
            {
                base.WndProc(ref m);
            }
        }
        
        private Func<Message, bool> _onInput;
    }

}
