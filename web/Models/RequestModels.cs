
using System.ComponentModel.DataAnnotations;

public record MoveResponse(
  int Row,
  int Column,
  int BatteryLevel,
  Neighbor[] Neighbors,
  string Message,
  string Orientation
);

public record IngenuityMoveResponse(
  int Row,
  int Column,
  int BatteryLevel,
  IEnumerable<Neighbor> Neighbors,
  string Message
);

public record StatusResult(
  string status
);


public record JoinResponse(
  string Token,
  int StartingRow,
  int StartingColumn,
  int TargetRow,
  int TargetColumn,
  Neighbor[] Neighbors,
  LowResolutionMap[] LowResolutionMap,
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