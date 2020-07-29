using CefSharp;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace OpenProject.WebViewIntegration
{
  /// <summary>
  /// This class is used to check if a request is in the whitelist and can thus be
  /// opened in the CefSharp browser window. If this is not the case, the default system
  /// browser is used to open the page, e.g. for external links
  /// </summary>
  public class OpenProjectBrowserRequestHandler : IRequestHandler
  {
    public bool GetAuthCredentials(IWebBrowser chromiumWebBrowser, IBrowser browser, string originUrl, bool isProxy, string host, int port, string realm, string scheme, IAuthCallback callback)
    {
      return true;
    }

    public IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
    {
      return null;
    }

    public bool OnBeforeBrowse(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool userGesture, bool isRedirect)
    {
      var url = request.Url;

      if (request.ResourceType != ResourceType.MainFrame)
      {
        // We're allowing all embedded requests to just go through
        return false;
      }

      var isValidLocalUrl = url.StartsWith("file://", System.StringComparison.InvariantCultureIgnoreCase)
                            || url.StartsWith("devtools://", System.StringComparison.InvariantCultureIgnoreCase);
      if (isValidLocalUrl)
      {
        return false;
      }

      var knownGoodUrls = ConfigurationHandler.LoadAllInstances();
      var isValidExternalUrl = knownGoodUrls.Any(goodUrl => url.StartsWith(goodUrl, System.StringComparison.InvariantCultureIgnoreCase));

      if (isValidExternalUrl)
      {
        return false;
      }

      var cmdUrl = url.Replace("&", "^&");
      Process.Start(new ProcessStartInfo("cmd", $"/c start {cmdUrl}") { CreateNoWindow = true });

      return true;
    }

    public bool OnCertificateError(IWebBrowser chromiumWebBrowser, IBrowser browser, CefErrorCode errorCode, string requestUrl, ISslInfo sslInfo, IRequestCallback callback)
    {
      return true;
    }

    public bool OnOpenUrlFromTab(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, string targetUrl, WindowOpenDisposition targetDisposition, bool userGesture)
    {
      return true;
    }

    public void OnPluginCrashed(IWebBrowser chromiumWebBrowser, IBrowser browser, string pluginPath)
    {
    }

    public bool OnQuotaRequest(IWebBrowser chromiumWebBrowser, IBrowser browser, string originUrl, long newSize, IRequestCallback callback)
    {
      return true;
    }

    public void OnRenderProcessTerminated(IWebBrowser chromiumWebBrowser, IBrowser browser, CefTerminationStatus status)
    {
    }

    public void OnRenderViewReady(IWebBrowser chromiumWebBrowser, IBrowser browser)
    {
    }

    public bool OnSelectClientCertificate(IWebBrowser chromiumWebBrowser, IBrowser browser, bool isProxy, string host, int port, X509Certificate2Collection certificates, ISelectClientCertificateCallback callback)
    {
      return true;
    }
  }
}
