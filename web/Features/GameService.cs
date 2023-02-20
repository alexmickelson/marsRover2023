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
  string? Token { get; }

  Task<JoinResponse> JoinGame(string name = "Test_Alex");
  Task<MoveResponse> Move(Direction direction);
  Task<MoveResponse> MoveIngenuity(int row, int col);
}

public class GameService : IGameService
{
  private RestClient client { get; }
  public string? GameId { get; set; }
  public string Name { get; private set; }
  public string ServerAddress { get; init; }
  public string? Token { get; private set; }

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
    Token = response.Data.Token;
    return response.Data;
  }

  public async Task<MoveResponse> Move(Direction direction)
  {
    var joinUrl = $"/game/moveperseverance?token={Token}&direction={direction}";
    var request = new RestRequest(joinUrl);
    var response = await client.ExecuteGetAsync<MoveResponse>(request);

    if (gameNotStarted(response))
    {
      System.Console.WriteLine("Not ready to play yet");
      Thread.Sleep(100);
      return await Move(direction);
      // response = await client.ExecuteGetAsync<MoveResponse>(request);
    }

    if (isRateLimited(response) || isOutOfBattery(response))
    {
      var sleepTime = 1000;
      if (isRateLimited(response))
      {
        System.Console.WriteLine(response.Data);
        System.Console.WriteLine("Got rate limited, sleeping");
        sleepTime = 300;
      }
      else
        System.Console.WriteLine("not enough battery, sleeping");

      Thread.Sleep(sleepTime);

      return await Move(direction);
    }

    handleBadMoveResponse(response);
    if (!response.Data.Message.ToLower().Contains(" ok"))
      System.Console.WriteLine(response.Data.Message);
    return response.Data;
  }


  private static bool gameNotStarted(RestResponse<MoveResponse> response)
  {
    return response.StatusCode == System.Net.HttpStatusCode.BadRequest
      && response.Content != null
      && response.Content.Contains("Game not in the Playing state.");
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
    return response.IsSuccessful
      && response.Data != null
      && response.Data.Message.ToLower().Contains("recharge");
  }

  private static bool isRateLimited(RestResponse<MoveResponse> response)
  {
    return response.StatusCode == System.Net.HttpStatusCode.TooManyRequests;
  }

  public async Task<MoveResponse> MoveIngenuity(int x, int y)
  {
    var joinUrl =
      $"/game/moveingenuity?token={Token}&destinationRow={x}&destinationColumn={y}";
    var request = new RestRequest(joinUrl);

    var response = await client.ExecuteGetAsync<MoveResponse>(request);

    if (gameNotStarted(response))
    {
      System.Console.WriteLine("Not ready to play yet");
      Thread.Sleep(100);
      return await MoveIngenuity(x, y);
      // response = await client.ExecuteGetAsync<MoveResponse>(request);
    }

    if (isRateLimited(response) || isOutOfBattery(response))
    {
      var sleepTime = 1000;
      if (isRateLimited(response))
      {
        System.Console.WriteLine(response.Data);
        System.Console.WriteLine("Got rate limited, sleeping");
        sleepTime = 300;
      }
      else
        System.Console.WriteLine("not enough battery, sleeping");

      Thread.Sleep(sleepTime);

      return await MoveIngenuity(x, y);
    }

    handleBadMoveResponse(response);
    if (!response.Data.Message.ToLower().Contains(" ok"))
      System.Console.WriteLine(response.Data.Message);
    return response.Data;
  }
}
