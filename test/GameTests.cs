public class GameTests
{
  [Test]
  public async Task CanCaluclatePath()
  {
    GamePlayer game = await createNewGamePlayer(
      new Neighbor[]
      {
        new Neighbor(1, 0, 5),
        new Neighbor(1, 1, 5)
      },
      (0, 0),
      (1, 1)
    );

    game.Path.Should().NotBeNull();
    game.Path.Should().NotBeEmpty();
    game.Path.First().Should().Be((1, 0));
  }

  [Test]
  public async Task CanCaluclatePath_2()
  {
    GamePlayer game = await createNewGamePlayer(
      new Neighbor[]
      {
        new Neighbor(0, 1, 2),
        new Neighbor(1, 1, 5)
      },
      (0, 0),
      (1, 1)
    );
    game.Path.First().Should().Be((0, 1));
  }

  [Test]
  public async Task CanCaluclatePath_3()
  {
    GamePlayer game = await createNewGamePlayer(
      new Neighbor[]
      {
        new Neighbor(0, 1, 2),
        new Neighbor(1, 1, 5)
      },
      (0, 1),
      (1, 1)
    );
    game.Path.First().Should().Be((1, 1));
  }

  [Test]
  public async Task CanCaluclatePath_4()
  {
    var grid = new int[][]
    {
      new int[] { 1, 2 },
      new int[] { 1, 2 },
      new int[] { 1, 2 },
    };
    Neighbor[] neighbors = gridToNeighbors(grid);
    GamePlayer game = await createNewGamePlayer(
      neighbors,
      (0, 0),
      (2, 0)
    );

    var expectedPath = new (int, int)[] { (1, 0), (2, 0) };
    game.Path.Should().BeEquivalentTo(expectedPath);
  }

  // [Test]
  // public async Task CanCaluclatePath_5()
  // {
  //   var grid = new int[][]
  //   {
  //     new int[] { 1, 2 },
  //     new int[] { 2, 10 },
  //     new int[] { 1, 1 },
  //   };
  //   Neighbor[] neighbors = gridToNeighbors(grid);
  //   GamePlayer game = await createNewGamePlayer(
  //     neighbors,
  //     (0, 0),
  //     (0, 2)
  //   );

  //   var expectedPath = new (int, int)[] { (0, 1), (0, 2) };
  //   game.Path.Should().BeEquivalentTo(expectedPath);
  // }

  [Test]
  public async Task CanCaluclatePath_6()
  {
    var grid = new int[][]
    {
      new int[] { 1, 2 },
      new int[] { 10, 1 },
      new int[] { 1, 1 },
    };
    Neighbor[] neighbors = gridToNeighbors(grid);
    GamePlayer game = await createNewGamePlayer(
      neighbors,
      (0, 0),
      (0, 2)
    );

    var expectedPath = new (int, int)[]
    {
      (0, 1),
      (1, 1),
      (2, 1),
      (2, 0)
    };
    game.Path.Should().BeEquivalentTo(expectedPath);
  }

  [Test]
  public void TestGridToNeighbors()
  {
    // index (row,column) which is (y,x)
    var grid = new int[][]
    {
      new int[] { 1, 2 },
      new int[] { 3, 4 },
    };
    Neighbor[] neighbors = gridToNeighbors(grid);

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

  private static Neighbor[] gridToNeighbors(int[][] grid)
  {
    var rowCount = grid.Count() - 1;
    var colCout = grid[0].Count() - 1;
    var neighbors = grid.SelectMany(
        (r, row) =>
          r.Select(
            (weight, column) =>
              new Neighbor(
                rowCount - row,
                column,
                weight
              )
          )
      )
      .ToArray();
    return neighbors;
  }

  private static async Task<GamePlayer> createNewGamePlayer(
    Neighbor[] cells,
    (int, int) start,
    (int, int) target
  )
  {
    IGameService mockService = createMockGameService(
      cells,
      start,
      target
    );
    var game = new GamePlayer(mockService);

    await game.Register("testGame");
    return game;
  }

  private static IGameService createMockGameService(
    Neighbor[] cells,
    (int, int) start,
    (int, int) target
  )
  {
    var averageDifficulty = Convert.ToInt32(
      cells.Average(c => c.Difficulty)
    );
    var gameResponse = new JoinResponse(
      Token: "sometoken",
      StartingRow: start.Item1,
      StartingColumn: start.Item2,
      TargetRow: target.Item1,
      TargetColumn: target.Item2,
      Neighbors: cells,
      new LowResolutionMap[]
      {
        new LowResolutionMap(
          LowerLeftRow: 0,
          LowerLeftColumn: 0,
          UpperRightRow: cells.Max(c => c.Row),
          UpperRightColumn: cells.Max(c => c.Column),
          AverageDifficulty: averageDifficulty + 10
        )
      },
      Orientation: Orientation.East
    );
    var gameServiceMock = new Mock<IGameService>();
    gameServiceMock
      .Setup(s => s.JoinGame())
      .ReturnsAsync(gameResponse);
    var mockService = gameServiceMock.Object;
    return mockService;
  }
}
