using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Browser.Core
{
    public class WindowsMouseApi
    {
        public const uint WM_LBUTTONDOWN = 0x0201;
        public const uint WM_LBUTTONUP = 0x0202;

        [DllImport("user32.dll")]
        public static extern int SendMessage(int hWnd, uint Msg, int wParam, int lParam);

        public static void MouseDown(IntPtr handle, int x, int y)
        {
            var coordinates = x | (y << 16);
            SendMessage((int) handle, WM_LBUTTONDOWN, 0x1, coordinates);
        }

        public static void MouseUp(IntPtr handle, int x, int y)
        {
            var coordinates = x | (y << 16);
            SendMessage((int)handle, WM_LBUTTONUP, 0x1, coordinates);
        }
    }
}
