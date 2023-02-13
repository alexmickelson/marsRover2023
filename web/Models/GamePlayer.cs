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

    CurrentLocation = (response.StartingX, response.StartingY);
    Target = (response.TargetX, response.TargetY);

    LowResCurrentLocation = (
      response.StartingX / Map.LowResScaleFactor,
      response.StartingY / Map.LowResScaleFactor
    );
    LowResTarget = (
      response.TargetX / Map.LowResScaleFactor,
      response.TargetY / Map.LowResScaleFactor
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
      var startingX = lowResLocation.Item1 * Map.LowResScaleFactor;
      var startingY = lowResLocation.Item2 * Map.LowResScaleFactor;
      foreach (var x in Range(startingX, Map.LowResScaleFactor))
      {
        foreach (var y in Range(startingY, Map.LowResScaleFactor))
        {
          newGrid[(x, y)] = Map.Grid[(x, y)];
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

  public async Task Take1Step()
  {
    // System.Console.WriteLine("starting at location:");
    // System.Console.WriteLine(CurrentLocation);
    // System.Console.WriteLine("path starts at");
    // System.Console.WriteLine(Path.First());

    while (Path.First() == CurrentLocation)
      Path = Path.Skip(1);

    var nextLocation = Path.First();
    System.Console.WriteLine(
      $"Want to move to {nextLocation}, currently at {CurrentLocation}"
    );
    // Path = Path.Skip(1);

    var xOffset = CurrentLocation.Item1 - nextLocation.Item1;
    var yOffset = CurrentLocation.Item2 - nextLocation.Item2;
    // System.Console.WriteLine(rowOffset);
    // System.Console.WriteLine(colOffset);

    var desiredOrientation = (xOffset, yOffset) switch
    {
      (0, 1) => "West",
      (0, -1) => "East",
      (1, 0) => "South",
      (-1, 0) => "North",
      (0, 0) => throw new Exception("Cannot move to same position"),
      _
        => throw new Exception(
          $"Error detecting direction, {(xOffset, yOffset)}"
        ),
    };

    await turnToFaceCorrectDirection(desiredOrientation);

    var response = await MoveAndUpdateStatus(Direction.Forward);

    System.Console.WriteLine(nextLocation);
    System.Console.WriteLine((response.X, response.Y));

    if (response.X != nextLocation.Item1)
      System.Console.WriteLine(
        $"Got back a different X coordinate than we tried to get to. wanted {nextLocation.Item1}, got {response.X}"
      );
    if (response.Y != nextLocation.Item2)
      System.Console.WriteLine(
        $"Got back a different Y coordinate than we tried to get to. wanted {nextLocation.Item2}, got {response.Y}"
      );

    Map.UpdateGridWithNeighbors(response.Neighbors);
  }

  private async Task turnToFaceCorrectDirection(string desiredOrientation)
  {
    while (Orientation != desiredOrientation)
    {
      System.Console.WriteLine(
        $"Facing {Orientation}, need to turn to face {desiredOrientation}"
      );
      var turnLeft =
        (Orientation == "East" && desiredOrientation == "North")
        || (Orientation == "North" && desiredOrientation == "West")
        || (Orientation == "West" && desiredOrientation == "South")
        || (Orientation == "South" && desiredOrientation == "East");
      if (turnLeft)
      {
        System.Console.WriteLine(
          $"Need to turn left to face {desiredOrientation}"
        );
        await MoveAndUpdateStatus(Direction.Left);
      }
      else
      {
        System.Console.WriteLine(
          $"Need to turn right to face {desiredOrientation}"
        );
        await MoveAndUpdateStatus(Direction.Right);
      }
    }
  }

  private async Task<MoveResponse> MoveAndUpdateStatus(Direction direction)
  {
    var response = await gameService.Move(direction);

    System.Console.WriteLine(response.Message);
    if (direction == Direction.Forward)
      System.Console.WriteLine(
        $"Moved to {(response.X, response.Y)} from {CurrentLocation}"
      );
    else
      System.Console.WriteLine(
        $"Turned to {response.Orientation} from {Orientation}"
      );
    Battery = response.BatteryLevel;
    Orientation = response.Orientation;
    CurrentLocation = (response.X, response.Y);
    return response;
  }
}
