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
  Task<MoveResponse> MoveInenuity(int row, int col);
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
    response = await sleepIfBatteryLowOrRateLimited(request, response);

    if (!response.IsSuccessful || response.Data == null)
    {
      System.Console.WriteLine(JsonSerializer.Serialize(response));
      System.Console.WriteLine(response.Content);
      System.Console.WriteLine(response.StatusCode);
      System.Console.WriteLine(response.Data?.Message);
      throw new Exception("Could not move perserverance, got null response");
    }
    if (!response.Data.Message.ToLower().Contains(" ok"))
      System.Console.WriteLine(response.Data.Message);
    return response.Data;
  }

  private async Task<RestResponse<MoveResponse>> sleepIfBatteryLowOrRateLimited(
    RestRequest request,
    RestResponse<MoveResponse> response
  )
  {
    while (
      response.StatusCode == System.Net.HttpStatusCode.TooManyRequests
      || (
        response.Data != null
        && response.Data.Message.ToLower().Contains("recharge")
      )
    )
    {
      var sleepTime = 1000;
      if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
      {
        System.Console.WriteLine("Got rate limited, sleeping");
        sleepTime = 300;
      }
      else
        System.Console.WriteLine("not enough battery, sleeping");

      Thread.Sleep(sleepTime);
      response = await client.ExecuteGetAsync<MoveResponse>(request);
    }

    return response;
  }

  public async Task<MoveResponse> MoveInenuity(int row, int col)
  {
    var joinUrl =
      $"/game/moveingenuity?token={Token}&destinationRow={row}&destinationColumn={col}";
    var request = new RestRequest(joinUrl);
    var response = await client.ExecuteGetAsync<MoveResponse>(request);

    if (!response.IsSuccessful || response.Data == null)
    {
      System.Console.WriteLine(JsonSerializer.Serialize(response));
      System.Console.WriteLine(response.Content);
      System.Console.WriteLine(response.StatusCode);
      System.Console.WriteLine(response.Data?.Message);
      throw new Exception("Could not move ingenuity, got null response");
    }
    return response.Data;
  }
}
