public class GamePlayer
{
  private IGameService gameService;
  public MarsMap? Map { get; set; } = null;
  public IEnumerable<(int, int)> Path { get; set; }
  public (int, int) CurrentLocation { get; private set; } =
    default;
  public (int, int) Target { get; private set; } = default;

  public GamePlayer(IGameService gameService)
  {
    this.gameService = gameService;
  }

  public async Task Register(string gameId)
  {
    System.Console.WriteLine("Registering");
    gameService.GameId = gameId;
    var response = await gameService.JoinGame();
    Map = new MarsMap(
      response.LowResolutionMap,
      response.Neighbors
    );
    CurrentLocation = (
      response.StartingRow,
      response.StartingColumn
    );
    Target = (response.TargetRow, response.TargetColumn);

    System.Console.WriteLine("Registered for game");
    calculatePath();
  }

  private void calculatePath()
  {
    if (Map == null)
      throw new Exception(
        "Map cannot be null to calculate path"
      );

    var checkLocation = CurrentLocation;
    var path = new List<(int, int)> { };

    while (checkLocation != Target)
    {
      List<(int, int)> neighbors = getNeighbors(
        checkLocation
      );
      var nextShortest = neighbors.Where(n => !path.Contains(n)).MinBy(n => Map.Grid[n]);
      path.Add(nextShortest);
      checkLocation = nextShortest;
    }
    Path = path;
  }

  private List<(int, int)> getNeighbors((int, int) location)
  {
    var neighbors = new List<(int, int)>();

    bool canGoDown =
      location.Item1 >= Target.Item1 && location.Item1 > 0;
    bool canGoUp =
      location.Item1 <= Target.Item1
      && location.Item1 < Map.TopRight.Item1;
    bool canGoLeft =
      location.Item2 >= Target.Item2 && location.Item2 > 0;
    bool canGoRight =
      location.Item2 <= Target.Item2
      && location.Item2 < Map.TopRight.Item2;

    if (canGoDown)
      neighbors.Add((location.Item1 - 1, location.Item2));
    if (canGoLeft)
      neighbors.Add((location.Item1, location.Item2 - 1));

    if (canGoUp)
      neighbors.Add((location.Item1 + 1, location.Item2));
    if (canGoRight)
      neighbors.Add((location.Item1, location.Item2 + 1));

    return neighbors;
  }
}
