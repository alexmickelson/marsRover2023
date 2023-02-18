using System.Collections.Concurrent;
using static System.Linq.Enumerable;

public class GamePlayer
{
  private IGameService gameService;
  public event Action OnPositionChanged;
  public event Action OnPathUpdated;
  public MarsMap? Map { get; set; } = null;
  public IEnumerable<(int, int)> Path { get; set; } = new (int, int)[] { };
  public int LastPathCalulationTime { get; private set; }
  public List<(int, int)> History { get; set; } = new();
  public (int, int) CurrentLocation { get; private set; } = default;
  public (int, int) StartingLocation { get; private set; } = default;
  public (int, int) Target { get; private set; } = default;
  public (int, int) LowResCurrentLocation { get; private set; } = default;
  public (int, int) LowResTarget { get; private set; } = default;

  // public IEnumerable<(int, int)> LowResPath { get; set; }
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
      var (start, end, cost, time) = await Take1Step();
      System.Console.WriteLine(
        $"{start} -> {end}, cost: {cost}, time: {time} ms"
      );

      CalculateDetailedPath();
    }
  }

  public void CalculateDetailedPath()
  {
    if (Map == null)
      throw new NullReferenceException("map cannot be null in detailed path");

    var pathTimer = System.Diagnostics.Stopwatch.StartNew();
    lock (Path)
    {
      if (Map.OptimizedGrid != null)
      {
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
    pathTimer.Stop();
    int elapsedMs = (int)pathTimer.ElapsedMilliseconds;
    LastPathCalulationTime = elapsedMs;

    if (OnPathUpdated != null)
      Task.Run(() => OnPathUpdated());
  }

  // public void CalculateLowResPath()
  // {
  //   if (Map == null)
  //     throw new NullReferenceException("map cannot be null in detailed path");

  //   LowResPath = MapPath.CalculatePath(
  //     Map.LowResGrid,
  //     LowResCurrentLocation,
  //     LowResTarget,
  //     Map.LowResTopRight
  //   );
  // }

  public void OptimizeGrid()
  {
    CalculateDetailedPath();

    var newGrid = new ConcurrentDictionary<(int, int), int>();

    // var detailedLocationsInLowResPath = new List<(int, int)>();

    // foreach (var lowResLocation in Path)
    // {
    //   var startingX = lowResLocation.Item1 * Map.LowResScaleFactor;
    //   var startingY = lowResLocation.Item2 * Map.LowResScaleFactor;
    //   foreach (var x in Range(startingX, Map.LowResScaleFactor))
    //   {
    //     foreach (var y in Range(startingY, Map.LowResScaleFactor))
    //     {
    //       newGrid[(x, y)] = Map.Grid[(x, y)];
    //     }
    //   }
    // }

    Map.Grid.Keys
      .Where((l) => pointCloseToTarget(l) || pointCloseToPath(l))
      .ToList()
      .ForEach(k =>
      {
        if (!newGrid.ContainsKey(k))
          newGrid[k] = Map.Grid[k];
      });

    Map.OptimizedGrid = newGrid;
  }

  private bool pointCloseToTarget((int, int) l)
  {
    var range = 30;
    return (Math.Abs(Target.Item1 - l.Item1) < range)
      && (Math.Abs(Target.Item2 - l.Item2) < range);
  }

  private bool pointCloseToPath((int, int) l)
  {
    var range = 30;

    var pointsInRange = Path.Where(
        p =>
          Math.Abs(p.Item1 - l.Item1) < range
          && Math.Abs(p.Item2 - l.Item2) < range
      )
      .Count();

    return pointsInRange > 0;
  }

  public async Task<(
    (int, int) start,
    (int, int) end,
    int cost,
    int time
  )> Take1Step()
  {
    var moveTimer = System.Diagnostics.Stopwatch.StartNew();
    var startingBattery = Battery;
    var startinglocation = CurrentLocation;

    while (Path.First() == CurrentLocation)
      Path = Path.Skip(1);

    var nextLocation = Path.First();

    GameMovement.CheckIfTargetTooFar(startinglocation, nextLocation);
    string desiredOrientation = GameMovement.CalculateOrientation(
      startinglocation,
      nextLocation
    );
    await turnToFaceCorrectDirection(desiredOrientation);

    var response = await MoveAndUpdateStatus(Direction.Forward);
    GameMovement.CheckIfNewLocationUnexpected(nextLocation, response);

    if (OnPositionChanged != null)
      await Task.Run(() => OnPositionChanged()).ConfigureAwait(false);

    Map.UpdateGridWithNeighbors(response.Neighbors);

    var batteryDiff = startingBattery - Battery;

    moveTimer.Stop();
    var elapsedMs = moveTimer.ElapsedMilliseconds;
    return (StartingLocation, nextLocation, batteryDiff, (int)elapsedMs);
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

    Battery = response.BatteryLevel;
    Orientation = response.Orientation;
    History.Add(CurrentLocation);
    Map.UpdateGridWithNeighbors(response.Neighbors);

    CurrentLocation = (response.X, response.Y);
    return response;
  }
}
