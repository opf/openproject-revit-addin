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
using System.Numerics;

namespace OpenProject.Revit.Entry
{
  /// <summary>
  /// Obfuscation Ignore for External Interface
  /// </summary>
  public class OpenViewpointEventHandler : IExternalEventHandler
  {
    /// <summary>
    /// This is the method declared in the <see cref="IExternalEventHandler"/> interface
    /// provided by the Revit API
    /// </summary>
    /// <param name="app"></param>
    public void Execute(UIApplication app)
    {
      ShowBcfViewpointInternal(app);
    }

    public string GetName() => nameof(OpenViewpointEventHandler);

    private static int _viewSequence = 0;
    private BcfViewpointViewModel _bcfViewpoint;

    private static OpenViewpointEventHandler _instance;
    private static OpenViewpointEventHandler Instance {
      get
      {
        if (_instance == null)
        {
          _instance = new OpenViewpointEventHandler();
          ExternalEvent = ExternalEvent.Create(_instance);
        }

        return _instance;
      }
    }
    private static ExternalEvent ExternalEvent { get; set; }

    /// <summary>
    /// External Event Implementation
    /// </summary>
    /// <param name="app"></param>
    public static void ShowBcfViewpoint(UIApplication app, BcfViewpointViewModel bcfViewpoint)
    {
      Instance._bcfViewpoint = bcfViewpoint;
      ExternalEvent.Raise();
    }

    private void ShowBcfViewpointInternal(UIApplication app)
    {
      try
      {
        UIDocument uidoc = app.ActiveUIDocument;
        Document doc = uidoc.Document;

        // IS ORTHOGONAL
        if (_bcfViewpoint.Viewpoint?.Orthogonal_camera != null)
        {
          ShowOrthogonalView(_bcfViewpoint, doc, uidoc, app);
        }
        //perspective
        else if (_bcfViewpoint.Viewpoint?.Perspective_camera != null)
        {
          ShowPerspectiveView(_bcfViewpoint, doc, uidoc);
        }
        else
        {
          //no view included
          return;
        }

        ApplyElementStyles(_bcfViewpoint, doc, uidoc);

        if (!ApplyClippingPlanes(_bcfViewpoint, uidoc)
          && uidoc.ActiveView is View3D view3d)
        {
          using (var trans = new Transaction(uidoc.Document))
          {
            if (trans.Start("Disable section box") == TransactionStatus.Started)
            {
              view3d.IsSectionBoxActive = false;
            }

            trans.Commit();
          }
        }

        // The local callback first needs to be initialized to null since it's
        // referencing itself in its body.
        // The reason for this is that we need to wait for Revit to load the view
        // and prepare everything. After that, we're waiting for the 'Idle' event
        // and instruct Revit to refresh and redraw the view. Otherwise, component
        // selection seemed not to work properly.
        EventHandler<IdlingEventArgs> afterIdleEventHandler = null;
        afterIdleEventHandler = (_, _) =>
        {
            uidoc.RefreshActiveView();
            app.ActiveUIDocument.UpdateAllOpenViews();
            app.Idling -= afterIdleEventHandler;
        };

        app.Idling += afterIdleEventHandler;
      }
      catch (Exception ex)
      {
        TaskDialog.Show("Error!", "exception: " + ex);
      }
    }

    private void ShowOrthogonalView(BcfViewpointViewModel bcfViewpoint, Document doc, UIDocument uidoc, UIApplication app)
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
        IEnumerable<View3D> viewcollector3D = get3DViews(doc);
        if (viewcollector3D.Any(o => o.Name == "{3D}" || o.Name == "BCFortho"))
          orthoView = viewcollector3D.First(o => o.Name == "{3D}" || o.Name == "BCFortho");
      }
      using (var trans = new Transaction(uidoc.Document))
      {
        if (trans.Start("Open orthogonal view") == TransactionStatus.Started)
        {
          if (orthoView == null)
          {
            orthoView = View3D.CreateIsometric(doc, getFamilyViews(doc).First().Id);
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
      IEnumerable<View3D> viewcollector3D = get3DViews(doc);
      if (viewcollector3D.Any(o => o.Name == "BCFpersp"))
        perspView = viewcollector3D.First(o => o.Name == "BCFpersp");

      using (var trans = new Transaction(uidoc.Document))
      {
        if (trans.Start("Open perspective view") == TransactionStatus.Started)
        {
          if (null == perspView)
          {
            perspView = View3D.CreatePerspective(doc, getFamilyViews(doc).First().Id);
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

    private static bool ApplyClippingPlanes(BcfViewpointViewModel bcfViewpoint,
      UIDocument uiDocument)
    {
      if (!(bcfViewpoint.Viewpoint?.Clipping_planes?.Any() ?? false)
        || !(uiDocument.ActiveView is View3D view3d))
      {
        return false;
      }

      // We need exactly 6 clipping planes to create a section box in Revit,
      // since 6 planes form a cuboid to enclose the visible space
      var clippingPlanes = bcfViewpoint.Viewpoint.Clipping_planes;
      if (clippingPlanes.Count != 6)
      {
        return false;
      }

      // Now we need to get all the combinations of the planes to get the 8 intersection points
      var numericsPlanes = clippingPlanes
        .Select(cp =>
        {
          var numericsPlane = new System.Numerics.Plane();
          numericsPlane.Normal = Vector3.Normalize(new Vector3(cp.Direction.X,
            cp.Direction.Y,
            cp.Direction.Z));

          var locationX = cp.Location.X.ToInternalRevitUnit();
          var locationY = cp.Location.Y.ToInternalRevitUnit();
          var locationZ = cp.Location.Z.ToInternalRevitUnit();

          // See here for how to transform a plane via (location, normal)
          // to (distance to origin, normal)
          // https://stackoverflow.com/a/3863777/4190785
          // We're using 0,0,0 as the origin here to stay consistent with our export.
          // Also, other application will likely not use the Revit-specific project zero point
          // but just 0,0,0 in world coordinates
          var deltaVector = Vector3.Subtract(new Vector3(0,0,0), new Vector3(locationX,
            locationY,
            locationZ));
          var distanceToOrigin = Vector3.Dot(numericsPlane.Normal, deltaVector);
          numericsPlane.D = Convert.ToSingle(distanceToOrigin);
          return numericsPlane;
        })
        .ToList();

      // We're getting 120 combinations for the planes here
      var combinations = (from p0 in numericsPlanes
                          from p1 in numericsPlanes.Where(rp => rp != p0)
                          from p2 in numericsPlanes.Where(rp => rp != p0 && rp != p1)
                          where p0 != p1 && p1 != p2
                          select new { p0, p1, p2 })
                         .ToList();

      var intersectionPoints = new List<Vector3>();
      foreach (var combination in combinations)
      {
        if (PlanesHaveSingleIntersectionPoint(combination.p0,
          combination.p1,
          combination.p2,
          out var intersectionPoint)
          && !intersectionPoints.Any(ip => ip.X == intersectionPoint.X
            && ip.Y == intersectionPoint.Y
            && ip.Z == intersectionPoint.Z))
        {
          intersectionPoints.Add(intersectionPoint);
        }
      }

      // We should have 8 intersection points left for the 8 corners of the cuboid that
      // represents the bounding box
      if (intersectionPoints.Count != 8)
      {
        return false;
      }

      // Now we need to get the min and max - basically, two corners opposite to eachother between
      // which the bounding box is spanned. Everything outside of this cuboid is cut off and hidden
      // by the bounding box
      var boundingBox = new BoundingBoxXYZ();
      boundingBox.Max = new XYZ(
        x: intersectionPoints.Max(ip => ip.X),
        y: intersectionPoints.Max(ip => ip.Y),
        z: intersectionPoints.Max(ip => ip.Z));
      boundingBox.Min = new XYZ(
        x: intersectionPoints.Min(ip => ip.X),
        y: intersectionPoints.Min(ip => ip.Y),
        z: intersectionPoints.Min(ip => ip.Z));

      using (var trans = new Transaction(uiDocument.Document))
      {
        if (trans.Start("Apply BCF section box") == TransactionStatus.Started)
        {
          view3d.SetSectionBox(boundingBox);
          view3d.IsSectionBoxActive = true;
        }

        trans.Commit();
        return true;
      }
    }

    // Taken from https://gist.github.com/StagPoint/2eaa878f151555f9f96ae7190f80352e
    private static bool PlanesHaveSingleIntersectionPoint(System.Numerics.Plane p0,
      System.Numerics.Plane p1,
      System.Numerics.Plane p2,
      out Vector3 intersectionPoint)
    {
      const float EPSILON = 1e-4f;

      var det = Vector3.Dot(Vector3.Cross(p0.Normal, p1.Normal), p2.Normal);
      if (det < EPSILON)
      {
        intersectionPoint = Vector3.Zero;
        return false;
      }

      intersectionPoint =
        (-(p0.D * Vector3.Cross(p1.Normal, p2.Normal)) -
        (p1.D * Vector3.Cross(p2.Normal, p0.Normal)) -
        (p2.D * Vector3.Cross(p0.Normal, p1.Normal))) / det;

      return true;
    }

    private static IEnumerable<ViewFamilyType> getFamilyViews(Document doc)
    {
      return from elem in new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType))
             let type = elem as ViewFamilyType
             where type.ViewFamily == ViewFamily.ThreeDimensional
             select type;
    }

    private static IEnumerable<View3D> get3DViews(Document doc)
    {
      return from elem in new FilteredElementCollector(doc).OfClass(typeof(View3D))
             let view = elem as View3D
             select view;
    }
  }
}
