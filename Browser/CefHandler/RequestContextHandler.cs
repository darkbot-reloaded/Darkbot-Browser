using System;
using System.IO;
using CefSharp;

namespace Browser.CefHandler
{
    public class RequestContextHandler : IRequestContextHandler
    {
        public RequestContextHandler(CookieManager cookies)
        {
            _cookies = cookies;
        }

        private readonly CookieManager _cookies;
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
