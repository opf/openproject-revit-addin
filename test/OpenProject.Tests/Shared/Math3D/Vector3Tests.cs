using System;
using System.Collections.Generic;
using OpenProject.Shared.Math3D;
using Xunit;

namespace OpenProject.Tests.Shared.Math3D
{
  public class Vector3Tests
  {
    [Fact]
    public void InfiniteMax_ContainsOnlyMaximumValues()
    {
      // Act
      var max = Vector3.InfiniteMax;

      // Assert
      Assert.Equal(decimal.MaxValue, max.X, 28);
      Assert.Equal(decimal.MaxValue, max.Y, 28);
      Assert.Equal(decimal.MaxValue, max.Z, 28);
    }

    [Fact]
    public void InfiniteMin_ContainsOnlyMinimumValues()
    {
      // Act
      var min = Vector3.InfiniteMin;

      // Assert
      Assert.Equal(decimal.MinValue, min.X, 28);
      Assert.Equal(decimal.MinValue, min.Y, 28);
      Assert.Equal(decimal.MinValue, min.Z, 28);
    }

    public static IEnumerable<object[]> DataForDotProduct()
    {
      yield return new object[] { new Vector3(0, 0, 1), new Vector3(1, 0, 0), 0 };
      yield return new object[] { new Vector3(1, -2, 4), new Vector3(2, -2, -3), -6 };
      yield return new object[] { new Vector3(10.7m, 2.371m, -1), new Vector3(-4.62m, 6.012m, 0.1m), -35.279548m };
    }

    [Theory]
    [MemberData(nameof(DataForDotProduct))]
    public void Multiply_CalculatesCorrectDotProduct(Vector3 v1, Vector3 v2, decimal expectedResult)
    {
      // Act
      var result = v1 * v2;

      // Assert
      Assert.Equal(expectedResult, result, 20);
    }

    [Fact]
    public void AngleBetween_ThrowsForZeroVectors()
    {
      // Arrange
      var vec = new Vector3(1, 1, 1);
      var zero = new Vector3(0, 0, 0);

      // Act / Assert
      Assert.Throws<ArgumentException>(() => vec.AngleBetween(zero));
      Assert.Throws<ArgumentException>(() => zero.AngleBetween(vec));
    }

    public static IEnumerable<object[]> DataForAngleBetween()
    {
      yield return new object[] { new Vector3(0, 0, 1), new Vector3(1, 0, 0), 1.5707963267948966m };
      yield return new object[] { new Vector3(1, -2, 4), new Vector3(2, -2, -3), 1.8939448375294m };
    }

    [Theory]
    [MemberData(nameof(DataForAngleBetween))]
    public void AngleBetween_CalculatesTheCorrectAngleBetweenVectors(Vector3 v1, Vector3 v2, decimal expectedResult)
    {
      // Act
      var result = v1.AngleBetween(v2);

      // Assert
      Assert.Equal(expectedResult, result, 10);
    }
  }
}