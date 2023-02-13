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

    var rowOffset = CurrentLocation.Item1 - nextLocation.Item1;
    var colOffset = CurrentLocation.Item2 - nextLocation.Item2;
    // System.Console.WriteLine(rowOffset);
    // System.Console.WriteLine(colOffset);

    var desiredOrientation = (rowOffset, colOffset) switch
    {
      (0, 1) => "West",
      (0, -1) => "East",
      (1, 0) => "South",
      (-1, 0) => "North",
      (0, 0) => throw new Exception("Cannot move to same position"),
      _
        => throw new Exception(
          $"Error detecting direction, {(rowOffset, colOffset)}"
        ),
    };

    await turnToFaceCorrectDirection(desiredOrientation);

    var response = await MoveAndUpdateStatus(Direction.Forward);

    System.Console.WriteLine(nextLocation);
    System.Console.WriteLine((response.Row, response.Column));

    if (response.Row != nextLocation.Item1)
      System.Console.WriteLine(
        $"Got back a different row than we tried to get to. wanted {nextLocation.Item1}, got {response.Row}"
      );
    if (response.Column != nextLocation.Item2)
      System.Console.WriteLine(
        $"Got back a different column than we tried to get to. wanted {nextLocation.Item2}, got {response.Column}"
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
        $"Moved to {(response.Row, response.Column)} from {CurrentLocation}"
      );
    else
      System.Console.WriteLine(
        $"Turned to {response.Orientation} from {Orientation}"
      );
    Battery = response.BatteryLevel;
    Orientation = response.Orientation;
    CurrentLocation = (response.Row, response.Column);
    return response;
  }
}
