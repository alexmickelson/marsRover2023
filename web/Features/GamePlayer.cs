using System.Collections.Concurrent;

public class GamePlayer
{
  private IGameService gameService;
  public MarsMap? Map { get; set; } = null;
  public PerserveranceRover Rover { get; private set; }
  public IngenuityCopter Copter { get; set; }

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

    var target = (response.TargetX, response.TargetY);
    var start = (response.StartingX, response.StartingY);
    var battery = 2000;

    Rover = new PerserveranceRover(
      gameService: gameService,
      map: Map,
      start: start,
      target: target,
      battery: battery,
      orientation: response.Orientation
    );

    Copter = new IngenuityCopter(
      gameService: gameService,
      map: Map,
      start: start
    );

    System.Console.WriteLine("Registered for game");
  }

  public async Task PlayGame()
  {
    Rover.OptimizeGrid();
    if (Rover.Path.Count() == 0)
      throw new Exception("Cannot play game if path is empty");
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

  public async Task PlayCopter()
  {
    var t = new Thread(async () =>
    {
      while (true)
      {
        while (Copter.Location != Rover.Target)
        {
          // await Copter.TakeStepToTarget(Rover.Target);
          await Copter.FollowPath(Rover.Path, Rover.CurrentLocation);
          Thread.Sleep(150);
        }

        while (Copter.Location != Rover.CurrentLocation)
        {
          await Copter.TakeStepToTarget(Rover.CurrentLocation);
          Thread.Sleep(100);
        }
      }
    });
    t.Start();
  }
}
