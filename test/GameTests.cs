public class GameTests
{
  [Test]
  public async Task CanCaluclatePath()
  {
    GamePlayer game = await Helpers.CreateNewGamePlayer(
      new Neighbor[] { new Neighbor(1, 0, 5), new Neighbor(1, 1, 5) },
      (0, 0),
      (1, 1)
    );

    game.Path.Should().NotBeNull();
    game.Path.Should().NotBeEmpty();
    game.Path.ElementAt(1).Should().Be((1, 0));
  }

  [Test]
  public async Task CanCaluclatePath_2()
  {
    GamePlayer game = await Helpers.CreateNewGamePlayer(
      new Neighbor[] { new Neighbor(0, 1, 2), new Neighbor(1, 1, 5) },
      (0, 0),
      (1, 1)
    );
    game.Path.ElementAt(1).Should().Be((0, 1));
  }

  [Test]
  public async Task CanCaluclatePath_3()
  {
    GamePlayer game = await Helpers.CreateNewGamePlayer(
      new Neighbor[] { new Neighbor(0, 1, 2), new Neighbor(1, 1, 5) },
      (0, 1),
      (1, 1)
    );
    game.Path.ElementAt(1).Should().Be((1, 1));
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
    Neighbor[] neighbors = Helpers.GridToNeighbors(grid);
    GamePlayer game = await Helpers.CreateNewGamePlayer(
      neighbors,
      (0, 0),
      (0, 2)
    );

    var expectedPath = new (int, int)[] { (0, 0), (0, 1), (0, 2) };
    game.Path.Should().BeEquivalentTo(expectedPath);
  }

  [Test]
  public async Task CanCaluclatePath_5()
  {
    var grid = new int[][]
    {
      new int[] { 1, 2 },
      new int[] { 2, 10 },
      new int[] { 1, 1 },
    };
    Neighbor[] neighbors = Helpers.GridToNeighbors(grid);
    GamePlayer game = await Helpers.CreateNewGamePlayer(
      neighbors,
      (0, 0),
      (0, 2)
    );

    var expectedPath = new (int, int)[] { (0, 0), (0, 1), (0, 2) };
    CollectionAssert.AreEquivalent(expectedPath, game.Path);
  }

  [Test]
  public async Task CanCaluclatePath_6()
  {
    var grid = new int[][]
    {
      new int[] { 1, 2 },
      new int[] { 10, 1 },
      new int[] { 1, 1 },
    };
    Neighbor[] neighbors = Helpers.GridToNeighbors(grid);
    GamePlayer game = await Helpers.CreateNewGamePlayer(
      neighbors,
      (0, 0),
      (0, 2)
    );

    var expectedPath = new (int, int)[]
    {
      (0, 0),
      (1, 0),
      (1, 1),
      (1, 2),
      (0, 2)
    };
    CollectionAssert.AreEquivalent(expectedPath, game.Path);
  }


  
}
