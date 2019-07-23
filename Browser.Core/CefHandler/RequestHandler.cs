using System.Collections.Generic;
using CefSharp;
using CefSharp.Handler;

namespace Browser.Core.CefHandler
{
    public class RequestHandler : DefaultRequestHandler
    {
        public override CefReturnValue OnBeforeResourceLoad(IWebBrowser browserControl, IBrowser browser, IFrame frame,
            IRequest request, IRequestCallback callback)
        {
            var result = CefReturnValue.Continue;
            if (request != null && request.Url.ToLower().Contains("js_click")) result = CefReturnValue.Cancel;
            callback.Dispose();
            return result;
        }


        public override IResponseFilter GetResourceResponseFilter(IWebBrowser browserControl, IBrowser browser,
            IFrame frame, IRequest request, IResponse response)
        {
            if (request.Url.Contains("indexInternal.es?action=internalMapRevolution"))
            {
                var dictionary = new Dictionary<string, string>
                {
                    {"\"onFail\": onFailFlashembed", "\"onFail\": onFailFlashembed, \"quality\": \"low\""} //Set Quality to low
                };

                return new FindReplaceResponseFilter(dictionary);
            }

            return null;
        }
    }
}