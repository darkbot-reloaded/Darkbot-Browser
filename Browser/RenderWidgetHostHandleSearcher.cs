using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Browser
{
    public class RenderWidgetHostHandleSearcher
    {
        public delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder buf, int nMaxCount);

        private static bool EnumChildWindowsCallback(IntPtr handle, IntPtr lParam)
        {
            var stringBuilder = new StringBuilder(128);
            GetClassName(handle, stringBuilder, stringBuilder.Capacity);
            if (stringBuilder.ToString() == "Chrome_RenderWidgetHostHWND")
            {
                ((HandleHolder) GCHandle.FromIntPtr(lParam).Target).Handle = handle;
                return false;
            }

            return true;
        }

        public static bool Search(IntPtr chromiumHandle, out IntPtr browserHandle)
        {
            var holder = new HandleHolder();
            var value = GCHandle.Alloc(holder);
            EnumChildWindows(chromiumHandle, EnumChildWindowsCallback, GCHandle.ToIntPtr(value));
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