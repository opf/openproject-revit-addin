using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using OpenProject.Shared.Math3D.Enumeration;

namespace OpenProject.Revit.Extensions
{
  public static class RevitExtensions
  {
    private const string _openProjectOrthogonalViewName = "OpenProject Orthogonal";
    private const string _openProjectPerspectiveViewName = "OpenProject Perspective";

    public static View3D GetOpenProjectView(this Document doc, CameraType type)
    {
      var viewName = type switch
      {
        CameraType.Orthogonal => _openProjectOrthogonalViewName,
        CameraType.Perspective => _openProjectPerspectiveViewName,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "invalid camera type")
      };

      View3D openProjectView = doc.Get3DViews().FirstOrDefault(view => view.Name == viewName);
      if (openProjectView != null) return openProjectView;

      using var trans = new Transaction(doc);
      trans.Start("Create open project view");

      openProjectView = type switch
      {
        CameraType.Orthogonal => View3D.CreateIsometric(doc, doc.GetFamilyViews().First().Id),
        CameraType.Perspective => View3D.CreatePerspective(doc, doc.GetFamilyViews().First().Id),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "invalid camera type")
      };

      openProjectView.Name = viewName;
      openProjectView.DetailLevel = ViewDetailLevel.Fine;
      openProjectView.DisplayStyle = DisplayStyle.Realistic;

      foreach (Category category in doc.Settings.Categories)
        if (category.CategoryType == CategoryType.Annotation && category.Name == "Levels")
          openProjectView.SetCategoryHidden(category.Id, true);

      trans.Commit();

      return openProjectView;
    }

    private static IEnumerable<ViewFamilyType> GetFamilyViews(this Document doc)
    {
      return from elem in new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType))
        let type = elem as ViewFamilyType
        where type.ViewFamily == ViewFamily.ThreeDimensional
        select type;
    }

    private static IEnumerable<View3D> Get3DViews(this Document doc)
    {
      return from elem in new FilteredElementCollector(doc).OfClass(typeof(View3D))
        let view = elem as View3D
        select view;
    }
  }
}
