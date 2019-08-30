using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CefSharp;

namespace Browser.Core
{
    public class Delegates
    {
        public delegate void MouseMove(int x, int y);
        public delegate void MouseDown(int x, int y);
        public delegate void MouseUp(int x, int y);
        public delegate Task MouseClick(int x, int y);
        public delegate void KeyClick(char chr);
        public delegate void Show();
        public delegate void Hide();
        public delegate void BlockUserInput(bool block);
        public delegate void Log(string message);

        public delegate void SetCookie(Cookie cookies, string server);
        public delegate void LoadUrl(string url);
        public delegate void Reload();
    }
}
