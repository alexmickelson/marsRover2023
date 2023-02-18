public class PathTests
{
  [Test]
  public void CanGetNextNeighbors()
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
    var grid = new int[][]
    {
      new int[] { 1, 1 },
      new int[] { 1, 1 },
      new int[] { 1, 1 },
    };
    Neighbor[] neighbors = Helpers.GridToNeighbors(grid);

    var map = new MarsMap(lowResMap, neighbors);

    var expectedLocationNeighbors = new (int, int)[] { (1, 0), (2, 1), };

    var actualLocationNeigbors = MapPath.GetNeighbors((1, 1), (2, 0), (2, 1));
    CollectionAssert.AreEquivalent(
      expectedLocationNeighbors,
      actualLocationNeigbors
    );
  }
}
