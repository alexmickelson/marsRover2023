using static System.Linq.Enumerable;
using System.Collections.Concurrent;

public class MarsMap
{
  public ConcurrentDictionary<(int, int), int> Grid { get; private set; }
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
      updateGridWithNeighbors(neighbors);
    }
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

  private void updateGridWithNeighbors(IEnumerable<Neighbor> neighbors)
  {
    foreach (var neighbor in neighbors)
    {
      Grid[(neighbor.Row, neighbor.Column)] = neighbor.Difficulty;
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
}
