using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using OpenProject.Revit.Data;
using OpenProject.Shared.ViewModels.Bcf;
using OpenProject.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using ZetaIpc.Runtime.Client;
using ZetaIpc.Runtime.Server;
using ZetaIpc.Runtime.Helper;

namespace OpenProject.Revit.Entry
{
  public class BcfierIpcHandler
  {
    private readonly UIApplication _uiApp;
    private Action<string> _sendData;
    private static readonly object _callbackStackLock = new();
    private static readonly Stack<Action> _callbackStack = new();

    public BcfierIpcHandler(UIApplication uiApp)
    {
      _uiApp = uiApp ?? throw new ArgumentNullException(nameof(uiApp));

      uiApp.Idling += (_, _) =>
      {
        lock (_callbackStackLock)
        {
          if (!_callbackStack.Any()) return;

          var action = _callbackStack.Pop();
          action.Invoke();
        }
      };
    }

    public int StartLocalServerAndReturnPort()
    {
      var freePort = FreePortHelper.GetFreePort();
      var server = new IpcServer();
      server.Start(freePort);
      server.ReceivedRequest += (_, e) =>
      {
        var eventArgs = JsonConvert.DeserializeObject<WebUIMessageEventArgs>(e.Request);
        var localMessageType = eventArgs.MessageType;
        var localTrackingId = eventArgs.TrackingId;
        var localMessagePayload = eventArgs.MessagePayload;
        _callbackStack.Push(() =>
        {
          switch (localMessageType)
          {
            case MessageTypes.VIEWPOINT_DATA:
            {
              var bcfViewpoint = MessageDeserializer.DeserializeBcfViewpoint(
                new WebUIMessageEventArgs(localMessageType, localTrackingId, localMessagePayload));
              OpenView(bcfViewpoint);
              break;
            }
            case MessageTypes.VIEWPOINT_GENERATION_REQUESTED:
              AddView(localTrackingId);
              break;
          }
        });
      };

      return freePort;
    }

    public void StartLocalClient(int bcfierWinServerPort)
    {
      var client = new IpcClient();
      client.Initialize(bcfierWinServerPort);
      _sendData = message =>
      {
        try
        {
          client.Send(message);
        }
        catch (System.Net.WebException)
        {
          // We can ignore the WebException, it's raised after
          // the shutdown event due to the other side just closing
          // the open TCP connection. This is what's expected😊
        }
      };
    }

    public void SendShutdownRequestToDesktopApp()
    {
      var eventArgs = new WebUIMessageEventArgs(MessageTypes.CLOSE_DESKTOP_APPLICATION, "0", string.Empty);
      var jsonEventArgs = JsonConvert.SerializeObject(eventArgs);
      _sendData(jsonEventArgs);
    }

    /// <summary>
    /// Raises the External Event to accomplish a transaction in a modeless window
    /// http://help.autodesk.com/view/RVT/2014/ENU/?guid=GUID-0A0D656E-5C44-49E8-A891-6C29F88E35C0
    /// http://matteocominetti.com/starting-a-transaction-from-an-external-application-running-outside-of-api-context-is-not-allowed/
    /// </summary>
    private void OpenView(BcfViewpointViewModel bcfViewpoint)
    {
      try
      {
        var uiDoc = _uiApp.ActiveUIDocument;

        if (uiDoc.ActiveView.ViewType == ViewType.Schedule)
        {
          MessageBox.Show("BCFier can't take snapshots of schedules.",
            "Warning!", MessageBoxButton.OK, MessageBoxImage.Warning);
          return;
        }

        OpenViewpointEventHandler.ShowBcfViewpoint(bcfViewpoint);
      }
      catch (Exception ex1)
      {
        TaskDialog.Show("Error opening a View!", "exception: " + ex1);
      }
    }

    public void SendOpenSettingsRequestToDesktopApp()
    {
      var eventArgs = new WebUIMessageEventArgs(MessageTypes.GO_TO_SETTINGS, "0", string.Empty);
      var jsonEventArgs = JsonConvert.SerializeObject(eventArgs);
      _sendData(jsonEventArgs);
    }

    public void SendBringBrowserToForegroundRequestToDesktopApp()
    {
      var eventArgs = new WebUIMessageEventArgs(MessageTypes.SET_BROWSER_TO_FOREGROUND, "0", string.Empty);
      var jsonEventArgs = JsonConvert.SerializeObject(eventArgs);
      _sendData(jsonEventArgs);
    }

    /// <summary>
    /// Same as in the windows app, but here we generate a VisInfo that is attached to the view
    /// </summary>
    /// <param name="trackingId">The local message tracking id.</param>
    private void AddView(string trackingId)
    {
      try
      {
        if (_uiApp.ActiveUIDocument.ActiveView.ViewType != ViewType.ThreeD)
        {
          TaskDialog.Show("Invalid active UI document",
            "To capture viewpoints the active document must be a 3D view.");
          return;
        }

        var generatedViewpoint = RevitView.GenerateViewpoint(_uiApp.ActiveUIDocument);
        var snapshot = GetRevitSnapshot(_uiApp.ActiveUIDocument.Document);
        var messageContent = new ViewpointGeneratedApiMessage
        {
          SnapshotPngBase64 = "data:image/png;base64," + ConvertToBase64(snapshot),
          Viewpoint = MessageSerializer.SerializeBcfViewpoint(generatedViewpoint)
        };

        var serializerSettings = new JsonSerializerSettings
        {
          ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        var jsonPayload =
          JObject.Parse(JsonConvert.SerializeObject(messageContent.Viewpoint.Viewpoint, serializerSettings));
        if (messageContent.Viewpoint.Components != null)
        {
          jsonPayload["components"] =
            JObject.Parse(JsonConvert.SerializeObject(messageContent.Viewpoint.Components, serializerSettings));
        }

        jsonPayload["snapshot"] = messageContent.SnapshotPngBase64;
        var payloadString = jsonPayload.ToString();

        var eventArgs = new WebUIMessageEventArgs(MessageTypes.VIEWPOINT_GENERATED, trackingId, payloadString);
        var jsonEventArgs = JsonConvert.SerializeObject(eventArgs);
        _sendData(jsonEventArgs);
      }
      catch (Exception exception)
      {
        TaskDialog.Show("Error adding a View!", "exception: " + exception);
      }
    }

    private static Stream GetRevitSnapshot(Document doc)
    {
      try
      {
        var tempPath = Path.Combine(Path.GetTempPath(), "BCFier");
        Directory.CreateDirectory(tempPath);
        var tempImg = Path.Combine(tempPath, Path.GetTempFileName() + ".png");
        var options = new ImageExportOptions
        {
          FilePath = tempImg,
          HLRandWFViewsFileType = ImageFileType.PNG,
          ShadowViewsFileType = ImageFileType.PNG,
          ExportRange = ExportRange.VisibleRegionOfCurrentView,
          ZoomType = ZoomFitType.FitToPage,
          ImageResolution = ImageResolution.DPI_72,
          PixelSize = 1000
        };
        doc.ExportImage(options);

        var memStream = new MemoryStream();
        using (var fs = File.OpenRead(tempImg))
        {
          fs.CopyTo(memStream);
        }

        File.Delete(tempImg);

        memStream.Position = 0;
        return memStream;
      }
      catch (Exception exception)
      {
        TaskDialog.Show("Error!", "exception: " + exception);
        throw;
      }
    }

    private static string ConvertToBase64(Stream stream)
    {
      using var memoryStream = new MemoryStream();
      stream.CopyTo(memoryStream);
      var bytes = memoryStream.ToArray();

      return Convert.ToBase64String(bytes);
    }
  }
}
