﻿using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using OpenProject.Shared;
using OpenProject.Shared.ViewModels.Bcf;
using System;
using System.Collections.Generic;
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
    public static BcfViewpointViewModel GenerateViewpoint(UIDocument uiDoc)
    {
      try
      {
        Document doc = uiDoc.Document;
        var bcfViewpoint = new BcfViewpointViewModel();

        //Corners of the active UI view
        XYZ bottomLeft = uiDoc.GetOpenUIViews()[0].GetZoomCorners()[0];
        XYZ topRight = uiDoc.GetOpenUIViews()[0].GetZoomCorners()[1];

        //It's a 3d view
        if (uiDoc.ActiveView.ViewType == ViewType.ThreeD)
        {
          var view3D = (View3D) uiDoc.ActiveView;

          // it is a orthogonal view
          if (!view3D.IsPerspective)
          {
            //center of the UI view
            var viewCenter = new XYZ((topRight.X + bottomLeft.X) / 2,
              (topRight.Y + bottomLeft.Y) / 2,
              (topRight.Z + bottomLeft.Z) / 2);

            var (viewBoxHeight, _) = RevitUtils.ConvertToViewBoxValues(topRight, bottomLeft, view3D.RightDirection);

            // **** CUSTOM VALUE FOR TEKLA **** //
            // calculated experimentally, not sure why but it works
            //if (UserSettings.Get("optTekla") == "1")
            //  zoomValue = zoomValue * 2.5;
            // **** CUSTOM VALUE FOR TEKLA **** //

            ViewOrientation3D t = RevitUtils.ConvertBasePoint(doc, viewCenter, uiDoc.ActiveView.ViewDirection,
              uiDoc.ActiveView.UpDirection, false);

            XYZ c = t.EyePosition;
            XYZ vi = t.ForwardDirection;
            XYZ up = t.UpDirection;

            bcfViewpoint.Viewpoint = new iabi.BCF.APIObjects.V21.Viewpoint_GET
            {
              Orthogonal_camera = new iabi.BCF.APIObjects.V21.Orthogonal_camera
              {
                View_to_world_scale = Convert.ToSingle(viewBoxHeight.ToMeters()),
                Camera_view_point =
                  new iabi.BCF.APIObjects.V21.Point
                  {
                    X = Convert.ToSingle(c.X.ToMeters()),
                    Y = Convert.ToSingle(c.Y.ToMeters()),
                    Z = Convert.ToSingle(c.Z.ToMeters())
                  },
                Camera_up_vector =
                  new iabi.BCF.APIObjects.V21.Direction
                  {
                    X = Convert.ToSingle(up.X), Y = Convert.ToSingle(up.Y), Z = Convert.ToSingle(up.Z)
                  },
                Camera_direction = new iabi.BCF.APIObjects.V21.Direction
                {
                  X = Convert.ToSingle(vi.X * -1),
                  Y = Convert.ToSingle(vi.Y * -1),
                  Z = Convert.ToSingle(vi.Z * -1)
                }
              }
            };
          }
          // it is a perspective view
          else
          {
            XYZ viewCenter = uiDoc.ActiveView.Origin;

            ViewOrientation3D t = RevitUtils.ConvertBasePoint(doc, viewCenter, uiDoc.ActiveView.ViewDirection,
              uiDoc.ActiveView.UpDirection, false);

            XYZ c = t.EyePosition;
            XYZ vi = t.ForwardDirection;
            XYZ up = t.UpDirection;

            bcfViewpoint.Viewpoint = new iabi.BCF.APIObjects.V21.Viewpoint_GET
            {
              Perspective_camera = new iabi.BCF.APIObjects.V21.Perspective_camera
              {
                Field_of_view = 45, //revit default value
                Camera_view_point =
                  new iabi.BCF.APIObjects.V21.Point
                  {
                    X = Convert.ToSingle(c.X.ToMeters()),
                    Y = Convert.ToSingle(c.Y.ToMeters()),
                    Z = Convert.ToSingle(c.Z.ToMeters())
                  },
                Camera_up_vector =
                  new iabi.BCF.APIObjects.V21.Direction
                  {
                    X = Convert.ToSingle(up.X), Y = Convert.ToSingle(up.Y), Z = Convert.ToSingle(up.Z)
                  },
                Camera_direction = new iabi.BCF.APIObjects.V21.Direction
                {
                  X = Convert.ToSingle(vi.X * -1), Y = Convert.ToSingle(vi.Y * -1), Z = Convert.ToSingle(vi.Z * -1)
                }
              }
            };
          }
        }

        SetViewpointComponents(bcfViewpoint, doc, uiDoc);
        SetViewpointClippingPlanes(bcfViewpoint, uiDoc);

        return bcfViewpoint;
      }
      catch (Exception ex1)
      {
        TaskDialog.Show("Error generating viewpoint", "exception: " + ex1);
      }

      return null;
    }

    private static void SetViewpointComponents(BcfViewpointViewModel bcfViewpoint, Document doc, UIDocument uiDoc)
    {
      var versionName = doc.Application.VersionName;

      var visibleElems = new FilteredElementCollector(doc, doc.ActiveView.Id)
        .WhereElementIsNotElementType()
        .WhereElementIsViewIndependent()
        .ToElementIds();
      var hiddenElems = new FilteredElementCollector(doc)
        .WhereElementIsNotElementType()
        .WhereElementIsViewIndependent()
        .Where(x => x.IsHidden(doc.ActiveView)
                    || !doc.ActiveView.IsElementVisibleInTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate,
                      x.Id)).Select(x => x.Id)
        .ToList(); //would need to check how much this is affecting performance

      var selectedElems = uiDoc.Selection.GetElementIds();

      bcfViewpoint.Components = new iabi.BCF.APIObjects.V21.Components();
      if (hiddenElems.Count > visibleElems.Count)
      {
        bcfViewpoint.Components.Visibility = new iabi.BCF.APIObjects.V21.Visibility
        {
          Default_visibility = false,
          Exceptions = visibleElems
            .Select(visibleComponent => new iabi.BCF.APIObjects.V21.Component
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
          Exceptions = hiddenElems
            .Select(hiddenComponent => new iabi.BCF.APIObjects.V21.Component
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
        bcfViewpoint.Components.Selection = new List<iabi.BCF.APIObjects.V21.Component>();
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
    }

    private static void SetViewpointClippingPlanes(BcfViewpointViewModel bcfViewpoint, UIDocument uiDoc)
    {
      // There are actually no clipping planes in a 3d view in revit, rather it's a
      // section box, meaning a set of 6 clipping planes that wrap the visible part
      // of a model inside a cuboid shape.
      if (uiDoc.ActiveView is not View3D { IsSectionBoxActive: true } view3d)
        return;

      bcfViewpoint.Viewpoint.Clipping_planes = new List<iabi.BCF.APIObjects.V21.Clipping_plane>();
      var revitSectionBox = view3d.GetSectionBox();

      // The Min and Max represent two corners of the section box on opposite
      // sides, so they're describing a cuboid 3D geometry, which we can now
      // use to derive the six actual planes from
      XYZ transformedMin = revitSectionBox.Transform.OfPoint(revitSectionBox.Min);
      XYZ transformedMax = revitSectionBox.Transform.OfPoint(revitSectionBox.Max);

      // A clipping plane is defined as a point and a direction, so we now
      // can use each point (Min and Max) three times, each with one direction in
      // X, Y and Z pointing outwards from the center
      // The vectors for the Min point should point away from the center (negative)
      // and the vectors for the Max point should be positive
      bcfViewpoint.Viewpoint.Clipping_planes = new[]
        {
          new
          {
            Location = transformedMin,
            Direction = revitSectionBox.Transform.OfVector(new XYZ(-1, 0, 0))
          },
          new
          {
            Location = transformedMin,
            Direction = revitSectionBox.Transform.OfVector(new XYZ(0, -1, 0))
          },
          new
          {
            Location = transformedMin,
            Direction = revitSectionBox.Transform.OfVector(new XYZ(0, 0, -1))
          },
          new
          {
            Location = transformedMax,
            Direction = revitSectionBox.Transform.OfVector(new XYZ(1, 0, 0))
          },
          new
          {
            Location = transformedMax,
            Direction = revitSectionBox.Transform.OfVector(new XYZ(0, 1, 0))
          },
          new
          {
            Location = transformedMax,
            Direction = revitSectionBox.Transform.OfVector(new XYZ(0, 0, 1))
          }
        }
        .Select(cp => new iabi.BCF.APIObjects.V21.Clipping_plane
        {
          Location = new iabi.BCF.APIObjects.V21.Location
          {
            X = Convert.ToSingle(cp.Location.X.ToMeters()),
            Y = Convert.ToSingle(cp.Location.Y.ToMeters()),
            Z = Convert.ToSingle(cp.Location.Z.ToMeters())
          },
          Direction = new iabi.BCF.APIObjects.V21.Direction
          {
            X = Convert.ToSingle(cp.Direction.X),
            Y = Convert.ToSingle(cp.Direction.Y),
            Z = Convert.ToSingle(cp.Direction.Z)
          }
        })
        .ToList();
    }
  }
}
