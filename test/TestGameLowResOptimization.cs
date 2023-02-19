public class TestGameLowResOptimization
{
  [Test]
  public async Task CanEliminateUneededCells()
  {
    var grid = new int[][]
    {
      new int[] { 1, 1 },
      new int[] { 1, 1 },
    };
    Neighbor[] neighbors = Helpers.GridToNeighbors(grid);
    var lowResMap = new LowResolutionMap[]
    {
      new LowResolutionMap(
        LowerLeftX: 0,
        LowerLeftY: 0,
        UpperRightX: 1,
        UpperRightY: 1,
        AverageDifficulty: 5
      ),
      new LowResolutionMap(
        LowerLeftX: 0,
        LowerLeftY: 2,
        UpperRightX: 0,
        UpperRightY: 3,
        AverageDifficulty: 5
      ),
      new LowResolutionMap(
        LowerLeftX: 2,
        LowerLeftY: 0,
        UpperRightX: 3,
        UpperRightY: 1,
        AverageDifficulty: 5
      ),
      new LowResolutionMap(
        LowerLeftX: 2,
        LowerLeftY: 2,
        UpperRightX: 4,
        UpperRightY: 4,
        AverageDifficulty: 5
      )
    };
    GamePlayer game = await Helpers.CreateNewGamePlayer(
      neighbors,
      (0, 0),
      (1, 1),
      lowResMap
    );

    game.Rover.OptimizeGrid();
    game.Map.OptimizedGrid.Count().Should().Be(25);
  }
}
