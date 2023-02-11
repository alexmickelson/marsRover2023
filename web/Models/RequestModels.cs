
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

public enum Orientation {
  North,
  South,
  East,
  West
}

public record JoinResponse(
  string Token,
  int StartingRow,
  int StartingColumn,
  int TargetRow,
  int TargetColumn,
  Neighbor[] Neighbors,
  LowResolutionMap[] LowResolutionMap,
  Orientation Orientation
);

public record Neighbor(
  int Row,
  int Column,
  int Difficulty
);

public record LowResolutionMap(
  int LowerLeftRow,
  int LowerLeftColumn,
  int UpperRightRow,
  int UpperRightColumn,
  int AverageDifficulty
);