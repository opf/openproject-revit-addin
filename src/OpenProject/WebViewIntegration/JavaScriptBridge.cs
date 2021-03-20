using CefSharp;
using CefSharp.Wpf;
using Newtonsoft.Json;
using OpenProject.Shared;
using System.Threading.Tasks;
using System.Windows;

namespace OpenProject.WebViewIntegration
{
  public class JavaScriptBridge
  {
    /// <summary>
    /// This is the name of the global window object that's set in JavaScript, e.g.
    /// 'window.RevitBridge'.
    /// </summary>
    public const string REVIT_BRIDGE_JAVASCRIPT_NAME = "RevitBridge";

    public const string REVIT_READY_EVENT_NAME = "revit.plugin.ready";

    private JavaScriptBridge()
    {
      // This class should only be available as a singleton, access
      // via the static 'Instance' property.
    }

    private ChromiumWebBrowser _webBrowser;
    public static JavaScriptBridge Instance { get; } = new JavaScriptBridge();

    public event WebUIMessageReceivedEventHandler OnWebUIMessageReveived;

    public delegate void WebUIMessageReceivedEventHandler(object sender, WebUIMessageEventArgs e);

    public event AppForegroundRequestReceivedEventHandler OnAppForegroundRequestReceived;

    public delegate void AppForegroundRequestReceivedEventHandler(object sender);

    private void ChangeLoadingState(object sender, object eventArgs)
    {
      isLoaded = true;
    }

    public void SetWebBrowser(ChromiumWebBrowser webBrowser)
    {
      if (_webBrowser != null)
      {
        _webBrowser.LoadingStateChanged -= ChangeLoadingState;
      }

      _webBrowser = webBrowser;
      _webBrowser.LoadingStateChanged += ChangeLoadingState;
    }

    private bool isLoaded = false;

    public void SendMessageToRevit(string messageType, string trackingId, string messagePayload)
    {
      if (!isLoaded)
      {
        return;
      }

      if (messageType == MessageTypes.INSTANCE_SELECTED)
      {
        // This is the case at the beginning when the user selects which instance of OpenProject
        // should be accessed. We're not relaying this to Revit.
        HandleInstanceNameReceived(messagePayload);
      }
      else if (messageType == MessageTypes.ADD_INSTANCE)
      {
        // Simply save the instance to the white list and do nothing else.
        ConfigurationHandler.SaveSelectedInstance(messagePayload);
      }
      else if (messageType == MessageTypes.REMOVE_INSTANCE)
      {
        ConfigurationHandler.RemoveSavedInstance(messagePayload);
      }
      else if (messageType == MessageTypes.ALL_INSTANCES_REQUESTED)
      {
        var allInstances = JsonConvert.SerializeObject(ConfigurationHandler.LoadAllInstances());
        SendMessageToOpenProject(MessageTypes.ALL_INSTANCES, trackingId, allInstances);
      }
      else if (messageType == MessageTypes.FOCUS_REVIT_APPLICATION)
      {
        RevitMainWindowHandler.SetFocusToRevit();
      }
      else if (messageType == MessageTypes.GO_TO_SETTINGS)
      {
        VisitUrl(LandingIndexPageUrl());
      }
      else if (messageType == MessageTypes.SET_BROWSER_TO_FOREGROUND)
      {
        OnAppForegroundRequestReceived?.Invoke(this);
      }
      else if (messageType == MessageTypes.VALIDATE_INSTANCE)
      {
        Task.Run(async () => await ValidateInstanceAsync(trackingId, messagePayload));
      }
      else
      {
        var eventArgs = new WebUIMessageEventArgs(messageType, trackingId, messagePayload);
        OnWebUIMessageReveived?.Invoke(this, eventArgs);
        // For some UI operations, revit should be focused
        RevitMainWindowHandler.SetFocusToRevit();
      }

      // Hacky solution to directly send focus back to OP.
      if (messageType == MessageTypes.VIEWPOINT_DATA)
      {
        OnAppForegroundRequestReceived?.Invoke(this);
      }
    }

    public void SendMessageToOpenProject(string messageType, string trackingId, string messagePayload)
    {
      if (!isLoaded)
      {
        return;
      }

      if (messageType == MessageTypes.CLOSE_DESKTOP_APPLICATION)
      {
        // This message means we should exit the application
        System.Environment.Exit(0);
        return;
      }
      else if (messageType == MessageTypes.GO_TO_SETTINGS)
      {
        VisitUrl(LandingIndexPageUrl());
      }
      else if (messageType == MessageTypes.SET_BROWSER_TO_FOREGROUND)
      {
        OnAppForegroundRequestReceived?.Invoke(this);
      }
      else if (messageType == MessageTypes.VIEWPOINT_GENERATED)
      {
        OnAppForegroundRequestReceived?.Invoke(this);
      }

      var messageData = JsonConvert.SerializeObject(new { messageType, trackingId, messagePayload });
      var encodedMessage = JsonConvert.ToString(messageData);
      Application.Current.Dispatcher.Invoke(() =>
      {
        _webBrowser?.GetMainFrame()
          .ExecuteJavaScriptAsync($"{REVIT_BRIDGE_JAVASCRIPT_NAME}.sendMessageToOpenProject({encodedMessage})");
      });
    }

    private void VisitUrl(string url)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        _webBrowser.Address = url;
      });
    }

    private string LandingIndexPageUrl()
    {
      string url = EmbeddedLandingPageHandler.GetEmbeddedLandingPageIndexUrl();
      return url;
    }

    private void HandleInstanceNameReceived(string instanceName)
    {
      ConfigurationHandler.SaveSelectedInstance(instanceName);
      VisitUrl(instanceName);
    }

    private async Task ValidateInstanceAsync(string trackingId, string message)
    {
      var instanceValidationResult = await OpenProjectInstanceValidator
        .IsValidOpenProjectInstanceAsync(message);

      var frontendResult = new
      {
        instanceValidationResult.isValid,
        instanceValidationResult.instanceBaseUrl
      };

      SendMessageToOpenProject(MessageTypes.VALIDATED_INSTANCE,
        trackingId,
        JsonConvert.SerializeObject(frontendResult));
    }
  }
}
