using System.Runtime.CompilerServices;
using OpenProject.Shared.Math3D.Enumeration;

namespace OpenProject.Shared.Math3D
{
  public sealed class OrthogonalCamera : Camera
  {
    /// <summary>
    /// The scale value from the view to the world system.
    /// </summary>
    public decimal ViewToWorldScale { get; set; }

    /// <inheritdoc />
    public override CameraType Type => CameraType.Orthogonal;

    /// <inheritdoc />
    public override bool Equals(Camera other)
    {
      return base.Equals(other) &&
             other is OrthogonalCamera ortho &&
             ViewToWorldScale == ortho.ViewToWorldScale;
    }
  }
}
