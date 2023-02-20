using RestSharp;
using System.Text.Json;

public enum Direction
{
  Forward,
  Right,
  Left,
  Reverse,
}

public interface IGameService
{
  string? GameId { get; set; }
  string Name { get; }
  string ServerAddress { get; init; }

  Task<JoinResponse> JoinGame(string name = "Test_Alex");
  Task<MoveResponse> Move(string token, Direction direction);
  Task<MoveResponse> MoveIngenuity(string token, int row, int col);
}

public class GameService : IGameService
{
  private RestClient client { get; }
  public string? GameId { get; set; }
  public string Name { get; private set; }
  public string ServerAddress { get; init; }

  public GameService()
  {
    ServerAddress = "https://snow-rover.azurewebsites.net/";
    client = new RestClient(ServerAddress);
  }

  public async Task<JoinResponse> JoinGame(string name = "Test_Alex")
  {
    Name = name;
    var joinUrl = $"/game/join?gameId={GameId}&name={Name}";
    var request = new RestRequest(joinUrl);
    var response = await client.ExecuteGetAsync<JoinResponse>(request);

    if (!response.IsSuccessful || response.Data == null)
    {
      System.Console.WriteLine(JsonSerializer.Serialize(response));
      System.Console.WriteLine(response.Content);
      System.Console.WriteLine(response.StatusCode);
      throw new Exception("Error Joining Game");
    }
    return response.Data;
  }

  public async Task<MoveResponse> Move(string token, Direction direction)
  {
    var joinUrl = $"/game/moveperseverance?token={token}&direction={direction}";
    var request = new RestRequest(joinUrl);
    var response = await client.ExecuteGetAsync<MoveResponse>(request);

    if (gameNotStarted(response))
    {
      Thread.Sleep(100);
      return await Move(token, direction);
    }

    if (isRateLimited(response))
    {
      Thread.Sleep(300);
      return await Move(token, direction);
    }

    if (isOutOfBattery(response))
    {
      Thread.Sleep(1000);
      return await Move(token, direction);
    }

    if (unableToUpdatePlayerExceptionReturned(response))
      return await Move(token, direction);

    handleBadMoveResponse(response);
    if (!response.Data.Message.ToLower().Contains(" ok"))
      System.Console.WriteLine(response.Data.Message);
    return response.Data;
  }

  public async Task<MoveResponse> MoveIngenuity(string token, int x, int y)
  {
    var joinUrl =
      $"/game/moveingenuity?token={token}&destinationRow={x}&destinationColumn={y}";
    var request = new RestRequest(joinUrl);

    var response = await client.ExecuteGetAsync<MoveResponse>(request);

    if (gameNotStarted(response))
    {
      Thread.Sleep(100);
      return await MoveIngenuity(token, x, y);
    }

    if (isRateLimited(response))
    {
      Thread.Sleep(300);
      return await MoveIngenuity(token, x, y);
    }

    if (isOutOfBattery(response))
    {
      Thread.Sleep(1000);
      return await MoveIngenuity(token, x, y);
    }

    if (unableToUpdatePlayerExceptionReturned(response))
      return await MoveIngenuity(token, x, y);

    handleBadMoveResponse(response);
    if (!response.Data.Message.ToLower().Contains(" ok"))
      System.Console.WriteLine(response.Data.Message);
    return response.Data;
  }

  private static bool unableToUpdatePlayerExceptionReturned(
    RestResponse<MoveResponse> response
  )
  {
    var isUnable =
      response.Content != null
      && response.Content.ToLower().Contains("unabletoupdateplayerexception");
    if (isUnable)
      System.Console.WriteLine("Found UnableToUpdatePlayer Exception");
    return isUnable;
  }

  private static bool gameNotStarted(RestResponse<MoveResponse> response)
  {
    var notStarted =
      response.StatusCode == System.Net.HttpStatusCode.BadRequest
      && response.Content != null
      && response.Content.Contains("Game not in the Playing state.");
    // if (notStarted)
    //   System.Console.WriteLine("Not ready to play yet");
    return notStarted;
  }

  private static void handleBadMoveResponse(RestResponse<MoveResponse> response)
  {
    if (!response.IsSuccessful || response.Data == null)
    {
      System.Console.WriteLine(JsonSerializer.Serialize(response));
      System.Console.WriteLine(response.Content);
      System.Console.WriteLine(response.ResponseUri);
      System.Console.WriteLine(response.StatusCode);
      System.Console.WriteLine(response.Data?.Message);
      throw new Exception("Could not move, got null response");
    }
  }

  private static bool isOutOfBattery(RestResponse<MoveResponse> response)
  {
    var outOfBattery =
      response.IsSuccessful
      && response.Data != null
      && response.Data.Message.ToLower().Contains("recharge");
    if (outOfBattery)
    {
      // System.Console.WriteLine("not enough battery, sleeping");
    }
    return outOfBattery;
  }

  private static bool isRateLimited(RestResponse<MoveResponse> response)
  {
    var isLimited =
      response.StatusCode == System.Net.HttpStatusCode.TooManyRequests;
    if (isLimited)
    {
      // System.Console.WriteLine(response.Data);
      // System.Console.WriteLine("Got rate limited, sleeping");
    }
    return isLimited;
  }
}
