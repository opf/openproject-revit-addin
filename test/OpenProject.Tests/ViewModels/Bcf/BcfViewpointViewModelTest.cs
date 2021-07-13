using System.Collections.Generic;
using iabi.BCF.APIObjects.V21;
using OpenProject.Shared.Math3D;
using OpenProject.Shared.ViewModels.Bcf;
using Xunit;

namespace OpenProject.Tests.ViewModels.Bcf
{
  public class BcfViewpointViewModelTest
  {
    public static IEnumerable<object[]> GetCameraTestData()
    {
      // viewpoint with orthogonal camera
      yield return new object[]
      {
        new BcfViewpointViewModel
        {
          Viewpoint = new Viewpoint_GET
          {
            Orthogonal_camera = new Orthogonal_camera
            {
              Camera_direction = new Direction { X = 1, Y = 1, Z = 1 },
              Camera_up_vector = new Direction { X = 0, Y = 1, Z = -1 },
              Camera_view_point = new Point { X = -1, Y = 0, Z = 0 },
              View_to_world_scale = 42
            }
          }
        },
        new OrthogonalCamera
        {
          Direction = new Vector3(1, 1, 1),
          UpVector = new Vector3(0, 1, -1),
          Viewpoint = new Vector3(-1, 0, 0),
          ViewToWorldScale = 42
        }
      };

      // viewpoint with perspective camera
      yield return new object[]
      {
        new BcfViewpointViewModel
        {
          Viewpoint = new Viewpoint_GET
          {
            Perspective_camera = new Perspective_camera
            {
              Camera_direction = new Direction { X = 1, Y = 0, Z = 1 },
              Camera_up_vector = new Direction { X = 0, Y = -1, Z = -1 },
              Camera_view_point = new Point { X = 0, Y = -1, Z = 0 },
              Field_of_view = 92
            }
          }
        },
        new PerspectiveCamera
        {
          Direction = new Vector3(1, 0, 1),
          UpVector = new Vector3(0, -1, -1),
          Viewpoint = new Vector3(0, -1, 0),
          FieldOfView = 92
        }
      };

      // viewpoint without camera
      yield return new object[]
      {
        new BcfViewpointViewModel
          { Viewpoint = new Viewpoint_GET { Orthogonal_camera = null, Perspective_camera = null } },
        null
      };
    }

    [Theory]
    [MemberData(nameof(GetCameraTestData))]
    public void GetCamera_ReturnsExpectedCameraForGivenViewpoint(BcfViewpointViewModel bcfViewpoint, Camera camera)
    {
      // Act / Assert
      bcfViewpoint.GetCamera().Match(
        c => Assert.Equal(camera, c),
        () => Assert.Null(camera));
    }
  }
}
