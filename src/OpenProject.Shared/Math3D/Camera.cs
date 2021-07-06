using System;
using OpenProject.Shared.Math3D.Enumeration;

namespace OpenProject.Shared.Math3D
{
  /// <summary>
  /// A camera wrapper using decimal precision.
  /// </summary>
  public abstract class Camera : IEquatable<Camera>
  {
    /// <summary>
    /// The camera location, also named camera viewpoint.
    /// </summary>
    public Vector3 Viewpoint { get; set; } = new Vector3(0, 0, 0);

    /// <summary>
    /// The camera view direction.
    /// </summary>
    public Vector3 Direction { get; set; } = new Vector3(0, 0, 0);

    /// <summary>
    /// The camera up vector.
    /// </summary>
    public Vector3 UpVector { get; set; } = new Vector3(0, 0, 0);

    /// <summary>
    /// The camera type, which can be orthogonal or perspective.
    /// </summary>
    public abstract CameraType Type { get; }

    /// <inheritdoc />
    public virtual bool Equals(Camera other)
    {
      if (other == null) return false;

      return Viewpoint.Equals(other.Viewpoint) &&
             Direction.Equals(other.Direction) &&
             UpVector.Equals(other.UpVector) &&
             Type == other.Type;
    }
  }
}
