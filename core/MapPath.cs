using System.Collections.Concurrent;

public class MapPath
{
  public static IEnumerable<(int x, int y)> CalculatePath(
    ConcurrentDictionary<(int x, int y), int> grid,
    (int x, int y) currentLocation,
    (int x, int y) target,
    (int x, int y) topRight,
    IEnumerable<(int x, int y)> roverHistory,
    bool optimize = true
  )
  {
    var (currentCheckLocation, locationPaths) =
      initializeBreadthFirstDataStructures(currentLocation);

    var nextLocationsToCheck = new List<PathHistory>();
    var visited = new List<(int x, int y)>();

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
    (int x, int y),
    Dictionary<(int x, int y), PathHistory>
  ) initializeBreadthFirstDataStructures((int x, int y) currentCheckLocation)
  {
    var locationPaths = new Dictionary<(int x, int y), PathHistory>();
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

  private static IEnumerable<(int x, int y)> reconstructPathFromHistory(
    Dictionary<(int x, int y), PathHistory> locationPaths,
    (int x, int y) destination
  )
  {
    List<(int x, int y)> path = new List<(int x, int y)>() { destination };
    var previous = locationPaths[destination].PreviousLocation;
    while (previous != null)
    {
      var notNullPrevious = ((int x, int y))previous;
      path.Add(notNullPrevious);
      previous = locationPaths[notNullPrevious].PreviousLocation;
    }
    return path.ToArray().Reverse();
  }

  record PathHistory(
    int Cost,
    (int x, int y) Location,
    (int x, int y)? PreviousLocation
  );

  private static void addNewNeighborsToList(
    ConcurrentDictionary<(int x, int y), int> grid,
    (int x, int y) currentCheckLocation,
    (int x, int y) target,
    (int x, int y) topRight,
    Dictionary<(int x, int y), PathHistory> locationPaths,
    List<PathHistory> locationsToCheck,
    IEnumerable<(int x, int y)> roverHistory,
    bool optimize
  )
  {
    // optimize = true;
    List<(int x, int y)> neighbors = optimize
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
      Math.Abs(currentCheckLocation.x - target.x)
      + Math.Abs(currentCheckLocation.y - target.y);

    foreach (var n in orderedNeighbors)
    {
      var nextDistanceToTarget =
        Math.Abs(n.x - target.x) + Math.Abs(n.y - target.y);
      var furtherAway = nextDistanceToTarget > distanceToTarget;

      var lastHistoryEntry = locationPaths[currentCheckLocation];
      var previousLocation = lastHistoryEntry.PreviousLocation;

      var lastOffset = (
        currentCheckLocation.x - previousLocation?.x ?? currentCheckLocation.x,
        currentCheckLocation.y - previousLocation?.y ?? currentCheckLocation.y
      );

      var currentOffset = (
        n.x - currentCheckLocation.x,
        n.y - currentCheckLocation.y
      );


      var costMultiplier = optimize ? 6 : 1;
      var nextCost = (grid[n] * costMultiplier) + currentCost;
      if (lastOffset != currentOffset)
        nextCost += 30;
      var historyEntry = new PathHistory(nextCost, n, currentCheckLocation);
      locationsToCheck.Add(historyEntry);
    }
  }

  public static List<(int x, int y)> GetNeighbors(
    (int x, int y) location,
    (int x, int y) target,
    (int x, int y) topRight
  )
  {
    var neighbors = new List<(int x, int y)>();

    var xDistance = Math.Abs(location.x - target.x);
    var yDistance = Math.Abs(location.y - target.y);
    var xAxisLonger = xDistance > yDistance;

    bool xDecreaseIsTowardsTarget = location.x >= target.x;
    bool xIncreaseIsTowardsTarget = location.x <= target.x;

    bool yDecreaseIsTowardsTarget = location.y >= target.y;
    bool yIncreaseIsTowardsTarget = location.y <= target.y;

    bool xDecreaseInBounds = location.x > 0;
    bool xIncreaseInBounds = location.x < topRight.x;
    bool yDecreaseInBounds = location.y > 0;
    bool yIncreaseInBounds = location.y < topRight.y;

    bool allowBackwards = false;

    if (allowBackwards)
    {
      if (xDecreaseInBounds)
        neighbors.Add((location.x - 1, location.y));
      if (yDecreaseInBounds)
        neighbors.Add((location.x, location.y - 1));

      if (xIncreaseInBounds)
        neighbors.Add((location.x + 1, location.y));
      if (yIncreaseInBounds)
        neighbors.Add((location.x, location.y + 1));
    }
    else
    {
      if (xDecreaseInBounds && (!xAxisLonger || xDecreaseIsTowardsTarget))
        neighbors.Add((location.x - 1, location.y));
      if (yDecreaseInBounds && (xAxisLonger || yDecreaseIsTowardsTarget))
        neighbors.Add((location.x, location.y - 1));

      if (xIncreaseInBounds && (!xAxisLonger || xIncreaseIsTowardsTarget))
        neighbors.Add((location.x + 1, location.y));
      if (yIncreaseInBounds && (xAxisLonger || yIncreaseIsTowardsTarget))
        neighbors.Add((location.x, location.y + 1));
    }
    return neighbors;
  }

  public static List<(int x, int y)> GetNeighborsOptimized(
    (int x, int y) location,
    (int x, int y) target,
    (int x, int y) topRight
  )
  {
    var neighbors = new List<(int x, int y)>();

    bool canDecreaseX = location.x >= target.x && location.x > 0;
    bool canIncreaseX = location.x <= target.x && location.x < topRight.x;
    bool canDecreaseY = location.y >= target.y && location.y > 0;
    bool canIncreaseY = location.y <= target.y && location.y < topRight.y;

    if (canDecreaseX)
      neighbors.Add((location.x - 1, location.y));
    if (canDecreaseY)
      neighbors.Add((location.x, location.y - 1));

    if (canIncreaseX)
      neighbors.Add((location.x + 1, location.y));
    if (canIncreaseY)
      neighbors.Add((location.x, location.y + 1));

    return neighbors;
  }
}
