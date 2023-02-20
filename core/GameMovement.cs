public class GameMovement
{
  public static void CheckIfTargetTooFar((int, int) start, (int, int) next)
  {
    if (
      Math.Abs(next.Item1 - start.Item1) > 1
      || Math.Abs(next.Item2 - start.Item2) > 1
    )
      System.Console.WriteLine(
        $"Trying to get to location more than one square away, current: {start}, target: {next}"
      );
  }

  public static string CalculateOrientation((int, int) start, (int, int) next)
  {
    var xOffset = next.Item1 - start.Item1;
    var yOffset = next.Item2 - start.Item2;

    var desiredOrientation = (xOffset, yOffset) switch
    {
      (0, > 0) => "North",
      (> 0, > 0) => "North",
      (0, < 0) => "South",
      (< 0, < 0) => "South",
      (> 0, 0) => "East",
      (< 0, 0) => "West",
      (0, 0) => throw new Exception("Cannot move to same position"),
      // _
      //   => throw new Exception(
      //     $"Error detecting direction, {(xOffset, yOffset)}"
      //   ),
    };
    return desiredOrientation;
  }

  public static void CheckIfNewLocationUnexpected(
    (int, int) nextLocation,
    MoveResponse response
  )
  {
    if (response.X != nextLocation.Item1 || response.Y != nextLocation.Item2)
      System.Console.WriteLine(
        $"Got back a different coordinate than we tried to get to."
          + $" wanted {(nextLocation.Item1, nextLocation.Item2)}, got {(response.X, response.Y)}"
      );
  }
}
