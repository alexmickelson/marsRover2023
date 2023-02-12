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

  public void CalculatePath()
  {
    if (Map == null)
      throw new Exception("Map cannot be null to calculate path");

    var (currentCheckLocation, locationPaths) =
      initializeBreadthFirstDataStructures();
    var locationsToProcess = new List<PathHistory>();

    while (currentCheckLocation != Target)
    {
      addNewNeighborsToList(
        currentCheckLocation,
        locationPaths,
        locationsToProcess
      );
      PathHistory historyEntry = getAndRemoveSmallestLocation(
        ref locationsToProcess
      );

      locationPaths[historyEntry.Location] = historyEntry;

      currentCheckLocation = historyEntry.Location;
    }

    reconstructPathFromHistory(locationPaths);
  }

  private (
    (int, int),
    Dictionary<(int, int), PathHistory>
  ) initializeBreadthFirstDataStructures()
  {
    var currentCheckLocation = CurrentLocation;
    var locationPaths = new Dictionary<(int, int), PathHistory>();
    locationPaths[currentCheckLocation] = new(
      0,
      currentCheckLocation,
      null
    );
    return (currentCheckLocation, locationPaths);
  }

  private static PathHistory getAndRemoveSmallestLocation(
    ref List<PathHistory> locationsToProcess
  )
  {
    locationsToProcess = locationsToProcess
      .OrderBy(h => h.Cost)
      .ToList();
    var historyEntry = locationsToProcess[0];
    locationsToProcess.RemoveAt(0);
    return historyEntry;
  }

  private void reconstructPathFromHistory(
    Dictionary<(int, int), PathHistory> locationPaths
  )
  {
    List<(int, int)> path = new List<(int, int)>() { Target };
    var previous = locationPaths[Target].PreviousLocation;
    while (previous != null)
    {
      var notNullPrevious = ((int, int))previous;
      path.Add(notNullPrevious);
      previous = locationPaths[notNullPrevious].PreviousLocation;
    }
    Path = path.ToArray().Reverse();
  }

  private void addNewNeighborsToList(
    (int, int) currentCheckLocation,
    Dictionary<(int, int), PathHistory> locationPaths,
    List<PathHistory> locationsToCheck
  )
  {
    if (Map == null)
      throw new NullReferenceException("Map Cannot be Null");
    List<(int, int)> neighbors = GetNeighbors(currentCheckLocation);

    var orderedNeighbors = neighbors
      .Where(n => !locationPaths.ContainsKey(n))
      .OrderBy((l) => Map.Grid[l]);

    var currentCost = Map.Grid[currentCheckLocation];
    foreach (var n in orderedNeighbors)
    {
      var nextCost = Map.Grid[n] + currentCost;
      var historyEntry = new PathHistory(
        nextCost,
        n,
        currentCheckLocation
      );
      locationsToCheck.Add(historyEntry);
    }
  }

  public List<(int, int)> GetNeighbors((int, int) location)
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
