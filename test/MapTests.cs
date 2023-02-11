

public class MapTests
{
  [Test]
  public void CanCreateMap()
  {
    var map = new MarsMap();
    map.Should().NotBeNull();
  }

  [Test]
  public void CanCreateMapFromLowResolutionMap()
  {
    var lowResMap = new LowResolutionMap[]
    {
      new LowResolutionMap(
        LowerLeftRow: 0,
        LowerLeftColumn: 0,
        UpperRightRow: 1,
        UpperRightColumn: 1,
        AverageDifficulty: 0
      )
    };
    var map = new MarsMap(lowResMap);
    map.Grid[(0, 0)].Should().Be(0);

    map = new MarsMap(
      new LowResolutionMap[]
      {
        new LowResolutionMap(
          LowerLeftRow: 0,
          LowerLeftColumn: 0,
          UpperRightRow: 1,
          UpperRightColumn: 1,
          AverageDifficulty: 1
        )
      }
    );
    map.Grid[(0, 0)].Should().Be(1);

    map = new MarsMap(
      new LowResolutionMap[]
      {
        new LowResolutionMap(
          LowerLeftRow: 0,
          LowerLeftColumn: 0,
          UpperRightRow: 2,
          UpperRightColumn: 2,
          AverageDifficulty: 1
        )
      }
    );
    map.Grid[(2, 2)].Should().Be(1);
    map.Grid.Keys.Select(k => k.Item1).Max().Should().Be(2);
    map.Grid.Keys.Select(k => k.Item2).Max().Should().Be(2);
    map.Grid.Keys.Select(k => k.Item1).Min().Should().Be(0);
    map.Grid.Keys.Select(k => k.Item2).Min().Should().Be(0);
  }

  [Test]
  public void CanCreateMapWithMultipleRoughCells()
  {
    var map = new MarsMap(
      new LowResolutionMap[]
      {
        new LowResolutionMap(
          LowerLeftRow: 0,
          LowerLeftColumn: 0,
          UpperRightRow: 2,
          UpperRightColumn: 2,
          AverageDifficulty: 1
        ),
        new LowResolutionMap(
          LowerLeftRow: 3,
          LowerLeftColumn: 3,
          UpperRightRow: 5,
          UpperRightColumn: 6,
          AverageDifficulty: 6
        )
      }
    );
    map.Grid[(2, 2)].Should().Be(1);
    map.Grid[(5, 5)].Should().Be(6);

    map.Grid.Keys.Select(k => k.Item1).Max().Should().Be(5);
    map.Grid.Keys.Select(k => k.Item2).Max().Should().Be(6);
  }

  [Test]
  public void CanAddNeighborsToMap()
  {
    var lowResMap = new LowResolutionMap[]
    {
      new LowResolutionMap(
        LowerLeftRow: 0,
        LowerLeftColumn: 0,
        UpperRightRow: 2,
        UpperRightColumn: 2,
        AverageDifficulty: 1
      ),
      new LowResolutionMap(
        LowerLeftRow: 3,
        LowerLeftColumn: 3,
        UpperRightRow: 5,
        UpperRightColumn: 6,
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
        LowerLeftRow: 0,
        LowerLeftColumn: 0,
        UpperRightRow: 2,
        UpperRightColumn: 2,
        AverageDifficulty: 1
      ),
      new LowResolutionMap(
        LowerLeftRow: 3,
        LowerLeftColumn: 3,
        UpperRightRow: 5,
        UpperRightColumn: 6,
        AverageDifficulty: 6
      )
    };
    var neighbors = new Neighbor[] { new Neighbor(3, 3, 100) };

    var map = new MarsMap(lowResMap, neighbors);
    map.Grid[(0, 6)].Should().Be(0);
  }
}
