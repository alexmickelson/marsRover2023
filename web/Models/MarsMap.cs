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
      lowResMap.First().UpperRightX - lowResMap.First().LowerLeftX + 1;

    var maxX = 0;
    var maxY = 0;
    foreach (var lowRes in lowResMap)
    {
      var scaledX = lowRes.LowerLeftX / LowResScaleFactor;
      var scaledY = lowRes.LowerLeftY / LowResScaleFactor;
      LowResGrid[(scaledX, scaledY)] = lowRes.AverageDifficulty;
      if (scaledY > maxY)
        maxY = scaledY;
      if (scaledX > maxX)
        maxX = scaledX;
    }
    LowResTopRight = (maxX, maxY);
  }

  public void UpdateGridWithNeighbors(IEnumerable<Neighbor> neighbors)
  {
    foreach (var neighbor in neighbors)
    {
      Grid[(neighbor.X, neighbor.Y)] = neighbor.Difficulty;
      if (OptimizedGrid != null)
        OptimizedGrid[(neighbor.X, neighbor.Y)] = neighbor.Difficulty;
    }
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
          Grid[(x, y)] = cell.AverageDifficulty;
    }
  }

}
