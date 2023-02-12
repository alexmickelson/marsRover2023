using System.Collections.Concurrent;
using static System.Linq.Enumerable;

public class GamePlayer
{
  private IGameService gameService;
  public MarsMap? Map { get; set; } = null;
  public IEnumerable<(int, int)> Path { get; set; }
  public (int, int) CurrentLocation { get; private set; } = default;
  public (int, int) Target { get; private set; } = default;

  public (int, int) LowResCurrentLocation { get; private set; } = default;
  public (int, int) LowResTarget { get; private set; } = default;
  public IEnumerable<(int, int)> LowResPath { get; set; }

  public int Battery { get; set; }
  public string Orientation { get; set; }

  public GamePlayer(IGameService gameService)
  {
    this.gameService = gameService;
  }

  public async Task Register(string gameId, string name = "Test_Alex")
  {
    System.Console.WriteLine("Registering");
    gameService.GameId = gameId;
    var response = await gameService.JoinGame(name);
    Battery = 2000;
    Orientation = response.Orientation;

    Map = new MarsMap(response.LowResolutionMap, response.Neighbors);

    CurrentLocation = (response.StartingRow, response.StartingColumn);
    Target = (response.TargetRow, response.TargetColumn);

    LowResCurrentLocation = (
      response.StartingRow / Map.LowResScaleFactor,
      response.StartingColumn / Map.LowResScaleFactor
    );
    LowResTarget = (
      response.TargetRow / Map.LowResScaleFactor,
      response.TargetColumn / Map.LowResScaleFactor
    );

    System.Console.WriteLine("Registered for game");
  }

  public void CalculateDetailedPath()
  {
    if (Map == null)
      throw new NullReferenceException("map cannot be null in detailed path");

    if (Map.OptimizedGrid != null)
    {
      System.Console.WriteLine("total cells");
      System.Console.WriteLine(Map.OptimizedGrid.Count());
      Path = MarsMap.CalculatePath(
        Map.OptimizedGrid,
        CurrentLocation,
        Target,
        Map.TopRight
      );
    }
    else
    {
      Path = MarsMap.CalculatePath(
        Map.Grid,
        CurrentLocation,
        Target,
        Map.TopRight
      );
    }
  }

  public void CalculateLowResPath()
  {
    if (Map == null)
      throw new NullReferenceException("map cannot be null in detailed path");

    LowResPath = MarsMap.CalculatePath(
      Map.LowResGrid,
      LowResCurrentLocation,
      LowResTarget,
      Map.LowResTopRight
    );
  }

  public void OptimizeGrid()
  {
    CalculateLowResPath();

    var newGrid = new ConcurrentDictionary<(int, int), int>();

    var detailedLocationsInLowResPath = new List<(int, int)>();

    foreach (var lowResLocation in LowResPath)
    {
      var startingRow = lowResLocation.Item1 * Map.LowResScaleFactor;
      var startingColumn = lowResLocation.Item2 * Map.LowResScaleFactor;
      foreach (var row in Range(startingRow, Map.LowResScaleFactor))
      {
        foreach (var col in Range(startingColumn, Map.LowResScaleFactor))
        {
          newGrid[(row, col)] = Map.Grid[(row, col)];
        }
      }
    }

    Map.Grid.Keys
      .Where(
        (l) =>
          (Math.Abs(Target.Item1 - l.Item1) < 10)
          && (Math.Abs(Target.Item2 - l.Item2) < 10)
      )
      .ToList()
      .ForEach(k =>
      {
        if (!newGrid.ContainsKey(k))
          newGrid[k] = Map.Grid[k];
      });

    Map.OptimizedGrid = newGrid;
  }

  public async Task FollowPath()
  {
    while (Path.Count() > 0)
    {
      var nextLocation = Path.First();
      Path = Path.Skip(1);
      var rowOffset = CurrentLocation.Item1 - nextLocation.Item1;
      var colOffset = CurrentLocation.Item2 - CurrentLocation.Item2;
      System.Console.WriteLine(rowOffset);
      System.Console.WriteLine(colOffset);

      var desiredOrientation = (rowOffset, colOffset) switch
      {
        (0, 1) => "East",
        (0, -1) => "West",
        (1, 0) => "North",
        (-1, 0) => "South",
      };

      MoveResponse response;
      if (Orientation != desiredOrientation)
      {
        var turnLeft =
          (Orientation == "East" && desiredOrientation == "North")
          || (Orientation == "North" && desiredOrientation == "West")
          || (Orientation == "West" && desiredOrientation == "South")
          || (Orientation == "South" && desiredOrientation == "East");
        if (turnLeft)
          response = await gameService.Move(Direction.Left);
        else
          response = await gameService.Move(Direction.Right);
      }
      else
      {
        response = await gameService.Move(Direction.Forward);
      }

      Battery = response.BatteryLevel;
      Orientation = response.Orientation;
      System.Console.WriteLine("resposne message: ");
      System.Console.WriteLine(response.Message);

      if (response.Row != Path.First().Item1)
        System.Console.WriteLine(
          $"Got back a different row than we tried to get to. wanted {Path.First().Item1}, got {response.Row}"
        );
      if (response.Column != Path.First().Item2)
        System.Console.WriteLine(
          $"Got back a different column than we tried to get to. wanted {Path.First().Item2}, got {response.Column}"
        );

      Map.UpdateGridWithNeighbors(response.Neighbors);
    }
  }
}
