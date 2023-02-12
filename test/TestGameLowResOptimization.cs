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
        UpperRightRow: 2,
        UpperRightColumn: 2,
        AverageDifficulty: 5
      ),
      new LowResolutionMap(
        LowerLeftRow: 0,
        LowerLeftColumn: 3,
        UpperRightRow: 2,
        UpperRightColumn: 5,
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
    game.Map.Grid.Count().Should().Be(4);
  }
}
