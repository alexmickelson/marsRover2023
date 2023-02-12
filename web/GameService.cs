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
  string Name { get; init; }
  string ServerAddress { get; init; }
  string? Token { get; }

  Task<JoinResponse> JoinGame();
  Task<MoveResponse> Move(Direction direction);
  Task<MoveResponse> MoveInenuity(int row, int col);
}

public class GameService : IGameService
{
  private RestClient client { get; }
  public string? GameId { get; set; }
  public string Name { get; init; }
  public string ServerAddress { get; init; }
  public string? Token { get; private set; }

  public GameService()
  {
    ServerAddress = "https://snow-rover.azurewebsites.net/";
    Name = "Test_A";
    client = new RestClient(ServerAddress);
  }

  public async Task<JoinResponse> JoinGame()
  {
    var joinUrl = $"/game/join?gameId={GameId}&name={Name}";
    var request = new RestRequest(joinUrl);
    var response = await client.GetAsync<JoinResponse>(request);
    if (response == null)
    {
      System.Console.WriteLine(JsonSerializer.Serialize(response));
      throw new Exception("Could not join game, got null response");
    }
    Token = response.Token;
    return response;
  }

  public async Task<MoveResponse> Move(Direction direction)
  {
    var joinUrl = $"/game/moveperseverance?token={Token}&direction={direction}";
    var request = new RestRequest(joinUrl);
    var response = await client.GetAsync<MoveResponse>(request);

    if (response == null)
    {
      System.Console.WriteLine(JsonSerializer.Serialize(response));
      throw new Exception("Could not move perserverance, got null response");
    }
    return response;
  }

  public async Task<MoveResponse> MoveInenuity(int row, int col)
  {
    var joinUrl = $"/game/moveingenuity?token={Token}&destinationRow={row}&destinationColumn={col}";
    var request = new RestRequest(joinUrl);
    var response = await client.GetAsync<MoveResponse>(request);

    if (response == null)
    {
      System.Console.WriteLine(JsonSerializer.Serialize(response));
      throw new Exception("Could not move ingenuity, got null response");
    }
    return response;
  }
}