
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Text.Json;


public record MoveResponse(
  int X,
  int Y,
  int BatteryLevel,
  Neighbor[] Neighbors,
  string Message,
  string Orientation
);

public record IngenuityMoveResponse(
  int X,
  int Y,
  int BatteryLevel,
  IEnumerable<Neighbor> Neighbors,
  string Message
);

public record StatusResult(
  string status
);


public record JoinResponse(
  string Token,
  int StartingX,
  int StartingY,
  int TargetX,
  int TargetY,
  IEnumerable<Neighbor> Neighbors,
  IEnumerable<LowResolutionMap> LowResolutionMap,
  [property: RegularExpression("North|South|East|West")]
  string Orientation
);

public record Neighbor(
  int X,
  int Y,
  int Difficulty
);

public record LowResolutionMap(
  int LowerLeftX,
  int LowerLeftY,
  int UpperRightX,
  int UpperRightY,
  int AverageDifficulty
);