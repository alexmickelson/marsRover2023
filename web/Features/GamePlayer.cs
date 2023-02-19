using System.Collections.Concurrent;
using static System.Linq.Enumerable;

public class GamePlayer
{
  private IGameService gameService;
  public MarsMap? Map { get; set; } = null;
  public PerserveranceRover Rover { get; private set; }
  public (int, int) Target { get; private set; } = default;

  public GamePlayer(IGameService gameService)
  {
    this.gameService = gameService;
  }

  public async Task Register(string gameId, string name = "Test_Alex")
  {
    System.Console.WriteLine("Registering");
    gameService.GameId = gameId;
    var response = await gameService.JoinGame(name);

    Map = new MarsMap(response.LowResolutionMap, response.Neighbors);

    Target = (response.TargetX, response.TargetY);

    Rover = new PerserveranceRover(gameService, Map, Target);
    Rover.Battery = 2000;
    Rover.Orientation = response.Orientation;
    Rover.CurrentLocation = (response.StartingX, response.StartingY);
    Rover.StartingLocation = Rover.CurrentLocation;

    System.Console.WriteLine("Registered for game");
  }

  public async Task PlayGame()
  {
    Rover.CalculateDetailedPath();
    Rover.OptimizeGrid();
    while (true)
    {
      var (start, end, cost, time) = await Rover.Take1Step();
      System.Console.WriteLine(
        $"{start} -> {end}, cost: {cost}, time: {time} ms"
      );
      if (!Map.IsAnEdge(Rover.CurrentLocation))
      {
        Rover.CalculateDetailedPath();
        Rover.OptimizeGrid();
      }
    }
  }
}
