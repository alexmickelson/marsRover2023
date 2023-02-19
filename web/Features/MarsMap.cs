using static System.Linq.Enumerable;
using System.Collections.Concurrent;

public class MarsMap
{
  public ConcurrentDictionary<(int, int), int> Grid { get; private set; }
  public ConcurrentDictionary<(int, int), int> OptimizedGrid { get; set; }
  public ConcurrentDictionary<(int, int), int> LowResGrid { get; private set; }
  public IEnumerable<LowResolutionMap> LowResolutionMaps { get; private set; }
  public event Action OnMapUpdated;
  public int LowResScaleFactor { get; set; }
  public (int, int) TopRight { get; set; } = (0, 0);

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
      lowResMap.First().UpperRightX - lowResMap.First().LowerLeftX + 1;

    foreach (var lowRes in lowResMap)
    {
      Range(lowRes.LowerLeftX, lowRes.UpperRightX - lowRes.LowerLeftX + 1)
        .ToList()
        .ForEach(
          x =>
            Range(lowRes.LowerLeftY, lowRes.UpperRightY - lowRes.LowerLeftY + 1)
              .ToList()
              .ForEach((y) => LowResGrid[(x, y)] = lowRes.AverageDifficulty)
        );
    }
  }

  public void UpdateGridWithNeighbors(IEnumerable<Neighbor> neighbors)
  {
    foreach (var neighbor in neighbors)
    {
      Grid[(neighbor.X, neighbor.Y)] = neighbor.Difficulty;
      if (OptimizedGrid != null)
        OptimizedGrid[(neighbor.X, neighbor.Y)] = neighbor.Difficulty;
    }
    if (OnMapUpdated != null)
      Task.Run(() => OnMapUpdated());
  }

  private (ConcurrentDictionary<(int, int), int>, (int, int)) createEmptyGrid(
    IEnumerable<LowResolutionMap> lowResMap
  )
  {
    var xCount = lowResMap.Max(l => l.UpperRightX);
    var yCount = lowResMap.Max(l => l.UpperRightY);

    var newGrid = new ConcurrentDictionary<(int, int), int>();
    foreach (var x in Range(0, xCount + 1))
      foreach (var y in Range(0, yCount + 1))
        newGrid[(x, y)] = 0;

    return (newGrid, (xCount, yCount));
  }

  private void setLowResMapValues(IEnumerable<LowResolutionMap> lowResMap)
  {
    foreach (var cell in lowResMap)
    {
      var xRangeCount = cell.UpperRightX - cell.LowerLeftX + 1;
      var yRangeCount = cell.UpperRightY - cell.LowerLeftY + 1;

      foreach (var x in Range(cell.LowerLeftX, xRangeCount))
        foreach (var y in Range(cell.LowerLeftY, yRangeCount))
        {
          if (!IsAnEdge((x, y)))
            Grid[(x, y)] = cell.AverageDifficulty;
        }
    }
  }

  public bool IsAnEdge((int, int) location)
  {
    return location.Item1 == 0
      || location.Item1 == TopRight.Item1
      || location.Item2 == 0
      || location.Item2 == TopRight.Item2;
  }

  public void OptimizeGrid(IEnumerable<(int, int)> path)
  {
    var newGrid =
      OptimizedGrid == null
        ? new ConcurrentDictionary<(int, int), int>()
        : new ConcurrentDictionary<(int, int), int>(OptimizedGrid);

    // var newGrid = new ConcurrentDictionary
    var range = 20;
    foreach (var location in path)
    {
      var neighbors = Range(location.Item1 - range / 2, range)
        .SelectMany(
          (x) => Range(location.Item2 - range / 2, range).Select(y => (x, y))
        );

      foreach (var neighbor in neighbors)
      {
        if (LocationIsInGrid(neighbor))
        {
          if (Grid.ContainsKey(neighbor))
            newGrid[neighbor] = Grid[neighbor];
          else
            newGrid[neighbor] = LowResGrid[neighbor];
        }
      }
    }
    OptimizedGrid = newGrid;
  }

  public bool LocationIsInGrid((int, int) l)
  {
    return l.Item1 >= 0
      && l.Item1 <= TopRight.Item1
      && l.Item2 >= 0
      && l.Item2 <= TopRight.Item2;
  }
}
