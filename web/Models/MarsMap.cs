using static System.Linq.Enumerable;
using System.Collections.Concurrent;

public class MarsMap
{
  public ConcurrentDictionary<(int, int), int> Grid { get; private set; }
  public (int, int) TopRight { get; set; } = (0, 0);

  public MarsMap()
  {
    Grid = new() { [(0, 0)] = 1 };
  }

  public MarsMap(IEnumerable<LowResolutionMap> lowResMap, IEnumerable<Neighbor>? neighbors = null)
  {
    (Grid, TopRight) = createEmptyGrid(lowResMap);
    setLowResMapValues(lowResMap);

    if (neighbors != null)
    {
      updateGridWithNeighbors(neighbors);
    }
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
}
