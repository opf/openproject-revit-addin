using System;
using iabi.BCF.APIObjects.V21;
using OpenProject.Shared.Math3D;

namespace OpenProject.Shared.BcfApi
{
  public static class BcfApiExtensions
  {
    public static Vector3 ToVector3(this Direction direction) =>
      new Vector3(
        Convert.ToDecimal(direction.X),
        Convert.ToDecimal(direction.Y),
        Convert.ToDecimal(direction.Z));

    public static Vector3 ToVector3(this Point point) =>
      new Vector3(
        Convert.ToDecimal(point.X),
        Convert.ToDecimal(point.Y),
        Convert.ToDecimal(point.Z));
  }
}
