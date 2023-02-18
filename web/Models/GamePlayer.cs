using System.Collections.Concurrent;
using static System.Linq.Enumerable;

public class GamePlayer
{
  private IGameService gameService;
  public event Action OnPositionChanged;
  public MarsMap? Map { get; set; } = null;
  public IEnumerable<(int, int)> Path { get; set; }
  public List<(int, int)> History { get; set; } = new();
  public (int, int) CurrentLocation { get; private set; } = default;
  public (int, int) StartingLocation { get; private set; } = default;
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
    StartingLocation = CurrentLocation;
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

  public async Task PlayGame()
  {
    CalculateDetailedPath();
    while (true)
    {
      var watch = System.Diagnostics.Stopwatch.StartNew();
      var (start, end, cost) = await Take1Step();
      watch.Stop();
      var elapsedMs = watch.ElapsedMilliseconds;

      System.Console.WriteLine(
        $"{start} -> {end}, cost: {cost}, time: {elapsedMs} ms"
      );

      var watch2 = System.Diagnostics.Stopwatch.StartNew();
      CalculateDetailedPath();
      elapsedMs = watch2.ElapsedMilliseconds;
      System.Console.WriteLine($"calculating path took {elapsedMs} ms");
    }
  }

  public void CalculateDetailedPath()
  {
    if (Map == null)
      throw new NullReferenceException("map cannot be null in detailed path");

    if (Map.OptimizedGrid != null)
    {
      System.Console.WriteLine("total cells");
      System.Console.WriteLine(Map.OptimizedGrid.Count());
      Path = MapPath.CalculatePath(
        Map.OptimizedGrid,
        CurrentLocation,
        Target,
        Map.TopRight
      );
    }
    else
    {
      Path = MapPath.CalculatePath(
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

    LowResPath = MapPath.CalculatePath(
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

  public async Task<((int, int) start, (int, int) end, int cost)> Take1Step()
  {
    var startingBattery = Battery;
    var startinglocation = CurrentLocation;

    while (Path.First() == CurrentLocation)
      Path = Path.Skip(1);

    var nextLocation = Path.First();

    CheckIfTargetTooFar(nextLocation);
    string desiredOrientation = calculateOrientation(nextLocation);
    await turnToFaceCorrectDirection(desiredOrientation);

    var response = await MoveAndUpdateStatus(Direction.Forward);
    checkIfNewLocationUnexpected(nextLocation, response);

    OnPositionChanged?.Invoke();
    Map.UpdateGridWithNeighbors(response.Neighbors);

    var batteryDiff = startingBattery - Battery;
    return (StartingLocation, nextLocation, batteryDiff);
  }

  private static void checkIfNewLocationUnexpected(
    (int, int) nextLocation,
    MoveResponse response
  )
  {
    if (response.X != nextLocation.Item1 || response.Y != nextLocation.Item2)
      System.Console.WriteLine(
        $"Got back a different coordinate than we tried to get to."
          + $" wanted {(nextLocation.Item1, nextLocation.Item2)}, got {(response.X, response.Y)}"
      );
  }

  private string calculateOrientation((int, int) nextLocation)
  {
    var xOffset = nextLocation.Item1 - CurrentLocation.Item1;
    var yOffset = nextLocation.Item2 - CurrentLocation.Item2;

    var desiredOrientation = (xOffset, yOffset) switch
    {
      (0, 1) => "North",
      (0, -1) => "South",
      (1, 0) => "East",
      (-1, 0) => "West",
      (0, 0) => throw new Exception("Cannot move to same position"),
      _
        => throw new Exception(
          $"Error detecting direction, {(xOffset, yOffset)}"
        ),
    };
    return desiredOrientation;
  }

  private void CheckIfTargetTooFar((int, int) nextLocation)
  {
    if (
      Math.Abs(nextLocation.Item1 - CurrentLocation.Item1) > 1
      || Math.Abs(nextLocation.Item2 - CurrentLocation.Item2) > 1
    )
      System.Console.WriteLine(
        $"Trying to get to location more than one square away, current: {CurrentLocation}, target: {nextLocation}"
      );
  }

  private async Task turnToFaceCorrectDirection(string desiredOrientation)
  {
    while (Orientation != desiredOrientation)
    {
      var turnLeft =
        (Orientation == "East" && desiredOrientation == "North")
        || (Orientation == "North" && desiredOrientation == "West")
        || (Orientation == "West" && desiredOrientation == "South")
        || (Orientation == "South" && desiredOrientation == "East");
      if (turnLeft)
      {
        await MoveAndUpdateStatus(Direction.Left);
      }
      else
      {
        await MoveAndUpdateStatus(Direction.Right);
      }
    }
  }

  private async Task<MoveResponse> MoveAndUpdateStatus(Direction direction)
  {
    var response = await gameService.Move(direction);

    var batteryDiff = Battery - response.BatteryLevel;
    var newLocation = (response.X, response.Y);
    // if (direction == Direction.Forward)
    // {
    //   System.Console.WriteLine(
    //     $"Moved to {(response.X, response.Y)} from {CurrentLocation}, cost {batteryDiff}"
    //   );
    // }
    // else
    //   System.Console.WriteLine(
    //     $"Turned to {response.Orientation} from {Orientation}, cost {batteryDiff}"
    //   );
    Battery = response.BatteryLevel;
    Orientation = response.Orientation;
    History.Add(CurrentLocation);
    Map.UpdateGridWithNeighbors(response.Neighbors);

    CurrentLocation = newLocation;
    return response;
  }
}
