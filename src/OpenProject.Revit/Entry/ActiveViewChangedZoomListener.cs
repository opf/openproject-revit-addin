using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using System;
using System.Linq;
using OpenProject.Revit.Data;

namespace OpenProject.Revit.Entry
{
  /// <summary>
  /// This class collects static functions that registers asynchronous callbacks to the idling state of the
  /// <see cref="Autodesk.Revit.UI.UIApplication"/>, which deregister themselves after execution.
  /// </summary>
  public static class AppIdlingCallbackListener
  {
    /// <summary>
    /// Sets a callback that applies a zoom to the current view.
    /// </summary>
    /// <param name="app">The current UI application.</param>
    /// <param name="viewId">The view ID for the view to be zoomed.</param>
    /// <param name="zoom">The zoom in decimal precision.</param>
    public static void SetPendingZoomChangedCallback(UIApplication app, ElementId viewId, decimal zoom)
    {
      void Callback(object sender, IdlingEventArgs args)
      {
        UIView currentView = app.ActiveUIDocument.GetOpenUIViews().First();
        if (currentView.ViewId != viewId) return;

        UIDocument uiDoc = app.ActiveUIDocument;
        View activeView = uiDoc.ActiveView;

        var zoomCorners = currentView.GetZoomCorners();
        XYZ bottomLeft = zoomCorners[0];
        XYZ topRight = zoomCorners[1];
        var (currentHeight, currentWidth) =
          RevitUtils.ConvertToViewBoxValues(topRight, bottomLeft, activeView.RightDirection);

        var zoomedViewBoxHeight = Convert.ToDouble(zoom).ToInternalRevitUnit();
        var zoomedViewBoxWidth = zoomedViewBoxHeight * currentWidth / currentHeight;

        XYZ newTopRight = activeView.Origin
          .Add(activeView.UpDirection.Multiply(zoomedViewBoxHeight / 2))
          .Add(activeView.RightDirection.Multiply(zoomedViewBoxWidth / 2));
        XYZ newBottomLeft = activeView.Origin
          .Subtract(activeView.UpDirection.Multiply(zoomedViewBoxHeight / 2))
          .Subtract(activeView.RightDirection.Multiply(zoomedViewBoxWidth / 2));

        currentView.ZoomAndCenterRectangle(newTopRight, newBottomLeft);
        app.Idling -= Callback;
      }

      app.Idling += Callback;
    }
  }
}
