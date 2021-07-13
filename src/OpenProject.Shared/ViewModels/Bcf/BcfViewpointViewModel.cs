using System;
using Dangl;
using iabi.BCF.APIObjects.V21;
using OpenProject.Shared.BcfApi;
using OpenProject.Shared.Math3D;
using Optional;

namespace OpenProject.Shared.ViewModels.Bcf
{
  /// <summary>
  /// A view model for BCF viewpoints.
  /// </summary>
  public sealed class BcfViewpointViewModel : BindableBase
  {
    public Viewpoint_GET Viewpoint { get; set; }

    public string SnapshotData { get; set; }

    public Components Components { get; set; }

    /// <summary>
    /// Gets the camera of the BCF viewpoint. It returns the value within an optional, which is None, if the BCF
    /// viewpoint has no camera set.
    /// </summary>
    /// <returns>The optional containing the camera.</returns>
    public Option<Camera> GetCamera()
    {
      Camera camera = null;

      if (Viewpoint?.Perspective_camera != null)
      {
        var c = new PerspectiveCamera();
        var bcfPerspective = Viewpoint.Perspective_camera;

        c.FieldOfView = Convert.ToDecimal(bcfPerspective.Field_of_view);
        c.Direction = bcfPerspective.Camera_direction.ToVector3();
        c.UpVector = bcfPerspective.Camera_up_vector.ToVector3();
        c.Viewpoint = bcfPerspective.Camera_view_point.ToVector3();

        camera = c;
      }

      if (Viewpoint?.Orthogonal_camera != null)
      {
        var c = new OrthogonalCamera();
        var bcfOrthogonal = Viewpoint.Orthogonal_camera;

        c.ViewToWorldScale = Convert.ToDecimal(bcfOrthogonal.View_to_world_scale);
        c.Direction = bcfOrthogonal.Camera_direction.ToVector3();
        c.UpVector = bcfOrthogonal.Camera_up_vector.ToVector3();
        c.Viewpoint = bcfOrthogonal.Camera_view_point.ToVector3();

        camera = c;
      }

      return camera.SomeNotNull();
    }
  }
}
