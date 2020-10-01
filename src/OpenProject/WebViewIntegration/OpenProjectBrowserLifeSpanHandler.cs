using CefSharp;

namespace OpenProject.WebViewIntegration
{
  /// <summary>
  /// This class is used to prevent popup windows, meaning that we prevent
  /// additional browser windows
  /// </summary>
  public class OpenProjectBrowserLifeSpanHandler : ILifeSpanHandler
  {
    public bool DoClose(IWebBrowser chromiumWebBrowser, IBrowser browser)
    {
      return true;
    }

    public void OnAfterCreated(IWebBrowser chromiumWebBrowser, IBrowser browser)
    {
    }

    public void OnBeforeClose(IWebBrowser chromiumWebBrowser, IBrowser browser)
    {
    }

    public bool OnBeforePopup(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, string targetUrl, string targetFrameName, WindowOpenDisposition targetDisposition, bool userGesture, IPopupFeatures popupFeatures, IWindowInfo windowInfo, IBrowserSettings browserSettings, ref bool noJavascriptAccess, out IWebBrowser newBrowser)
    {
      browser.MainFrame.LoadUrl(targetUrl);
      newBrowser = null;
      return true;
    }
  }
}
