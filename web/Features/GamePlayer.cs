using System.Collections.Concurrent;

public class GamePlayer
{
  private IGameService gameService;
  public MarsMap? Map { get; set; } = null;
  public PerserveranceRover Rover { get; private set; }
  public List<PerserveranceRover> Rovers { get; private set; } = new();
  public List<IngenuityCopter> Copters { get; set; } = new();

  public int CopterCount { get; set; } = 3;

  public GamePlayer(IGameService gameService)
  {
    this.gameService = gameService;
  }

  public async Task Register(string gameId, string name = "Test_Alex")
  {
    System.Console.WriteLine("Registering");
    gameService.GameId = gameId;

    for (int i = 0; i < CopterCount; i++)
    {
      (PerserveranceRover rover, IngenuityCopter copter) = await registerCopterAndRover(name);
      Rovers.Add(rover);
      Copters.Add(copter);

    }

    Rovers.ForEach(r => r.CalculateDetailedPath(optimize: true));
    Rover = Rovers.MinBy(r => r.StartingPathCost) ?? throw new Exception("No rovers to choose from");

    System.Console.WriteLine("Registered for game");
  }

  public void PlayGame()
  {
    var roverThread = PlayRover();

    var copterThreads = PlayCopter();

    roverThread.Join();
    copterThreads.ToList().ForEach(t => t.Join());
  }

  private async Task<(PerserveranceRover rover, IngenuityCopter copter)> registerCopterAndRover(string name)
  {
    var response = await gameService.JoinGame(name);
    if (Map == null)
      Map = new MarsMap(response.LowResolutionMap, response.Neighbors);
    else
      Map.UpdateGridWithNeighbors(response.Neighbors);

    var target = (response.TargetX, response.TargetY);
    var start = (response.StartingX, response.StartingY);

    var rover = new PerserveranceRover(
      gameService: gameService,
      map: Map,
      start: start,
      target: target,
      orientation: response.Orientation,
      token: response.Token
    );

    var copter = new IngenuityCopter(
      gameService: gameService,
      map: Map,
      start: start,
      token: response.Token
    );

    var roverCopter = (rover, copter);
    return roverCopter;
  }

  public Thread PlayRover()
  {
    Rover.OptimizeGrid();
    if (Rover.Path.Count() == 0)
      throw new Exception("Cannot play game if path is empty");

    var t = new Thread(async () =>
    {
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
    });
    t.Start();
    return t;
  }

  public IEnumerable<Thread> PlayCopter()
  {
    var freeCopters = Copters.Where(c => c.Token != Rover.Token);
    var closeCotper = Copters.Where(c => c.Token == Rover.Token).First();

    var copter1 = freeCopters.ElementAt(0);
    var copter2 = freeCopters.ElementAt(1);

    var sleepConstant = 100;

    var tclose = new Thread(async () =>
    {
      while (closeCotper.Location != Rover.Target)
      {
        await closeCotper.FollowPath(Rover.Path, Rover.CurrentLocation);
        Thread.Sleep(200);
      }
    });

    var t1 = new Thread(async () =>
    {
      while (true)
      {
        while (copter1.Location != Rover.Target)
        {
          await copter1.FollowPath(Rover.Path, Rover.CurrentLocation);
          // Thread.Sleep(sleepConstant);
        }

        while (copter1.Location != Rover.CurrentLocation)
        {
          await copter1.TakeStepToTarget(Rover.CurrentLocation);
          // Thread.Sleep(sleepConstant);
        }
      }
    });
    var t2 = new Thread(async () =>
    {
      while (true)
      {
        await copter2.TakeStepToTarget(Rover.CurrentLocation);
        // Thread.Sleep(sleepConstant);
      }
    });

    tclose.Start();
    t1.Start();
    t2.Start();

    return new Thread[] {
      tclose,
      t1,
      t2
    };
  }

}
