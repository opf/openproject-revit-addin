using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using System;
using System.Linq;

namespace OpenProject.Revit.Entry
{
  /// <summary>
  /// This class listens to the App.Idling event in Revit an checks if a specific view is set.
  /// If the view is the active view, the zoom is set to display the model correctly.
  /// </summary>
  public class ActiveViewChangedZoomListener
  {
    private readonly int _orthoViewId;
    private readonly double _zoomValue;
    private readonly UIApplication _app;

    public ActiveViewChangedZoomListener(int orthoViewId, double zoomValue, UIApplication app)
    {
      _orthoViewId = orthoViewId;
      _zoomValue = zoomValue;
      _app = app;
    }

    public void ListenForActiveViewChangeAndSetZoom()
    {
      // Setting the zoom value happens after the actual view is displayed
      _app.Idling += SetZoomAndUnsubscribeIfMatchingViewId;
    }

    private void SetZoomAndUnsubscribeIfMatchingViewId(object sender, IdlingEventArgs eventArgs)
    {
      var currentView = _app.ActiveUIDocument.GetOpenUIViews().First();
      if (currentView.ViewId.IntegerValue == _orthoViewId)
      {
        var activeView = _app.ActiveUIDocument.ActiveView;
        var uiDoc = _app.ActiveUIDocument;
        //set UI view position and zoom
        XYZ m_xyzTopLeft = activeView.Origin.Add(activeView.UpDirection.Multiply(_zoomValue)).Subtract(activeView.RightDirection.Multiply(_zoomValue));
        XYZ m_xyzBottomRight = activeView.Origin.Subtract(activeView.UpDirection.Multiply(_zoomValue)).Add(activeView.RightDirection.Multiply(_zoomValue));
        uiDoc.GetOpenUIViews().First().ZoomAndCenterRectangle(m_xyzTopLeft, m_xyzBottomRight);
        _app.Idling -= SetZoomAndUnsubscribeIfMatchingViewId;
      }
    }
  }
}
