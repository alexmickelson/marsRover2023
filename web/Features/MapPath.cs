using System.Collections.Concurrent;

public class MapPath
{
  public static IEnumerable<(int, int)> CalculatePath(
    ConcurrentDictionary<(int, int), int> grid,
    (int, int) currentLocation,
    (int, int) target,
    (int, int) topRight,
    IEnumerable<(int, int)> roverHistory,
    bool optimize = true
  )
  {
    var (currentCheckLocation, locationPaths) =
      initializeBreadthFirstDataStructures(currentLocation);

    var nextLocationsToCheck = new List<PathHistory>();
    var visited = new List<(int, int)>();

    while (currentCheckLocation != target)
    {
      addNewNeighborsToList(
        grid,
        currentCheckLocation,
        target,
        topRight,
        locationPaths,
        nextLocationsToCheck,
        roverHistory,
        optimize
      );
      (var historyEntry, nextLocationsToCheck) = getAndRemoveSmallestLocation(
        nextLocationsToCheck
      );

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
    List<PathHistory> locationsToCheck,
    IEnumerable<(int, int)> roverHistory,
    bool optimize
  )
  {
    optimize = true;
    List<(int, int)> neighbors = optimize
      ? GetNeighborsOptimized(currentCheckLocation, target, topRight)
      : GetNeighbors(currentCheckLocation, target, topRight);

    var orderedNeighbors = neighbors
      .Where(
        n =>
          !locationPaths.ContainsKey(n)
          && locationsToCheck.Find(l => l.Location == n) == null
          && grid.ContainsKey(n)
      )
      .OrderBy((l) => grid[l]);

    var currentCost = grid[currentCheckLocation];

    var distanceToTarget =
      Math.Abs(currentCheckLocation.Item1 - target.Item1)
      + Math.Abs(currentCheckLocation.Item2 - target.Item2);

    foreach (var n in orderedNeighbors)
    {
      var nextDistanceToTarget =
        Math.Abs(n.Item1 - target.Item1) + Math.Abs(n.Item2 - target.Item2);
      var furtherAway = nextDistanceToTarget > distanceToTarget;

      var nextCost = grid[n] * 3 + currentCost;
      var historyEntry = new PathHistory(nextCost, n, currentCheckLocation);
      locationsToCheck.Add(historyEntry);
    }
  }

  public static List<(int, int)> GetNeighbors(
    (int, int) location,
    (int, int) target,
    (int, int) topRight
  )
  {
    // var allowBacktracking = false;

    var neighbors = new List<(int, int)>();

    var xDistance = Math.Abs(location.Item1 - target.Item1);
    var yDistance = Math.Abs(location.Item2 - target.Item2);
    var xAxisLonger = xDistance > yDistance;

    bool xDecreaseIsTowardsTarget = location.Item1 >= target.Item1;
    bool xIncreaseIsTowardsTarget = location.Item1 <= target.Item1;

    bool yDecreaseIsTowardsTarget = location.Item2 >= target.Item2;
    bool yIncreaseIsTowardsTarget = location.Item2 <= target.Item2;

    bool xDecreaseInBounds = location.Item1 > 0;
    bool xIncreaseInBounds = location.Item1 < topRight.Item1;
    bool yDecreaseInBounds = location.Item2 > 0;
    bool yIncreaseInBounds = location.Item2 < topRight.Item2;

    bool allowBackwards = false;

    if (allowBackwards)
    {
      if (xDecreaseInBounds)
        neighbors.Add((location.Item1 - 1, location.Item2));
      if (yDecreaseInBounds)
        neighbors.Add((location.Item1, location.Item2 - 1));

      if (xIncreaseInBounds)
        neighbors.Add((location.Item1 + 1, location.Item2));
      if (yIncreaseInBounds)
        neighbors.Add((location.Item1, location.Item2 + 1));
    }
    else
    {
      if (xDecreaseInBounds && (!xAxisLonger || xDecreaseIsTowardsTarget))
        neighbors.Add((location.Item1 - 1, location.Item2));
      if (yDecreaseInBounds && (xAxisLonger || yDecreaseIsTowardsTarget))
        neighbors.Add((location.Item1, location.Item2 - 1));

      if (xIncreaseInBounds && (!xAxisLonger || xIncreaseIsTowardsTarget))
        neighbors.Add((location.Item1 + 1, location.Item2));
      if (yIncreaseInBounds && (xAxisLonger || yIncreaseIsTowardsTarget))
        neighbors.Add((location.Item1, location.Item2 + 1));
    }
    return neighbors;
  }

  public static List<(int, int)> GetNeighborsOptimized(
    (int, int) location,
    (int, int) target,
    (int, int) topRight
  )
  {
    var neighbors = new List<(int, int)>();

    bool canDecreaseX = location.Item1 >= target.Item1 && location.Item1 > 0;
    bool canIncreaseX =
      location.Item1 <= target.Item1 && location.Item1 < topRight.Item1;
    bool canDecreaseY = location.Item2 >= target.Item2 && location.Item2 > 0;
    bool canIncreaseY =
      location.Item2 <= target.Item2 && location.Item2 < topRight.Item2;

    if (canDecreaseX)
      neighbors.Add((location.Item1 - 1, location.Item2));
    if (canDecreaseY)
      neighbors.Add((location.Item1, location.Item2 - 1));

    if (canIncreaseX)
      neighbors.Add((location.Item1 + 1, location.Item2));
    if (canIncreaseY)
      neighbors.Add((location.Item1, location.Item2 + 1));

    return neighbors;
  }
}
