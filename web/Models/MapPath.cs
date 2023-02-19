using System.Collections.Concurrent;

public class MapPath
{
  public static int costMultiplier = 10;
  public static List<(int, int)> GetNeighbors(
    (int, int) location,
    (int, int) target,
    (int, int) topRight
  )
  {
    var neighbors = new List<(int, int)>();

    bool canGoDown = location.Item1 >= target.Item1 && location.Item1 > 0;
    bool canGoUp =
      location.Item1 <= target.Item1 && location.Item1 < topRight.Item1;
    bool canGoLeft = location.Item2 >= target.Item2 && location.Item2 > 0;
    bool canGoRight =
      location.Item2 <= target.Item2 && location.Item2 < topRight.Item2;

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

  public static IEnumerable<(int, int)> CalculatePath(
    ConcurrentDictionary<(int, int), int> grid,
    (int, int) currentLocation,
    (int, int) target,
    (int, int) topRight
  )
  {
    var (currentCheckLocation, locationPaths) =
      initializeBreadthFirstDataStructures(currentLocation);
    var locationsToProcess = new List<PathHistory>();

    var visited = new List<(int, int)>();

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

      // System.Console.WriteLine(historyEntry);

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

  record PathHistory(
    int Cost,
    (int, int) Location,
    (int, int)? PreviousLocation
  );

  private static void addNewNeighborsToList(
    ConcurrentDictionary<(int, int), int> grid,
    (int, int) currentCheckLocation,
    (int, int) target,
    (int, int) topRight,
    Dictionary<(int, int), PathHistory> locationPaths,
    List<PathHistory> locationsToCheck
  )
  {
    List<(int, int)> neighbors = GetNeighbors(
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
      var nextCost = (costMultiplier * grid[n]) + currentCost;
      var historyEntry = new PathHistory(nextCost, n, currentCheckLocation);
      locationsToCheck.Add(historyEntry);
    }
  }
}
