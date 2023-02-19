public class Helpers
{
  [Test]
  public void TestGridToNeighbors()
  {
    // index (row,column) which is (x,y)
    // row = x
    // col = y
    var grid = new int[][] { new int[] { 1, 2 }, new int[] { 3, 4 }, };
    Neighbor[] neighbors = GridToNeighbors(grid);

    var expectedNeighbors = new Neighbor[]
    {
      new Neighbor(0, 0, 3),
      new Neighbor(0, 1, 1),
      new Neighbor(1, 0, 4),
      new Neighbor(1, 1, 2)
    };
    Assert.That(neighbors, Is.EquivalentTo(expectedNeighbors));
  }

  public static Neighbor[] GridToNeighbors(int[][] grid)
  {
    var yCount = grid.Count() - 1;
    var neighbors = grid.SelectMany(
        (r, y) =>
          r.Select((weight, x) => new Neighbor(X: x, Y: yCount - y, weight))
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
    game.Rover.CalculateDetailedPath();
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
      StartingX: start.Item1,
      StartingY: start.Item2,
      TargetX: target.Item1,
      TargetY: target.Item2,
      Neighbors: cells,
      LowResolutionMap: lowResolutionMap != null
        ? lowResolutionMap
        : new LowResolutionMap[]
        {
          new LowResolutionMap(
            LowerLeftX: 0,
            LowerLeftY: 0,
            UpperRightX: cells.Max(c => c.X),
            UpperRightY: cells.Max(c => c.Y),
            AverageDifficulty: averageDifficulty + 10
          )
        },
      Orientation: "East"
    );
    var gameServiceMock = new Mock<IGameService>();
    gameServiceMock
      .Setup(s => s.JoinGame(It.IsAny<string>()))
      .ReturnsAsync(gameResponse);
    var mockService = gameServiceMock.Object;
    return mockService;
  }
}
