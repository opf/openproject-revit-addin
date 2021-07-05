using OpenProject.Shared.Math3D.Enumeration;

namespace OpenProject.Shared.Math3D
{
  /// <summary>
  /// A camera wrapper using decimal precision.
  /// </summary>
  public abstract class Camera
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
    /// No camera type (None) is considered invalid camera data.
    /// </summary>
    public CameraType Type { get; set; } = CameraType.None;
  }
}
