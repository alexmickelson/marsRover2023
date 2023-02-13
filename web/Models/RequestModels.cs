
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Text.Json;


public record MoveResponse(
  [property: JsonPropertyName("row")]
  int X,
  [property: JsonPropertyName("column")]
  int Y,
  int BatteryLevel,
  Neighbor[] Neighbors,
  string Message,
  string Orientation
);

public record IngenuityMoveResponse(
  [property: JsonPropertyName("row")]
  int X,
  [property: JsonPropertyName("column")]
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
  [property: JsonPropertyName("startingRow")]
  int StartingX,
  [property: JsonPropertyName("startingColumn")]
  int StartingY,
  [property: JsonPropertyName("targetRow")]
  int TargetX,
  [property: JsonPropertyName("targetColumn")]
  int TargetY,
  Neighbor[] Neighbors,
  LowResolutionMap[] LowResolutionMap,
  [property: RegularExpression("North|South|East|West")]
  string Orientation
);

public record Neighbor(
  [property: JsonPropertyName("row")]
  int X,
  [property: JsonPropertyName("column")]
  int Y,
  int Difficulty
);

public record LowResolutionMap(
  [property: JsonPropertyName("lowerLeftRow")]
  int LowerLeftX,
  [property: JsonPropertyName("lowerLeftColumn")]
  int LowerLeftY,
  [property: JsonPropertyName("upperRightRow")]
  int UpperRightX,
  [property: JsonPropertyName("upperRightColumn")]
  int UpperRightY,
  int AverageDifficulty
);