using iabi.BCF.APIObjects.V21;
using Newtonsoft.Json.Linq;
using OpenProject.Shared.ViewModels.Bcf;
using System;

namespace OpenProject.Shared
{
  public static class MessageDeserializer
  {
    public static BcfViewpointViewModel DeserializeBcfViewpoint(WebUIMessageEventArgs webUIMessage)
    {
      if (webUIMessage.MessageType != MessageTypes.VIEWPOINT_DATA)
      {
        throw new InvalidOperationException("Tried to deserialize a message with the wrong data type");
      }

      var jObject = JObject.Parse(webUIMessage.MessagePayload.Trim('"').Replace("\\\"", "\""));

      var bcfViewpoint = new BcfViewpointViewModel
      {
        Viewpoint = jObject.ToObject<Viewpoint_GET>(),
        SnapshotData = jObject["snapshot"]?["snapshot_data"]?.ToString(),
        Components = jObject["components"]?.ToObject<Components>()
      };

      return bcfViewpoint;
    }
  }
}
