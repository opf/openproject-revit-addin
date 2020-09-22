using CefSharp;
using CefSharp.Wpf;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;

namespace OpenProject.WebViewIntegration
{
  public partial class BrowserManager
  {
    private readonly ChromiumWebBrowser _webBrowser;

    public BrowserManager(ChromiumWebBrowser webBrowser)
    {
      _webBrowser = webBrowser;
      JavaScriptBridge.Instance.SetWebBrowser(_webBrowser);

      _webBrowser.LoadingStateChanged += (s, e) =>
      {
        if (!e.IsLoading) // Not loading means the load is complete
        {
          Application.Current.Dispatcher.Invoke(async () =>
          {
            await InitializeRevitBridgeIfNotPresentAsync();
          });
        }
      };

      // Save all page changes, so that the user can resume to the same
      // page as last time she used the add-in.
      DependencyPropertyDescriptor
        .FromProperty(ChromiumWebBrowser.AddressProperty,
                      typeof(ChromiumWebBrowser))
        .AddValueChanged(_webBrowser, (s, e) =>
        {
          string newUrl = _webBrowser.Address;
          // Don't save local file addresses such as the settings page.
          if (newUrl.StartsWith("http"))
          {
            ConfigurationHandler.SaveLastVisitedPage(_webBrowser.Address);
          }
        });

      // This handles checking of valid urls, otherwise they're opened in
      // the system browser
      _webBrowser.RequestHandler = new OpenProjectBrowserRequestHandler();
      // This one prevents popups or additional browser windows
      _webBrowser.LifeSpanHandler = new OpenProjectBrowserLifeSpanHandler();

      if (ConfigurationHandler.ShouldEnableDevelopmentTools())
      {
        var devToolsEnabled = false;
        _webBrowser.IsBrowserInitializedChanged += (s, e) =>
        {
          if (!devToolsEnabled)
          {
            _webBrowser.ShowDevTools();
            devToolsEnabled = true;
          }
        };
      }
    }

    private async Task InitializeRevitBridgeIfNotPresentAsync()
    {
      var revitBridgeIsPresentCheckResponse = await _webBrowser.GetMainFrame().EvaluateScriptAsync("window." + JavaScriptBridge.REVIT_BRIDGE_JAVASCRIPT_NAME);
      if (revitBridgeIsPresentCheckResponse?.Result != null)
      {
        // No need to register the bridge since it's already bound
        return;
      }

      // Register the bridge between JS and C#
      // This also registers the callback that should be bound to by OpenProject to receive messages from BCFier
      _webBrowser.JavascriptObjectRepository.UnRegisterAll();
      _webBrowser.GetMainFrame().ExecuteJavaScriptAsync($"CefSharp.DeleteBoundObject('{JavaScriptBridge.REVIT_BRIDGE_JAVASCRIPT_NAME}');");
      _webBrowser.JavascriptObjectRepository.Register(JavaScriptBridge.REVIT_BRIDGE_JAVASCRIPT_NAME, new BcfierJavascriptInterop(), true);

      _webBrowser.GetMainFrame().ExecuteJavaScriptAsync(@"(async function(){
await CefSharp.BindObjectAsync(""" + JavaScriptBridge.REVIT_BRIDGE_JAVASCRIPT_NAME + @""", ""bound"");
window." + JavaScriptBridge.REVIT_BRIDGE_JAVASCRIPT_NAME + @".sendMessageToOpenProject = (message) => {console.log(JSON.parse(message))}; // This is the callback to be used by OpenProject for receiving messages
window.dispatchEvent(new Event('" + JavaScriptBridge.REVIT_READY_EVENT_NAME + @"'));
})();");
      // Now in JS, call this: openProjectBridge.messageFromOpenProject('Message from JS');
    }
  }
}
