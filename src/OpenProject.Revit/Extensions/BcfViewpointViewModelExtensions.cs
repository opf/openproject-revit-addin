using System;
using OpenProject.Shared.BcfApi;
using OpenProject.Shared.Math3D;
using OpenProject.Shared.Math3D.Enumeration;
using OpenProject.Shared.ViewModels.Bcf;

namespace OpenProject.Revit.Extensions
{
  public static class BcfViewpointViewModelExtensions
  {
    public static Camera GetCamera(this BcfViewpointViewModel bcfViewpoint)
    {
      if (bcfViewpoint?.Viewpoint?.Perspective_camera != null)
      {
        var camera = new PerspectiveCamera();
        var bcfPerspective = bcfViewpoint.Viewpoint.Perspective_camera;

        camera.Type = CameraType.Perspective;
        camera.FieldOfView = Convert.ToDecimal(bcfPerspective.Field_of_view);
        camera.Direction = bcfPerspective.Camera_direction.ToVector3();
        camera.UpVector = bcfPerspective.Camera_up_vector.ToVector3();
        camera.Viewpoint = bcfPerspective.Camera_view_point.ToVector3();

        return camera;
      }

      if (bcfViewpoint?.Viewpoint?.Orthogonal_camera != null)
      {
        var camera = new OrthogonalCamera();
        var bcfOrthogonal = bcfViewpoint.Viewpoint.Orthogonal_camera;

        camera.Type = CameraType.Orthogonal;
        camera.ViewToWorldScale = Convert.ToDecimal(bcfOrthogonal.View_to_world_scale);
        camera.Direction = bcfOrthogonal.Camera_direction.ToVector3();
        camera.UpVector = bcfOrthogonal.Camera_up_vector.ToVector3();
        camera.Viewpoint = bcfOrthogonal.Camera_view_point.ToVector3();

        return camera;
      }

      return new OrthogonalCamera();
    }
  }
}
