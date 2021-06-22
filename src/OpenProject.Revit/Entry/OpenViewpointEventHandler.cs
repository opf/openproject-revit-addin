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
        var uiDoc = app.ActiveUIDocument;
        var doc = uiDoc.Document;

        if (_bcfViewpoint.Viewpoint?.Orthogonal_camera != null)
          ShowOrthogonalView(_bcfViewpoint, doc, uiDoc, app);
        else if (_bcfViewpoint.Viewpoint?.Perspective_camera != null)
          ShowPerspectiveView(_bcfViewpoint, doc, uiDoc);
        else
          return;

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

    private static void ShowOrthogonalView(BcfViewpointViewModel bcfViewpoint, Document doc, UIDocument uidoc,
      UIApplication app)
    {
      var orthogonalCamera = bcfViewpoint.Viewpoint?.Orthogonal_camera;
      if (orthogonalCamera == null)
      {
        return;
      }

      // TODO: Below, we're using the objects from the 'iabi.BCF' package. I'm not sure why, but it's
      // using float instead of a double for the 'View to World Scale' part. This should probably not
      // cause any problems, since the lower precision is likely enough for the task, but we might
      // want to think about maybe just fix the iabi.BCF package.
      var zoom = Convert.ToDouble(orthogonalCamera.View_to_world_scale).ToInternalRevitUnit();
      var cameraDirection = RevitUtils.GetRevitXYZ(orthogonalCamera.Camera_direction.X,
        orthogonalCamera.Camera_direction.Y,
        orthogonalCamera.Camera_direction.Z);
      var cameraUpVector = RevitUtils.GetRevitXYZ(orthogonalCamera.Camera_up_vector.X,
        orthogonalCamera.Camera_up_vector.Y,
        orthogonalCamera.Camera_up_vector.Z);
      var cameraViewPoint = RevitUtils.GetRevitXYZ(orthogonalCamera.Camera_view_point.X,
        orthogonalCamera.Camera_view_point.Y,
        orthogonalCamera.Camera_view_point.Z);
      var orient3D = RevitUtils.ConvertBasePoint(doc, cameraViewPoint, cameraDirection, cameraUpVector, true);

      View3D orthoView = null;
      //if active view is 3d ortho use it
      if (doc.ActiveView.ViewType == ViewType.ThreeD)
      {
        var activeView3D = doc.ActiveView as View3D;
        if (!activeView3D.IsPerspective)
          orthoView = activeView3D;
      }

      if (orthoView == null)
      {
        //try to use an existing 3D view
        IEnumerable<View3D> viewcollector3D = Get3dViews(doc);
        if (viewcollector3D.Any(o => o.Name == "{3D}" || o.Name == "BCFortho"))
          orthoView = viewcollector3D.First(o => o.Name == "{3D}" || o.Name == "BCFortho");
      }

      using (var trans = new Transaction(uidoc.Document))
      {
        if (trans.Start("Open orthogonal view") == TransactionStatus.Started)
        {
          if (orthoView == null)
          {
            orthoView = View3D.CreateIsometric(doc, GetFamilyViews(doc).First().Id);
            orthoView.Name = "BCFortho";
          }
          else
          {
            orthoView.DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate);
          }

          orthoView.SetOrientation(orient3D);
          trans.Commit();
        }
      }

      uidoc.ActiveView = orthoView;

      var viewChangedZoomListener = new ActiveViewChangedZoomListener(orthoView.Id.IntegerValue, zoom, app);
      viewChangedZoomListener.ListenForActiveViewChangeAndSetZoom();
    }

    private static void ShowPerspectiveView(BcfViewpointViewModel bcfViewpoint, Document doc, UIDocument uidoc)
    {
      var perspectiveCamera = bcfViewpoint.Viewpoint?.Perspective_camera;
      if (perspectiveCamera == null)
      {
        return;
      }

      bcfViewpoint.EnsurePerspectiveCameraVectorsAreOrthogonal();

      //not used since the fov cannot be changed in Revit
      // This is also a float, similar to the view to world scale
      var zoom = perspectiveCamera.Field_of_view;
      //FOV - not used
      //double z1 = 18 / Math.Tan(zoom / 2 * Math.PI / 180);
      //double z = 18 / Math.Tan(25 / 2 * Math.PI / 180);
      //double factor = z1 - z;

      var cameraDirection = RevitUtils.GetRevitXYZ(perspectiveCamera.Camera_direction.X,
        perspectiveCamera.Camera_direction.Y,
        perspectiveCamera.Camera_direction.Z);
      var cameraUpVector = RevitUtils.GetRevitXYZ(perspectiveCamera.Camera_up_vector.X,
        perspectiveCamera.Camera_up_vector.Y,
        perspectiveCamera.Camera_up_vector.Z);
      var cameraViewPoint = RevitUtils.GetRevitXYZ(perspectiveCamera.Camera_view_point.X,
        perspectiveCamera.Camera_view_point.Y,
        perspectiveCamera.Camera_view_point.Z);
      var orient3D = RevitUtils.ConvertBasePoint(doc, cameraViewPoint, cameraDirection, cameraUpVector, false);

      View3D perspView = null;
      //try to use an existing 3D view
      var viewcollector3D = Get3dViews(doc);
      if (viewcollector3D.Any(o => o.Name == "BCFpersp"))
        perspView = viewcollector3D.First(o => o.Name == "BCFpersp");

      using (var trans = new Transaction(uidoc.Document))
      {
        if (trans.Start("Open perspective view") == TransactionStatus.Started)
        {
          if (null == perspView)
          {
            perspView = View3D.CreatePerspective(doc, GetFamilyViews(doc).First().Id);
            perspView.Name = "BCFpersp";
          }
          else
          {
            //reusing an existing view, I net to reset the visibility
            //placed this here because if set afterwards it doesn't work
            perspView.DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate);
          }

          perspView.SetOrientation(orient3D);

          // turn off the far clip plane
          if (perspView.get_Parameter(BuiltInParameter.VIEWER_BOUND_ACTIVE_FAR).HasValue)
          {
            Parameter m_farClip = perspView.get_Parameter(BuiltInParameter.VIEWER_BOUND_ACTIVE_FAR);
            m_farClip.Set(0);
          }

          perspView.CropBoxActive = false;
          perspView.CropBoxVisible = false;

          trans.Commit();
        }
      }

      uidoc.ActiveView = perspView;
    }

    private static void ApplyElementStyles(BcfViewpointViewModel bcfViewpoint, Document document, UIDocument uiDocument)
    {
      using (var trans = new Transaction(uiDocument.Document))
      {
        if (trans.Start("Apply BCF visibility and selection") == TransactionStatus.Started)
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

        trans.Commit();
      }

      if (bcfViewpoint.Components?.Visibility == null)
      {
        return;
      }

      var visibleRevitElements = new FilteredElementCollector(document, document.ActiveView.Id)
        .WhereElementIsNotElementType()
        .WhereElementIsViewIndependent()
        .Where(e => e.CanBeHidden(document.ActiveView)) //might affect performance, but it's necessary
        .Select(e => e.Id)
        .ToList();

      // We're creating a dictionary of all the Revit internal Ids to be looked up by their IFC Guids
      // If this proves to be a performance issue, we should cache this dictionary in an instance variable
      var revitElementsByIfcGuid = new Dictionary<string, ElementId>();
      foreach (var revitElement in visibleRevitElements)
      {
        var ifcGuid = IfcGuid.ToIfcGuid(ExportUtils.GetExportId(document, revitElement));
        if (!revitElementsByIfcGuid.ContainsKey(ifcGuid))
        {
          revitElementsByIfcGuid.Add(ifcGuid, revitElement);
        }
      }

      using (var trans = new Transaction(uiDocument.Document))
      {
        if (trans.Start("Apply BCF visibility and selection") == TransactionStatus.Started)
        {
          if (bcfViewpoint.Components.Visibility.Default_visibility)
          {
            var hiddenElements = new List<ElementId>();
            foreach (var bcfComponentException in bcfViewpoint.Components.Visibility.Exceptions)
            {
              if (revitElementsByIfcGuid.ContainsKey(bcfComponentException.Ifc_guid))
              {
                hiddenElements.Add(revitElementsByIfcGuid[bcfComponentException.Ifc_guid]);
              }
            }

            if (hiddenElements.Any())
            {
              document.ActiveView.HideElementsTemporary(hiddenElements);
            }
          }
          else
          {
            var visibleElements = new List<ElementId>();
            foreach (var bcfComponentException in bcfViewpoint.Components.Visibility.Exceptions)
            {
              if (revitElementsByIfcGuid.ContainsKey(bcfComponentException.Ifc_guid))
              {
                visibleElements.Add(revitElementsByIfcGuid[bcfComponentException.Ifc_guid]);
              }
            }

            if (visibleElements.Any())
            {
              document.ActiveView.IsolateElementsTemporary(visibleElements);
            }
          }

          if (bcfViewpoint.Components.Selection?.Any() ?? false)
          {
            var selectedElements = new List<ElementId>();
            foreach (var selectedElement in bcfViewpoint.Components.Selection)
            {
              if (revitElementsByIfcGuid.ContainsKey(selectedElement.Ifc_guid))
              {
                selectedElements.Add(revitElementsByIfcGuid[selectedElement.Ifc_guid]);
              }
            }

            if (selectedElements.Any())
            {
              uiDocument.Selection.SetElementIds(selectedElements);
            }
          }
        }

        trans.Commit();
      }
    }

    private static void ApplyClippingPlanes(BcfViewpointViewModel bcfViewpoint, UIDocument uiDocument)
    {
      var clippingPlanes = bcfViewpoint.Viewpoint?.Clipping_planes;

      if (clippingPlanes == null || !clippingPlanes.Any() || uiDocument.ActiveView is not View3D view3d)
        return;

      var boxes = clippingPlanes
        .Select(p => p.ToAxisAlignedBoundingBox(_viewpointAngleThresholdRad));
      var boundingBox = boxes
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

    private static IEnumerable<ViewFamilyType> GetFamilyViews(Document doc)
    {
      return from elem in new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType))
        let type = elem as ViewFamilyType
        where type.ViewFamily == ViewFamily.ThreeDimensional
        select type;
    }

    private static IEnumerable<View3D> Get3dViews(Document doc)
    {
      return from elem in new FilteredElementCollector(doc).OfClass(typeof(View3D))
        let view = elem as View3D
        select view;
    }
  }
}