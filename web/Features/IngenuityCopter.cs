using static System.Linq.Enumerable;

public class IngenuityCopter
{
  private IGameService gameService;
  public MarsMap Map { get; }
  public (int x, int y) Location { get; private set; }
  public int Battery { get; private set; }
  public string Token { get; private set; }

  public IngenuityCopter(
    IGameService gameService,
    MarsMap map,
    (int, int) start,
    string token
  )
  {
    this.gameService = gameService;
    Map = map;
    Location = start;
    Token = token;
  }

  public async Task TakeStepToTarget((int x, int y) target)
  {
    IEnumerable<(int x, int y)> validNeighbors = getValidNeighborsInRange();

    var bestNeigbor = validNeighbors.MinBy(l => DistanceToTarget(target, l));

    await moveAndUpdateStatus(target, bestNeigbor);
  }

  public async Task FollowPath(
    IEnumerable<(int x, int y)> path,
    (int, int) fallbackTarget
  )
  {
    var target = path.Last();

    IEnumerable<(int x, int y)> validNeighbors = getValidNeighborsInRange();

    var bestNeigbor = path.Reverse()
      .FirstOrDefault(l => validNeighbors.Contains(l));

    var closestPath = path.Reverse().MinBy(l => validNeighbors.Min(n => DistanceToTarget(l, n)));

    // if (bestNeigbor == default)
    // {
    //   await TakeStepToTarget(fallbackTarget);
    //   return;
    // }

    await TakeStepToTarget(closestPath);
  }

  private async Task moveAndUpdateStatus(
    (int x, int y) target,
    (int x, int y) bestNeigbor
  )
  {
    checkDistanceTooFar(bestNeigbor);
    // System.Console.WriteLine(
    //   $"Moving copter from {Location} to {bestNeigbor}, target: {target}"
    // );
    var response = await gameService.MoveIngenuity(
      Token,
      bestNeigbor.x,
      bestNeigbor.y
    );
    GameMovement.CheckIfNewLocationUnexpected(bestNeigbor, response);

    Map.UpdateGridWithNeighbors(response.Neighbors);
    Location = (response.X, response.Y);
    Battery = response.BatteryLevel;
  }

  private static double DistanceToTarget(
    (int x, int y) target,
    (int x, int y) l
  )
  {
    return Math.Sqrt(
      Math.Pow(Math.Abs(l.x - target.x), 2)
        + Math.Pow(Math.Abs(l.y - target.y), 2)
    );
  }

  private IEnumerable<(int x, int y)> getValidNeighborsInRange()
  {
    var xOptions = Range(Location.Item1 - 2, 5);
    var yOptions = Range(Location.Item2 - 2, 5);
    var neigbors = xOptions
      .SelectMany(x => yOptions.Select(y => (x, y)))
      .Where(l => Map.LocationIsInGrid(l));
    return neigbors;
  }

  private void checkDistanceTooFar((int x, int y) bestNeigbor)
  {
    var deltaX = Math.Abs(bestNeigbor.x - Location.x);
    var deltaY = Math.Abs(bestNeigbor.y - Location.y);
    if (deltaX >= 3 || deltaY >= 3)
      System.Console.WriteLine(
        $"Trying to move ingenuity too far, current: {Location}, target: {bestNeigbor}, offset ({deltaX}, {deltaY})"
      );
  }
}
