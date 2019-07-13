using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using CefSharp;
using CefSharp.Enums;
using CefSharp.OffScreen;
using CefSharp.Structs;

namespace Browser.CefHandler
{
    public class RenderHandler : IRenderHandler
    {
        public delegate void BrowserPaintHandler(Bitmap bitmap);
        public event BrowserPaintHandler BrowserPaint;

        public object BitMapLocker { get; set; } = new object();
        public bool Render { get; set; }

        private ChromiumWebBrowser _browser;

        private byte[] _buffer;
        private int _lastWidth;
        private int _lastHeight;

        protected virtual void OnBrowserPaint(Bitmap bitmap)
        {
            BrowserPaint?.Invoke(bitmap);
        }

        public RenderHandler(ChromiumWebBrowser browser)
        {
            _browser = browser;
            Render = true;

        }

        public void Dispose()
        {
            _browser = null;
        }

        public virtual ScreenInfo? GetScreenInfo()
        {
            var screenInfo = new ScreenInfo { DeviceScaleFactor = 1.0F };

            return screenInfo;
        }

        public virtual Rect GetViewRect()
        {
            var size = _browser.Size;

            var viewRect = new Rect(0, 0, size.Width, size.Height);

            return viewRect;
        }

        public virtual bool GetScreenPoint(int viewX, int viewY, out int screenX, out int screenY)
        {
            screenX = viewX;
            screenY = viewY;

            return false;
        }

        public virtual void OnAcceleratedPaint(PaintElementType type, Rect dirtyRect, IntPtr sharedHandle)
        {
            
        }

        private void ResizeBuffer(int width, int height)
        {
            if (_buffer == null || width != _lastWidth || height != _lastHeight)
            {
                var nob = width * height * 4;
                _buffer = new byte[nob];
                _lastWidth = width;
                _lastHeight = height;
            }
        }
        public virtual void OnPaint(PaintElementType type, Rect dirtyRect, IntPtr buffer, int width, int height)
        {
            if (Render && _browser.IsBrowserInitialized)
            {
                lock (BitMapLocker)
                {
                    ResizeBuffer(width, height);
                    Marshal.Copy(buffer, _buffer, 0, _buffer.Length);

                    var bitmap = new Bitmap(width, height, PixelFormat.Format32bppPArgb);

                    var bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);

                    Marshal.Copy(_buffer, 0, bitmapData.Scan0, _buffer.Length);

                    bitmap.UnlockBits(bitmapData);

                    OnBrowserPaint(bitmap);
                }
            }
        }

        public virtual void OnCursorChange(IntPtr cursor, CursorType type, CursorInfo customCursorInfo)
        {

        }

        public virtual bool StartDragging(IDragData dragData, DragOperationsMask mask, int x, int y)
        {
            return false;
        }

        public virtual void UpdateDragCursor(DragOperationsMask operation)
        {

        }

        public virtual void OnPopupShow(bool show)
        {
            
        }

        public virtual void OnPopupSize(Rect rect)
        {
        }

        public virtual void OnImeCompositionRangeChanged(Range selectedRange, Rect[] characterBounds)
        {

        }

        public virtual void OnVirtualKeyboardRequested(IBrowser browser, TextInputMode inputMode)
        {

        }
    }
}
