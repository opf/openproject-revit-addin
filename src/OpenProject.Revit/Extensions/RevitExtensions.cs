using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using OpenProject.Shared.Math3D.Enumeration;

namespace OpenProject.Revit.Extensions
{
  /// <summary>
  /// Extension written for handling of classes of the Revit API.
  /// </summary>
  public static class RevitExtensions
  {
    private const string _openProjectOrthogonalViewName = "OpenProject Orthogonal";
    private const string _openProjectPerspectiveViewName = "OpenProject Perspective";

    /// <summary>
    /// Gets the correct 3D view for displaying OpenProject content. The type of the view is dependent of the requested
    /// camera type, either orthogonal or perspective. If the view is not yet available, it is created.
    /// </summary>
    /// <param name="doc">The current revit document.</param>
    /// <param name="type">The camera type for the requested view.</param>
    /// <returns>A <see cref="View3D"/> with the correct settings to display OpenProject content.</returns>
    /// <exception cref="ArgumentOutOfRangeException"> Throws, if camera type is neither orthogonal nor perspective.</exception>
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
