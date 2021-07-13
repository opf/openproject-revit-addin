using System;
using Autodesk.Revit.DB;
using OpenProject.Shared.Math3D;

namespace OpenProject.Revit.Data
{
  public static class RevitUtils
  {
    /// <summary>
    /// MOVES THE CAMERA ACCORDING TO THE PROJECT BASE LOCATION
    /// function that changes the coordinates accordingly to the project base location to an absolute location (for BCF export)
    /// if the value negative is set to true, does the opposite (for opening BCF views)
    /// </summary>
    /// <param name="doc">The Revit Document</param>
    /// <param name="c">center</param>
    /// <param name="view">view direction</param>
    /// <param name="up">up direction</param>
    /// <param name="negative">convert to/from</param>
    /// <returns></returns>
    public static ViewOrientation3D ConvertBasePoint(Document doc, XYZ c, XYZ view, XYZ up, bool negative)
    {
      // VERY IMPORTANT
      // `BuiltInParameter.BASEPOINT_EASTWEST_PARAM` is the value of the BASE POINT LOCATION.
      // `position` is the location of the BPL related to Revit's absolute origin.
      // If BPL is set to 0,0,0 not always it corresponds to Revit's origin.

      var origin = new XYZ(0, 0, 0);

      ProjectPosition position = doc.ActiveProjectLocation.GetProjectPosition(origin);

      var i = negative ? -1 : 1;

      var x = i * position.EastWest;
      var y = i * position.NorthSouth;
      var z = i * position.Elevation;
      var angle = i * position.Angle;

      if (negative) // I do the addition BEFORE
        c = new XYZ(c.X + x, c.Y + y, c.Z + z);

      //rotation
      var centX = c.X * Math.Cos(angle) - c.Y * Math.Sin(angle);
      var centY = c.X * Math.Sin(angle) + c.Y * Math.Cos(angle);

      XYZ newC = negative ? new XYZ(centX, centY, c.Z) : new XYZ(centX + x, centY + y, c.Z + z);

      var viewX = (view.X * Math.Cos(angle)) - (view.Y * Math.Sin(angle));
      var viewY = (view.X * Math.Sin(angle)) + (view.Y * Math.Cos(angle));
      var newView = new XYZ(viewX, viewY, view.Z);

      var upX = (up.X * Math.Cos(angle)) - (up.Y * Math.Sin(angle));
      var upY = (up.X * Math.Sin(angle)) + (up.Y * Math.Cos(angle));

      var newUp = new XYZ(upX, upY, up.Z);
      return new ViewOrientation3D(newC, newUp, newView);
    }

    /// <summary>
    /// Converts some basic revit view values to a view box height and a view box width.
    /// The revit views are defined by coordinates in project space.
    /// </summary>
    /// <param name="topRight">The top right corner of the revit view.</param>
    /// <param name="bottomLeft">The bottom left corner of the revit view.</param>
    /// <param name="right">The right direction of the revit view.</param>
    /// <returns>A tuple of the height and the width of the view box.</returns>
    public static ( double viewBoxHeight, double viewBoxWidth) ConvertToViewBoxValues(
      XYZ topRight, XYZ bottomLeft, XYZ right)
    {
      XYZ diagonal = topRight.Subtract(bottomLeft);
      var distance = topRight.DistanceTo(bottomLeft);
      var angleBetweenBottomAndDiagonal = diagonal.AngleTo(right);

      var height = distance * Math.Sin(angleBetweenBottomAndDiagonal);
      var width = distance * Math.Cos(angleBetweenBottomAndDiagonal);

      return (height, width);
    }

    public static XYZ GetRevitXYZ(Vector3 vec) =>
      new(Convert.ToDouble(vec.X).ToInternalRevitUnit(),
        Convert.ToDouble(vec.Y).ToInternalRevitUnit(),
        Convert.ToDouble(vec.Z).ToInternalRevitUnit());

    /// <summary>
    /// Converts feet units to meters. Feet are the internal Revit units.
    /// </summary>
    /// <param name="internalUnits">Value in internal Revit units to be converted to meters</param>
    /// <returns></returns>
    public static double ToMeters(this double internalUnits)
    {
#if Version2021 || Version2022
      return UnitUtils.ConvertFromInternalUnits(internalUnits, UnitTypeId.Meters);
#else
      return UnitUtils.ConvertFromInternalUnits(internalUnits, DisplayUnitType.DUT_METERS);
#endif
    }

    /// <summary>
    /// Converts meters units to feet. Feet are the internal Revit units.
    /// </summary>
    /// <param name="meters">Value in feet to be converted to feet</param>
    /// <returns></returns>
    public static double ToInternalRevitUnit(this double meters)
    {
#if Version2021 || Version2022
      return UnitUtils.ConvertToInternalUnits(meters, UnitTypeId.Meters);
#else
      return UnitUtils.ConvertToInternalUnits(meters, DisplayUnitType.DUT_METERS);
#endif
    }
  }
}
