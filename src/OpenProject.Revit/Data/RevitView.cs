using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using OpenProject.Shared;
using OpenProject.Shared.ViewModels.Bcf;
using System;
using System.Linq;

namespace OpenProject.Revit.Data
{
  //Methods for working with views
  public static class RevitView
  {
    //<summary>
    //Generate a VisualizationInfo of the current view
    //</summary>
    //<returns></returns>
    public static BcfViewpointViewModel GenerateViewpoint(UIDocument uidoc)
    {
      try
      {
        var doc = uidoc.Document;

        var bcfViewpoint = new BcfViewpointViewModel();

        //Corners of the active UI view
        var topLeft = uidoc.GetOpenUIViews()[0].GetZoomCorners()[0];
        var bottomRight = uidoc.GetOpenUIViews()[0].GetZoomCorners()[1];

        //It's a 3d view
        if (uidoc.ActiveView.ViewType == ViewType.ThreeD)
        {
          var viewCenter = new XYZ();
          var view3D = (View3D)uidoc.ActiveView;
          double zoomValue = 1;
          // it is a orthogonal view
          if (!view3D.IsPerspective)
          {
            double x = (topLeft.X + bottomRight.X) / 2;
            double y = (topLeft.Y + bottomRight.Y) / 2;
            double z = (topLeft.Z + bottomRight.Z) / 2;
            //center of the UI view
            viewCenter = new XYZ(x, y, z);

            //vector going from BR to TL
            XYZ diagVector = topLeft.Subtract(bottomRight);
            //length of the vector
            double dist = topLeft.DistanceTo(bottomRight);

            //ViewToWorldScale value
            zoomValue = (dist * Math.Sin(diagVector.AngleTo(view3D.RightDirection))).ToMeters();

            // **** CUSTOM VALUE FOR TEKLA **** //
            // calculated experimentally, not sure why but it works
            //if (UserSettings.Get("optTekla") == "1")
            //  zoomValue = zoomValue * 2.5;
            // **** CUSTOM VALUE FOR TEKLA **** //

            ViewOrientation3D t = RevitUtils.ConvertBasePoint(doc, viewCenter, uidoc.ActiveView.ViewDirection,
            uidoc.ActiveView.UpDirection, false);

            XYZ c = t.EyePosition;
            XYZ vi = t.ForwardDirection;
            XYZ up = t.UpDirection;

            bcfViewpoint.Viewpoint = new iabi.BCF.APIObjects.V21.Viewpoint_GET();
            bcfViewpoint.Viewpoint.Orthogonal_camera = new iabi.BCF.APIObjects.V21.Orthogonal_camera
            {
              View_to_world_scale = Convert.ToSingle(zoomValue),
              Camera_view_point = new iabi.BCF.APIObjects.V21.Point
              {
                X = Convert.ToSingle(c.X.ToMeters()),
                Y = Convert.ToSingle(c.Y.ToMeters()),
                Z = Convert.ToSingle(c.Z.ToMeters())
              },
              Camera_up_vector = new iabi.BCF.APIObjects.V21.Direction
              {
                X = Convert.ToSingle(up.X),
                Y = Convert.ToSingle(up.Y),
                Z = Convert.ToSingle(up.Z)
              },
              Camera_direction = new iabi.BCF.APIObjects.V21.Direction
              {
                X = Convert.ToSingle(vi.X * -1),
                Y = Convert.ToSingle(vi.Y * -1),
                Z = Convert.ToSingle(vi.Z * -1)
              }
            };
          }
          // it is a perspective view
          else
          {
            viewCenter = uidoc.ActiveView.Origin;
            //revit default value
            zoomValue = 45;

            ViewOrientation3D t = RevitUtils.ConvertBasePoint(doc, viewCenter, uidoc.ActiveView.ViewDirection,
             uidoc.ActiveView.UpDirection, false);

            XYZ c = t.EyePosition;
            XYZ vi = t.ForwardDirection;
            XYZ up = t.UpDirection;

            bcfViewpoint.Viewpoint = new iabi.BCF.APIObjects.V21.Viewpoint_GET();
            bcfViewpoint.Viewpoint.Perspective_camera = new iabi.BCF.APIObjects.V21.Perspective_camera
            {
              Field_of_view = Convert.ToSingle(zoomValue),
              Camera_view_point = new iabi.BCF.APIObjects.V21.Point
              {
                X = Convert.ToSingle(c.X.ToMeters()),
                Y = Convert.ToSingle(c.Y.ToMeters()),
                Z = Convert.ToSingle(c.Z.ToMeters())


              },
              Camera_up_vector = new iabi.BCF.APIObjects.V21.Direction
              {
                X = Convert.ToSingle(up.X),
                Y = Convert.ToSingle(up.Y),
                Z = Convert.ToSingle(up.Z)


              },
              Camera_direction = new iabi.BCF.APIObjects.V21.Direction
              {
                X = Convert.ToSingle(vi.X * -1),
                Y = Convert.ToSingle(vi.Y * -1),
                Z = Convert.ToSingle(vi.Z * -1)
              }
            };
          }
        }

        string versionName = doc.Application.VersionName;

        var visibleElems = new FilteredElementCollector(doc, doc.ActiveView.Id)
          .WhereElementIsNotElementType()
          .WhereElementIsViewIndependent()
        .ToElementIds();
        var hiddenElems = new FilteredElementCollector(doc)
          .WhereElementIsNotElementType()
          .WhereElementIsViewIndependent()
          .Where(x => x.IsHidden(doc.ActiveView)
            || !doc.ActiveView.IsElementVisibleInTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate, x.Id)).Select(x => x.Id)
            .ToList()
           ;//would need to check how much this is affecting performance

        var selectedElems = uidoc.Selection.GetElementIds();

        //TODO: create clipping planes

        bcfViewpoint.Components = new iabi.BCF.APIObjects.V21.Components();
        if (hiddenElems.Count > visibleElems.Count)
        {
          bcfViewpoint.Components.Visibility = new iabi.BCF.APIObjects.V21.Visibility
          {
            Default_visibility = false,
            Exceptions = visibleElems.Select(visibleComponent => new iabi.BCF.APIObjects.V21.Component
            {
              Originating_system = versionName,
              Ifc_guid = IfcGuid.ToIfcGuid(ExportUtils.GetExportId(doc, visibleComponent)),
              Authoring_tool_id = visibleComponent.IntegerValue.ToString()
            })
            .ToList()
          };
        }
        else
        {
          bcfViewpoint.Components.Visibility = new iabi.BCF.APIObjects.V21.Visibility
          {
            Default_visibility = true,
            Exceptions = hiddenElems.Select(hiddenComponent => new iabi.BCF.APIObjects.V21.Component
            {
              Originating_system = versionName,
              Ifc_guid = IfcGuid.ToIfcGuid(ExportUtils.GetExportId(doc, hiddenComponent)),
              Authoring_tool_id = hiddenComponent.IntegerValue.ToString()
            })
            .ToList()
          };
        }

        if (selectedElems.Any())
        {
          bcfViewpoint.Components.Selection = new System.Collections.Generic.List<iabi.BCF.APIObjects.V21.Component>();
          foreach (var selectedComponent in selectedElems)
          {
            bcfViewpoint.Components.Selection.Add(new iabi.BCF.APIObjects.V21.Component
            {
              Originating_system = versionName,
              Ifc_guid = IfcGuid.ToIfcGuid(ExportUtils.GetExportId(doc, selectedComponent)),
              Authoring_tool_id = selectedComponent.IntegerValue.ToString()
            });
          }
        }

        return bcfViewpoint;
      }
      catch (Exception ex1)
      {
        TaskDialog.Show("Error generating viewpoint", "exception: " + ex1);
      }
      return null;
    }
  }
}
