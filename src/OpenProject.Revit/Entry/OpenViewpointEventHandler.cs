using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using OpenProject.Revit.Data;
using OpenProject.Revit.Extensions;
using OpenProject.Shared;
using OpenProject.Shared.ViewModels.Bcf;
using System;
using System.Collections.Generic;
using System.Linq;
using OpenProject.Shared.Math3D;
using OpenProject.Shared.Math3D.Enumeration;

namespace OpenProject.Revit.Entry
{
  /// <summary>
  /// Obfuscation Ignore for External Interface
  /// </summary>
  public class OpenViewpointEventHandler : IExternalEventHandler
  {
    private const decimal _viewpointAngleThresholdRad = 0.087266462599716m;

    /// <inheritdoc />
    public void Execute(UIApplication app)
    {
      ShowBcfViewpointInternal(app);
    }

    /// <inheritdoc />
    public string GetName() => nameof(OpenViewpointEventHandler);

    private BcfViewpointViewModel _bcfViewpoint;

    private static OpenViewpointEventHandler _instance;

    private static OpenViewpointEventHandler Instance
    {
      get
      {
        if (_instance != null) return _instance;

        _instance = new OpenViewpointEventHandler();
        ExternalEvent = ExternalEvent.Create(_instance);

        return _instance;
      }
    }

    private static ExternalEvent ExternalEvent { get; set; }

    /// <summary>
    /// Wraps the raising of the external event and thus the execution of the event callback,
    /// that show given bcf viewpoint.
    /// </summary>
    /// <param name="bcfViewpoint">The bcf viewpoint to be shown in current view.</param>
    public static void ShowBcfViewpoint(BcfViewpointViewModel bcfViewpoint)
    {
      Instance._bcfViewpoint = bcfViewpoint;
      ExternalEvent.Raise();
    }

    private void ShowBcfViewpointInternal(UIApplication app)
    {
      try
      {
        UIDocument uiDoc = app.ActiveUIDocument;
        Document doc = uiDoc.Document;

        var hasCamera = _bcfViewpoint.GetCamera().Match(
          camera => ShowOpenProjectView(app, camera),
          () => false);
        if (!hasCamera) return;

        DeselectAndUnhideElements(uiDoc);
        ApplyElementStyles(_bcfViewpoint, doc, uiDoc);
        ApplyClippingPlanes(_bcfViewpoint, uiDoc);

        // The local callback first needs to be initialized to null since it's
        // referencing itself in its body.
        // The reason for this is that we need to wait for Revit to load the view
        // and prepare everything. After that, we're waiting for the 'Idle' event
        // and instruct Revit to refresh and redraw the view. Otherwise, component
        // selection seemed not to work properly.
        void AfterIdleEventHandler(object o, IdlingEventArgs idlingEventArgs)
        {
          uiDoc.RefreshActiveView();
          app.ActiveUIDocument.UpdateAllOpenViews();
          app.Idling -= AfterIdleEventHandler;
        }

        app.Idling += AfterIdleEventHandler;
      }
      catch (Exception ex)
      {
        TaskDialog.Show("Error!", "exception: " + ex);
      }
    }

    private static bool ShowOpenProjectView(UIApplication app, Camera camera)
    {
      Document doc = app.ActiveUIDocument.Document;
      View3D openProjectView = doc.GetOpenProjectView(camera.Type);

      XYZ cameraViewPoint = RevitUtils.GetRevitXYZ(camera.Viewpoint);
      XYZ cameraDirection = RevitUtils.GetRevitXYZ(camera.Direction);
      XYZ cameraUpVector = RevitUtils.GetRevitXYZ(camera.UpVector);

      ViewOrientation3D orient3D =
        RevitUtils.ConvertBasePoint(doc, cameraViewPoint, cameraDirection, cameraUpVector, true);

      using var trans = new Transaction(doc);
      if (trans.Start("Apply view camera") == TransactionStatus.Started)
      {
        if (camera.Type == CameraType.Perspective)
        {
          Parameter farClip = openProjectView.get_Parameter(BuiltInParameter.VIEWER_BOUND_ACTIVE_FAR);
          if (farClip.HasValue) farClip.Set(0);
        }

        openProjectView.DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate);
        openProjectView.CropBoxActive = false;
        openProjectView.CropBoxVisible = false;
        openProjectView.SetOrientation(orient3D);
      }

      trans.Commit();

      app.ActiveUIDocument.ActiveView = openProjectView;

      if (camera.Type == CameraType.Orthogonal && camera is OrthogonalCamera orthoCam)
      {
        openProjectView.ToggleToIsometric();
        AppIdlingCallbackListener.SetPendingZoomChangedCallback(app, openProjectView.Id,
          orthoCam.ViewToWorldScale);
      }

      return true;
    }

    private static void DeselectAndUnhideElements(UIDocument uiDocument)
    {
      Document document = uiDocument.Document;

      using var transaction = new Transaction(uiDocument.Document);
      if (transaction.Start("Deselect selected and show hidden elements") == TransactionStatus.Started)
      {
        // This is to ensure no components are selected
        uiDocument.Selection.SetElementIds(new List<ElementId>());

        var hiddenRevitElements = new FilteredElementCollector(document)
          .WhereElementIsNotElementType()
          .WhereElementIsViewIndependent()
          .Where(e => e.IsHidden(document.ActiveView)) //might affect performance, but it's necessary
          .Select(e => e.Id)
          .ToList();

        if (hiddenRevitElements.Any())
        {
          // Resetting hidden elements to show all elements in the model
          document.ActiveView.UnhideElements(hiddenRevitElements);
        }
      }

      transaction.Commit();
    }

    private static void ApplyElementStyles(BcfViewpointViewModel bcfViewpoint, Document document, UIDocument uiDocument)
    {
      if (bcfViewpoint.Components?.Visibility == null)
        return;

      var visibleRevitElements = new FilteredElementCollector(document, document.ActiveView.Id)
        .WhereElementIsNotElementType()
        .WhereElementIsViewIndependent()
        .Where(e => e.CanBeHidden(document.ActiveView)) //might affect performance, but it's necessary
        .Select(e => e.Id)
        .ToList();

      // We're creating a dictionary of all the Revit internal Ids to be looked up by their IFC Guids
      // If this proves to be a performance issue, we should cache this dictionary in an instance variable
      var revitElementsByIfcGuid = new Dictionary<string, ElementId>();
      foreach (ElementId revitElement in visibleRevitElements)
      {
        var ifcGuid = IfcGuid.ToIfcGuid(ExportUtils.GetExportId(document, revitElement));
        if (!revitElementsByIfcGuid.ContainsKey(ifcGuid))
          revitElementsByIfcGuid.Add(ifcGuid, revitElement);
      }

      using var trans = new Transaction(uiDocument.Document);
      if (trans.Start("Apply BCF visibility and selection") == TransactionStatus.Started)
      {
        var exceptionElements = bcfViewpoint.Components.Visibility.Exceptions
          .Where(bcfComponentException => revitElementsByIfcGuid.ContainsKey(bcfComponentException.Ifc_guid))
          .Select(bcfComponentException => revitElementsByIfcGuid[bcfComponentException.Ifc_guid])
          .ToList();

        if (exceptionElements.Any())
          if (bcfViewpoint.Components.Visibility.Default_visibility)
            document.ActiveView.HideElementsTemporary(exceptionElements);
          else
            document.ActiveView.IsolateElementsTemporary(exceptionElements);

        if (bcfViewpoint.Components.Selection?.Any() ?? false)
        {
          var selectedElements = bcfViewpoint.Components.Selection
            .Where(selectedElement => revitElementsByIfcGuid.ContainsKey(selectedElement.Ifc_guid))
            .Select(selectedElement => revitElementsByIfcGuid[selectedElement.Ifc_guid])
            .ToList();

          if (selectedElements.Any())
            uiDocument.Selection.SetElementIds(selectedElements);
        }
      }

      trans.Commit();
    }

    private static void ApplyClippingPlanes(BcfViewpointViewModel bcfViewpoint, UIDocument uiDocument)
    {
      var clippingPlanes = bcfViewpoint.Viewpoint?.Clipping_planes;

      if (clippingPlanes == null || !clippingPlanes.Any() || uiDocument.ActiveView is not View3D view3d)
        return;

      AxisAlignedBoundingBox boundingBox = clippingPlanes
        .Select(p => p.ToAxisAlignedBoundingBox(_viewpointAngleThresholdRad))
        .Aggregate(AxisAlignedBoundingBox.Infinite, (current, nextBox) => current.MergeReduce(nextBox));

      using var trans = new Transaction(uiDocument.Document);
      if (!boundingBox.Equals(AxisAlignedBoundingBox.Infinite))
      {
        if (trans.Start("Apply BCF section box") == TransactionStatus.Started)
        {
          view3d.SetSectionBox(ToRevitSectionBox(boundingBox));
          view3d.IsSectionBoxActive = true;
        }
      }
      else
      {
        if (trans.Start("Disable section box") == TransactionStatus.Started)
          view3d.IsSectionBoxActive = false;
      }

      trans.Commit();
    }

    private static BoundingBoxXYZ ToRevitSectionBox(AxisAlignedBoundingBox box)
    {
      var min = new XYZ(
        box.Min.X == decimal.MinValue ? double.MinValue : ((double) box.Min.X).ToInternalRevitUnit(),
        box.Min.Y == decimal.MinValue ? double.MinValue : ((double) box.Min.Y).ToInternalRevitUnit(),
        box.Min.Z == decimal.MinValue ? double.MinValue : ((double) box.Min.Z).ToInternalRevitUnit());
      var max = new XYZ(
        box.Max.X == decimal.MaxValue ? double.MaxValue : ((double) box.Max.X).ToInternalRevitUnit(),
        box.Max.Y == decimal.MaxValue ? double.MaxValue : ((double) box.Max.Y).ToInternalRevitUnit(),
        box.Max.Z == decimal.MaxValue ? double.MaxValue : ((double) box.Max.Z).ToInternalRevitUnit());

      return new BoundingBoxXYZ { Min = min, Max = max };
    }
  }
}
