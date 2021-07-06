using OpenProject.Shared.Math3D.Enumeration;

namespace OpenProject.Shared.Math3D
{
  public sealed class PerspectiveCamera : Camera
  {
    /// <summary>
    /// The field of view of the perspective camera.
    /// </summary>
    public decimal FieldOfView { get; set; }

    /// <inheritdoc />
    public override CameraType Type => CameraType.Perspective;

    /// <inheritdoc />
    public override bool Equals(Camera other)
    {
      return base.Equals(other) &&
             other is PerspectiveCamera pc &&
             FieldOfView == pc.FieldOfView;
    }
  }
}
