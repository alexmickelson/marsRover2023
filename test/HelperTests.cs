public class Helpers
{
  [Test]
  public void TestGridToNeighbors()
  {
    // index (row,column) which is (y,x)
    var grid = new int[][] { new int[] { 1, 2 }, new int[] { 3, 4 }, };
    Neighbor[] neighbors = GridToNeighbors(grid);

    var expectedNeighbors = new Neighbor[]
    {
      new Neighbor(0, 0, 3),
      new Neighbor(1, 0, 1),
      new Neighbor(0, 1, 4),
      new Neighbor(1, 1, 2)
    };
    // neighbors.Should().BeEquivalentTo(expectedNeighbors);
    Assert.That(neighbors, Is.EquivalentTo(expectedNeighbors));
  }

  public static Neighbor[] GridToNeighbors(int[][] grid)
  {
    var rowCount = grid.Count() - 1;
    var colCout = grid[0].Count() - 1;
    var neighbors = grid.SelectMany(
        (r, row) =>
          r.Select(
            (weight, column) => new Neighbor(rowCount - row, column, weight)
          )
      )
      .ToArray();
    return neighbors;
  }

  public static async Task<GamePlayer> CreateNewGamePlayer(
    Neighbor[] cells,
    (int, int) start,
    (int, int) target,
    LowResolutionMap[]? lowResolutionMap = null
  )
  {
    IGameService mockService = CreateMockGameService(
      cells,
      start,
      target,
      lowResolutionMap
    );
    var game = new GamePlayer(mockService);

    await game.Register("testGame");
    game.CalculatePath(game.Map.Grid, game.Target, game.Map.TopRight);
    return game;
  }

  public static IGameService CreateMockGameService(
    Neighbor[] cells,
    (int, int) start,
    (int, int) target,
    LowResolutionMap[]? lowResolutionMap = null
  )
  {
    var averageDifficulty = Convert.ToInt32(cells.Average(c => c.Difficulty));
    var gameResponse = new JoinResponse(
      Token: "sometoken",
      StartingRow: start.Item1,
      StartingColumn: start.Item2,
      TargetRow: target.Item1,
      TargetColumn: target.Item2,
      Neighbors: cells,
      LowResolutionMap: lowResolutionMap != null
        ? lowResolutionMap
        : new LowResolutionMap[]
        {
          new LowResolutionMap(
            LowerLeftRow: 0,
            LowerLeftColumn: 0,
            UpperRightRow: cells.Max(c => c.Row),
            UpperRightColumn: cells.Max(c => c.Column),
            AverageDifficulty: averageDifficulty + 10
          )
        },
      Orientation: "East"
    );
    var gameServiceMock = new Mock<IGameService>();
    gameServiceMock.Setup(s => s.JoinGame()).ReturnsAsync(gameResponse);
    var mockService = gameServiceMock.Object;
    return mockService;
  }
}
