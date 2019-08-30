using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Browser
{
    public class WindowClassSearcher
    {
        public delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder buf, int nMaxCount);

        private string _className;

        public WindowClassSearcher(string className)
        {
            _className = className;
        }
        private bool EnumChildWindowsCallback(IntPtr handle, IntPtr lParam)
        {
            var stringBuilder = new StringBuilder(128);
            GetClassName(handle, stringBuilder, stringBuilder.Capacity);
            if (stringBuilder.ToString() == _className)
            {
                ((HandleHolder)GCHandle.FromIntPtr(lParam).Target).Handle = handle;
                return false;
            }

            return true;
        }

        public bool Search(IntPtr handle, out IntPtr browserHandle)
        {
            var holder = new HandleHolder();
            var value = GCHandle.Alloc(holder);
            EnumChildWindows(handle, EnumChildWindowsCallback, GCHandle.ToIntPtr(value));
            browserHandle = holder.Handle;
            value.Free();
            return holder.Handle != IntPtr.Zero;
        }

        private class HandleHolder
        {
            public IntPtr Handle { get; set; }
        }
    }
}
