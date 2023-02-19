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
  public int LastGridOptimizationTime { get; private set; }
  public List<(int, int)> History { get; set; } = new();
  public (int, int) CurrentLocation { get; private set; } = default;
  public (int, int) StartingLocation { get; private set; } = default;
  public (int, int) Target { get; private set; } = default;
  public (int, int) LowResCurrentLocation { get; private set; } = default;
  public (int, int) LowResTarget { get; private set; } = default;
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
    OptimizeGrid();
    while (true)
    {
      var (start, end, cost, time) = await Take1Step();
      System.Console.WriteLine(
        $"{start} -> {end}, cost: {cost}, time: {time} ms"
      );
      if (!Map.IsAnEdge(CurrentLocation))
      {
        CalculateDetailedPath();
        OptimizeGrid();
      }
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

  public void OptimizeGrid()
  {
    if (Path == null)
      CalculateDetailedPath();

    var gridOptimizationTimer = System.Diagnostics.Stopwatch.StartNew();
    gridOptimizationTimer.Start();
    Map.OptimizeGrid(Path);

    gridOptimizationTimer.Stop();
    LastGridOptimizationTime = (int)gridOptimizationTimer.ElapsedMilliseconds;

    if (OnPathUpdated != null)
      Task.Run(() => OnPathUpdated());
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
