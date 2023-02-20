public class PerserveranceRover
{
  private IGameService gameService;
  public (int x, int y) CurrentLocation { get; private set; } = default;

  public (int x, int y) GetCurrentLocation() => CurrentLocation;

  public (int x, int y) StartingLocation { get; private set; } = default;
  public int LastPathCalulationTime { get; private set; }
  public int LastGridOptimizationTime { get; private set; }
  public string Orientation { get; private set; }
  public int Battery { get; private set; }
  public MarsMap? Map { get; private set; } = null;
  public List<(int x, int y)> History { get; set; } = new();
  public event Action OnPositionChanged;
  public event Action OnPathUpdated;
  public IEnumerable<(int x, int y)> Path { get; private set; } =
    new (int, int)[] { };

  public IEnumerable<(int x, int y)> GetPath()
  {
    return Path;
  }

  public int StartingPathCost { get; private set; }
  public int TotalProjectedCost
  {
    get => Map.CalculatePathCost(History) + Map.CalculatePathCost(Path);
  }
  public (int x, int y) Target { get; private set; } = default;

  public string Token { get; private set; }

  public PerserveranceRover(
    IGameService gameService,
    MarsMap map,
    (int, int) start,
    (int, int) target,
    string orientation,
    string token
  )
  {
    this.gameService = gameService;
    Map = map;
    CurrentLocation = start;
    StartingLocation = start;
    Target = target;
    Orientation = Orientation;
    Token = token;
  }

  public void CalculateDetailedPath(bool optimize = false)
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
          Map.TopRight,
          History,
          optimize
        );
      }
      else
      {
        Path = MapPath.CalculatePath(
          Map.Grid,
          CurrentLocation,
          Target,
          Map.TopRight,
          History,
          optimize
        );
      }
      if (StartingPathCost == 0)
        StartingPathCost = Map.CalculatePathCost(Path);
    }
    pathTimer.Stop();
    int elapsedMs = (int)pathTimer.ElapsedMilliseconds;
    LastPathCalulationTime = elapsedMs;

    if (OnPathUpdated != null)
      Task.Run(() => OnPathUpdated());
  }

  public void OptimizeGrid(bool reset = false)
  {
    if (Path.Count() == 0)
      CalculateDetailedPath(optimize: true);

    var gridOptimizationTimer = System.Diagnostics.Stopwatch.StartNew();
    gridOptimizationTimer.Start();
    Map.OptimizeGrid(Path, reset);

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

    if (Path.Count() == 0)
      throw new Exception("Cannot take step if path is empty");

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
    var response = await gameService.Move(Token, direction);

    var batteryDiff = Battery - response.BatteryLevel;

    Battery = response.BatteryLevel;
    Orientation = response.Orientation;
    History.Add(CurrentLocation);
    Map.UpdateGridWithNeighbors(response.Neighbors);

    CurrentLocation = (response.X, response.Y);
    return response;
  }
}
