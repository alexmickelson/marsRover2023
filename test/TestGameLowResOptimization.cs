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
        LowerLeftRow: 0,
        LowerLeftColumn: 0,
        UpperRightRow: 1,
        UpperRightColumn: 1,
        AverageDifficulty: 5
      ),
      new LowResolutionMap(
        LowerLeftRow: 0,
        LowerLeftColumn: 2,
        UpperRightRow: 0,
        UpperRightColumn: 3,
        AverageDifficulty: 5
      ),
      new LowResolutionMap(
        LowerLeftRow: 2,
        LowerLeftColumn: 0,
        UpperRightRow: 3,
        UpperRightColumn: 1,
        AverageDifficulty: 5
      ),
      new LowResolutionMap(
        LowerLeftRow: 2,
        LowerLeftColumn: 2,
        UpperRightRow: 4,
        UpperRightColumn: 4,
        AverageDifficulty: 5
      )
    };
    GamePlayer game = await Helpers.CreateNewGamePlayer(
      neighbors,
      (0, 0),
      (1, 1),
      lowResMap
    );

    game.OptimizeGrid();
    game.Map.OptimizedGrid.Count().Should().Be(4);
  }
}
