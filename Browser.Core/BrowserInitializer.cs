using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CefSharp;

namespace Browser.Core
{
    public class BrowserInitializer
    {
        public static readonly string PATH_RESOURCES = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");
        public static readonly string PATH_CEF = Path.Combine(PATH_RESOURCES, "Cef");
        public static readonly string PATH_LIB = Path.Combine(PATH_RESOURCES, "Libs");

        public static bool InitStage1()
        {
            var libraryLoader = new CefLibraryHandle(Path.Combine(PATH_CEF, "libcef.dll"));
            libraryLoader.Dispose();
            return !libraryLoader.IsInvalid;
        }
    }
}
