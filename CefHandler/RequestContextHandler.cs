using System;
using System.IO;
using CefSharp;

namespace DarkBrowser.CefHandler
{
    public class RequestContextHandler : IRequestContextHandler
    {
        private readonly CookieManager _cookies = new CookieManager(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", "cookies"), true, null);
        public ICookieManager GetCookieManager()
        {
            return _cookies;
        }

        public bool OnBeforePluginLoad(string mimeType, string url, bool isMainFrame, string topOriginUrl, WebPluginInfo pluginInfo,
            ref PluginPolicy pluginPolicy)
        {
            var isFlash = pluginInfo.Name.ToLower().Contains("flash");
            if (isFlash)
            {
                pluginPolicy = PluginPolicy.Allow;
            }
            return isFlash;
        }

        public void OnRequestContextInitialized(IRequestContext requestContext)
        {
            
        }
    }
}
