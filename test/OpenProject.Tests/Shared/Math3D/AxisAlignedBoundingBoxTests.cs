using System.Collections.Generic;
using OpenProject.Shared.Math3D;
using Xunit;

namespace OpenProject.Tests.Shared.Math3D
{
  public class AxisAlignedBoundingBoxTests
  {
    public static IEnumerable<object[]> DataForMergeReduce()
    {
      yield return new object[]
      {
        new AxisAlignedBoundingBox(new Vector3(0, 0, 0), new Vector3(2, 2, 2)),
        new AxisAlignedBoundingBox(new Vector3(1, 1, 1), new Vector3(3, 3, 3)),
        new AxisAlignedBoundingBox(new Vector3(1, 1, 1), new Vector3(2, 2, 2))
      };
      yield return new object[]
      {
        new AxisAlignedBoundingBox(new Vector3(0, 0, 0), new Vector3(2, 2, 2)),
        new AxisAlignedBoundingBox(new Vector3(-1, -1, -1), new Vector3(0, 0, 0)),
        AxisAlignedBoundingBox.Infinite
      };
      yield return new object[]
      {
        new AxisAlignedBoundingBox(new Vector3(0, 0, 0), new Vector3(2, 2, 2)),
        new AxisAlignedBoundingBox(new Vector3(-3, -3, -3), new Vector3(-1, -1, -1)),
        AxisAlignedBoundingBox.Infinite
      };
    }

    [Theory]
    [MemberData(nameof(DataForMergeReduce))]
    public void MergeReduce_MergesTwoBoundingBoxesAndReturnsIntersection(
      AxisAlignedBoundingBox aabb1, AxisAlignedBoundingBox aabb2, AxisAlignedBoundingBox expected)
    {
      // Act
      var result = aabb1.MergeReduce(aabb2);

      // Assert
      Assert.Equal(expected, result);
    }
  }
}