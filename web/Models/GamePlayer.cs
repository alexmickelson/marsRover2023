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

  public GamePlayer(IGameService gameService)
  {
    this.gameService = gameService;
  }

  public async Task Register(string gameId)
  {
    System.Console.WriteLine("Registering");
    gameService.GameId = gameId;
    var response = await gameService.JoinGame();
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

  record PathHistory(
    int Cost,
    (int, int) Location,
    (int, int)? PreviousLocation
  );

  public void CalculateDetailedPath()
  {
    if (Map == null)
      throw new NullReferenceException("map cannot be null in detailed path");

    if (Map.OptimizedGrid != null)
    {
      System.Console.WriteLine("total cells");
      System.Console.WriteLine(Map.OptimizedGrid.Count());
      Path = calculatePath(
        Map.OptimizedGrid,
        CurrentLocation,
        Target,
        Map.TopRight
      );
    }
    else
    {
      Path = calculatePath(Map.Grid, CurrentLocation, Target, Map.TopRight);
    }
  }

  public void CalculateLowResPath()
  {
    if (Map == null)
      throw new NullReferenceException("map cannot be null in detailed path");

    LowResPath = calculatePath(
      Map.LowResGrid,
      LowResCurrentLocation,
      LowResTarget,
      Map.LowResTopRight
    );
  }

  private static IEnumerable<(int, int)> calculatePath(
    ConcurrentDictionary<(int, int), int> grid,
    (int, int) currentLocation,
    (int, int) target,
    (int, int) topRight
  )
  {
    var (currentCheckLocation, locationPaths) =
      initializeBreadthFirstDataStructures(currentLocation);
    var locationsToProcess = new List<PathHistory>();

    var visisted = new List<(int, int)>();

    while (currentCheckLocation != target)
    {
      addNewNeighborsToList(
        grid,
        currentCheckLocation,
        target,
        topRight,
        locationPaths,
        locationsToProcess
      );
      (var historyEntry, locationsToProcess) = getAndRemoveSmallestLocation(
        locationsToProcess
      );

      System.Console.WriteLine(historyEntry);

      locationPaths[historyEntry.Location] = historyEntry;
      currentCheckLocation = historyEntry.Location;
    }

    return reconstructPathFromHistory(locationPaths, target);
  }

  private static (
    (int, int),
    Dictionary<(int, int), PathHistory>
  ) initializeBreadthFirstDataStructures((int, int) currentCheckLocation)
  {
    var locationPaths = new Dictionary<(int, int), PathHistory>();
    locationPaths[currentCheckLocation] = new(0, currentCheckLocation, null);
    return (currentCheckLocation, locationPaths);
  }

  private static (PathHistory, List<PathHistory>) getAndRemoveSmallestLocation(
    List<PathHistory> locationsToProcess
  )
  {
    locationsToProcess = locationsToProcess.OrderBy(h => h.Cost).ToList();
    if (locationsToProcess.Count() == 0)
      throw new Exception("Ran out of locations to search");

    var historyEntry = locationsToProcess[0];
    locationsToProcess.RemoveAt(0);
    return (historyEntry, locationsToProcess);
  }

  private static IEnumerable<(int, int)> reconstructPathFromHistory(
    Dictionary<(int, int), PathHistory> locationPaths,
    (int, int) destination
  )
  {
    List<(int, int)> path = new List<(int, int)>() { destination };
    var previous = locationPaths[destination].PreviousLocation;
    while (previous != null)
    {
      var notNullPrevious = ((int, int))previous;
      path.Add(notNullPrevious);
      previous = locationPaths[notNullPrevious].PreviousLocation;
    }
    return path.ToArray().Reverse();
  }

  private static void addNewNeighborsToList(
    ConcurrentDictionary<(int, int), int> grid,
    (int, int) currentCheckLocation,
    (int, int) target,
    (int, int) topRight,
    Dictionary<(int, int), PathHistory> locationPaths,
    List<PathHistory> locationsToCheck
  )
  {
    List<(int, int)> neighbors = MarsMap.GetNeighbors(
      currentCheckLocation,
      target,
      topRight
    );

    var orderedNeighbors = neighbors
      .Where(
        n =>
          !locationPaths.ContainsKey(n)
          && locationsToCheck.Find(l => l.Location == n) == null
          && grid.ContainsKey(n)
      )
      .OrderBy((l) => grid[l]);

    var currentCost = grid[currentCheckLocation];
    foreach (var n in orderedNeighbors)
    {
      var nextCost = grid[n] + currentCost;
      var historyEntry = new PathHistory(nextCost, n, currentCheckLocation);
      locationsToCheck.Add(historyEntry);
    }
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
      .ForEach(k => {
        if(!newGrid.ContainsKey(k))
          newGrid[k] = Map.Grid[k];
      });

    Map.OptimizedGrid = newGrid;
  }
}
