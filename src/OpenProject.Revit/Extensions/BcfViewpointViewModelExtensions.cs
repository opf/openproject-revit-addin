using OpenProject.Shared.ViewModels.Bcf;

namespace OpenProject.Revit.Extensions
{
  public static class BcfViewpointViewModelExtensions
  {
    public static void EnsurePerspectiveCameraVectorsAreOrthogonal(this BcfViewpointViewModel bcfViewpointViewModel)
    {
      var perspectiveCamera = bcfViewpointViewModel?.Viewpoint?.Perspective_camera;
      if (perspectiveCamera == null)
      {
        return;
      }

      if (perspectiveCamera.Camera_direction.X != 0)
      {
        perspectiveCamera.Camera_up_vector.X = -1 * (perspectiveCamera.Camera_direction.Y * perspectiveCamera.Camera_up_vector.Y + perspectiveCamera.Camera_direction.Z * perspectiveCamera.Camera_up_vector.Z) / perspectiveCamera.Camera_direction.X;
      }
      else if (perspectiveCamera.Camera_direction.Y != 0)
      {
        perspectiveCamera.Camera_up_vector.Y = -1 * (perspectiveCamera.Camera_direction.X * perspectiveCamera.Camera_up_vector.X + perspectiveCamera.Camera_direction.Z * perspectiveCamera.Camera_up_vector.Z) / perspectiveCamera.Camera_direction.Y;
      }
      else if (perspectiveCamera.Camera_direction.Z != 0)
      {
        perspectiveCamera.Camera_up_vector.Z = -1 * (perspectiveCamera.Camera_direction.X * perspectiveCamera.Camera_up_vector.X + perspectiveCamera.Camera_direction.Y * perspectiveCamera.Camera_up_vector.Y) / perspectiveCamera.Camera_direction.Z;
      }
    }
  }
}
