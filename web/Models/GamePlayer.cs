using System.Collections.Concurrent;

public class GamePlayer
{
  private IGameService gameService;
  public MarsMap? Map { get; set; } = null;
  public IEnumerable<(int, int)> Path { get; set; }
  public (int, int) CurrentLocation { get; private set; } = default;
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
    Map = new MarsMap(response.LowResolutionMap, response.Neighbors);
    CurrentLocation = (response.StartingRow, response.StartingColumn);
    Target = (response.TargetRow, response.TargetColumn);

    System.Console.WriteLine("Registered for game");
  }

  record PathHistory(
    int Cost,
    (int, int) Location,
    (int, int)? PreviousLocation
  );

  public void CalculatePath(
    ConcurrentDictionary<(int, int), int> grid,
    (int, int) target,
    (int, int) topRight
  )
  {
    var (currentCheckLocation, locationPaths) =
      initializeBreadthFirstDataStructures(CurrentLocation);
    var locationsToProcess = new List<PathHistory>();

    while (currentCheckLocation != Target)
    {
      addNewNeighborsToList(
        grid,
        currentCheckLocation,
        target,
        topRight,
        locationPaths,
        locationsToProcess
      );
      PathHistory historyEntry = getAndRemoveSmallestLocation(
        ref locationsToProcess
      );

      locationPaths[historyEntry.Location] = historyEntry;
      currentCheckLocation = historyEntry.Location;
    }

    Path = reconstructPathFromHistory(locationPaths, Target);
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

  private static PathHistory getAndRemoveSmallestLocation(
    ref List<PathHistory> locationsToProcess
  )
  {
    locationsToProcess = locationsToProcess.OrderBy(h => h.Cost).ToList();
    var historyEntry = locationsToProcess[0];
    locationsToProcess.RemoveAt(0);
    return historyEntry;
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
      .Where(n => !locationPaths.ContainsKey(n))
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
    throw new NotImplementedException();
  }
}
