using static System.Linq.Enumerable;
using System.Collections.Concurrent;

public class MarsMap
{
  public ConcurrentDictionary<(int, int), int> Grid { get; private set; }
  public ConcurrentDictionary<(int, int), int> OptimizedGrid { get; set; }
  public ConcurrentDictionary<(int, int), int> LowResGrid { get; private set; }
  public IEnumerable<LowResolutionMap> LowResolutionMaps { get; private set; }
  public int LowResScaleFactor { get; set; }
  public (int, int) TopRight { get; set; } = (0, 0);
  public (int, int) LowResTopRight { get; set; } = (0, 0);

  public MarsMap()
  {
    Grid = new() { [(0, 0)] = 1 };
  }

  public MarsMap(
    IEnumerable<LowResolutionMap> lowResMap,
    IEnumerable<Neighbor>? neighbors = null
  )
  {
    initializeLowResData(lowResMap);
    (Grid, TopRight) = createEmptyGrid(lowResMap);
    setLowResMapValues(lowResMap);

    if (neighbors != null)
    {
      UpdateGridWithNeighbors(neighbors);
    }
  }

  public int CalculatePathCost(IEnumerable<(int, int)> path)
  {
    return path.Select(l => Grid[l]).Sum();
  }

  private void initializeLowResData(IEnumerable<LowResolutionMap> lowResMap)
  {
    LowResGrid = new();
    LowResolutionMaps = lowResMap;
    LowResScaleFactor =
      lowResMap.First().UpperRightRow - lowResMap.First().LowerLeftRow + 1;

    var maxRow = 0;
    var maxCol = 0;
    foreach (var lowRes in lowResMap)
    {
      var scaledRow = lowRes.LowerLeftRow / LowResScaleFactor;
      var scaledColumn = lowRes.LowerLeftColumn / LowResScaleFactor;
      LowResGrid[(scaledRow, scaledColumn)] = lowRes.AverageDifficulty;
      if (scaledColumn > maxCol)
        maxCol = scaledColumn;
      if (scaledRow > maxRow)
        maxRow = scaledRow;
    }
    LowResTopRight = (maxRow, maxCol);
  }

  public void UpdateGridWithNeighbors(IEnumerable<Neighbor> neighbors)
  {
    foreach (var neighbor in neighbors)
    {
      Grid[(neighbor.Row, neighbor.Column)] = neighbor.Difficulty;
      if (OptimizedGrid != null)
        OptimizedGrid[(neighbor.Row, neighbor.Column)] = neighbor.Difficulty;
    }
  }

  private (ConcurrentDictionary<(int, int), int>, (int, int)) createEmptyGrid(
    IEnumerable<LowResolutionMap> lowResMap
  )
  {
    var rows = lowResMap.Max(l => l.UpperRightRow);
    var columns = lowResMap.Max(l => l.UpperRightColumn);

    var newGrid = new ConcurrentDictionary<(int, int), int>();
    foreach (var r in Range(0, rows + 1))
      foreach (var c in Range(0, columns + 1))
        newGrid[(r, c)] = 0;

    return (newGrid, (rows, columns));
  }

  private void setLowResMapValues(IEnumerable<LowResolutionMap> lowResMap)
  {
    foreach (var cell in lowResMap)
    {
      var rowRangeCount = cell.UpperRightRow - cell.LowerLeftRow + 1;
      var colRangeCount = cell.UpperRightColumn - cell.LowerLeftColumn + 1;

      foreach (var i in Range(cell.LowerLeftRow, rowRangeCount))
        foreach (var j in Range(cell.LowerLeftColumn, colRangeCount))
          Grid[(i, j)] = cell.AverageDifficulty;
    }
  }

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
}
