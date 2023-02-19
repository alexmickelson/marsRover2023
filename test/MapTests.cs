public class MapTests
{
  [Test]
  public void CanAddNeighborsToMap()
  {
    var lowResMap = new LowResolutionMap[]
    {
      new LowResolutionMap(
        LowerLeftX: 0,
        LowerLeftY: 0,
        UpperRightX: 2,
        UpperRightY: 2,
        AverageDifficulty: 1
      ),
      new LowResolutionMap(
        LowerLeftX: 3,
        LowerLeftY: 3,
        UpperRightX: 5,
        UpperRightY: 6,
        AverageDifficulty: 6
      )
    };
    var neighbors = new Neighbor[] { new Neighbor(3, 3, 100) };

    var map = new MarsMap(lowResMap, neighbors);
    map.Grid[(3, 3)].Should().Be(100);
  }

  [Test]
  public void OtherValuesShouldBeInitializedToZero()
  {
    var lowResMap = new LowResolutionMap[]
    {
      new LowResolutionMap(
        LowerLeftX: 0,
        LowerLeftY: 0,
        UpperRightX: 2,
        UpperRightY: 2,
        AverageDifficulty: 1
      ),
      new LowResolutionMap(
        LowerLeftX: 3,
        LowerLeftY: 3,
        UpperRightX: 5,
        UpperRightY: 6,
        AverageDifficulty: 6
      )
    };
    var neighbors = new Neighbor[] { new Neighbor(3, 3, 100) };

    var map = new MarsMap(lowResMap, neighbors);
    map.Grid[(0, 6)].Should().Be(0);
  }
}
