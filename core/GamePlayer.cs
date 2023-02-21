using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json;

public class GamePlayer
{
  private IGameService gameService;
  public MarsMap? Map { get; set; } = null;
  public PerserveranceRover Rover { get; private set; }
  public List<PerserveranceRover> Rovers { get; private set; } = new();
  public List<IngenuityCopter> Copters { get; set; } = new();
  public bool Turbo { get; set; } = false;

  public bool Playing { get; set; } = false;

  public int CopterCount { get; set; } = 10;

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
      (PerserveranceRover rover, IngenuityCopter copter) =
        await registerCopterAndRover(name);
      Rovers.Add(rover);
      Copters.Add(copter);
    }

    var threads = Rovers.Select(r =>
    {
      var t = new Thread(() => r.CalculateDetailedPath(optimize: true));
      t.Start();
      return t;
    });

    threads.ToList().ForEach(t => t.Join());

    Rover =
      Rovers.MinBy(r => r.StartingPathCost)
      ?? throw new Exception("No rovers to choose from");

    System.Console.WriteLine("Registered for game");
  }

  public void PlayGame()
  {
    Playing = true;

    new Thread(() =>
    {
      var roverThread = PlayRover();
      var copterThreads = PlayCopter();
    }).Start();
  }

  private async Task<(
    PerserveranceRover rover,
    IngenuityCopter copter
  )> registerCopterAndRover(string name)
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
        var fullTimer = System.Diagnostics.Stopwatch.StartNew();
        var (start, end, cost, time) = await Rover.Take1Step();

        if (!Map.IsAnEdge(Rover.CurrentLocation) && !Turbo)
        {
          if (Rover.History.Count() % 3 == 0)
            Rover.CalculateDetailedPath();

          Task.Run(() =>
          {
            if (Rover.History.Count() % 10 == 0)
              Rover.OptimizeGrid(reset: true);
            else
              Rover.OptimizeGrid();
          });
        }

        fullTimer.Stop();
        var fullTime = (int)fullTimer.ElapsedMilliseconds;
        System.Console.WriteLine(
          $"{start.ToString().PadLeft(8, ' ')} -> {end.ToString().PadLeft(8, ' ')}, cost: {cost.ToString().PadLeft(3, ' ')},"
            + $" time: {fullTime.ToString().PadLeft(4, ' ')},"
            + $" request time: {time.ToString().PadLeft(4, ' ')} ms"
        );
      }
    });
    t.Start();
    return t;
  }

  public IEnumerable<Thread> PlayCopter()
  {
    var closeCotper = Copters.Where(c => c.Token == Rover.Token).First();
    var freeCopters = Copters
      .Where(c => c.Token != Rover.Token)
      .OrderBy(
        c => IngenuityCopter.DistanceToTarget(Rover.CurrentLocation, c.Location)
      );

    var copter1 = freeCopters.ElementAt(0);
    var copter2 = freeCopters.ElementAt(1);
    var restOfCopters = freeCopters.Skip(2);

    var followPathThenStop = new Thread(async () =>
    {
      while (closeCotper.Location != Rover.Target)
      {
        await closeCotper.TakeBestStepOnPath(Rover.GetPath());
        Thread.Sleep(200);
      }
    });

    var followPathThenDoItAgain = new Thread(async () =>
    {
      while (true)
      {
        while (copter1.Location != Rover.CurrentLocation)
        {
          await copter1.TakeStepToTarget(Rover.GetCurrentLocation());
        }

        while (copter1.Location != Rover.Target)
        {
          await copter1.TakeBestStepOnPath(Rover.GetPath());
        }
      }
    });
    var roverFollowerThread = new Thread(async () =>
    {
      while (true)
      {
        await copter2.TakeStepToTarget(Rover.GetCurrentLocation());
      }
    });

    var pointsGroupedForCopters = groupPointsIntoCopterPaths(restOfCopters);

    var otherThreads = pointsGroupedForCopters.Select(
      (paths, i) =>
      {
        var t = new Thread(async () =>
        {
          var c = restOfCopters.ElementAt(i);

          foreach (var originalPath in paths)
          {
            var startIsClosest =
              IngenuityCopter.DistanceToTarget(originalPath.First(), c.Location)
              < IngenuityCopter.DistanceToTarget(
                originalPath.Last(),
                c.Location
              );

            var bestPath = startIsClosest
              ? originalPath
              : originalPath.Reverse();
            var startOfPath = bestPath.First();

            while (startOfPath != c.Location)
            {
              await c.TakeStepToTarget(startOfPath);
            }

            var target = bestPath.Last();

            while (c.Location != target)
            {
              await c.TakeStepToTarget(target);
            }
          }
        });
        return t;
      }
    );

    var copterThreads = new Thread[]
    {
      followPathThenStop,
      followPathThenDoItAgain,
      roverFollowerThread
    }.Concat(otherThreads);
    // var copterThreads = new Thread[] { otherThreads.First() };
    // var copterThreads = otherThreads;

    foreach (var t in copterThreads)
    {
      t.Start();
    }
    return copterThreads;
  }

  private IEnumerable<
    IEnumerable<IEnumerable<(int x, int y)>>
  > groupPointsIntoCopterPaths(IEnumerable<IngenuityCopter> restOfCopters)
  {
    var myBuffer = 20;
    (int x, int y) bottomLeft = getBottomLeft(myBuffer);
    (int x, int y) topRight = getTopRight(myBuffer);

    var xPointsCount = (topRight.x - bottomLeft.x) / 10;
    var yPointsCount = (topRight.y - bottomLeft.y) / 10;

    var xPoints = Enumerable
      .Range(0, xPointsCount + 1)
      .Select(x => (x * 10) + bottomLeft.x);

    var yPoints = Enumerable
      .Range(0, yPointsCount + 1)
      .Select(y => (y * 10) + bottomLeft.y);

    var pointsInRows = xPoints.Select(
      x =>
        yPoints.Select(y =>
        {
          return (x, y);
        })
    );

    var copterCount = restOfCopters.Count();
    var pointRowCount = pointsInRows.Count();
    var pointsPerGroup = (pointRowCount / copterCount) + 1;

    var pointsGroupedForCopters = pointsInRows
      .Select((p, i) => new { Index = i, Point = p })
      .GroupBy(x => x.Index / pointsPerGroup)
      .Select(x => x.Select(v => v.Point));

    if (pointsGroupedForCopters.Count() != restOfCopters.Count())
      System.Console.WriteLine(
        $"Warning, have group points into {pointsGroupedForCopters.Count()} groups, but only have {restOfCopters.Count()} copters"
      );
    return pointsGroupedForCopters;
  }

  private (int x, int y) getTopRight(int myBuffer)
  {
    var highX =
      Rover.StartingLocation.x > Rover.Target.y
        ? Rover.StartingLocation.x
        : Rover.Target.x;
    var highY =
      Rover.StartingLocation.y > Rover.Target.y
        ? Rover.StartingLocation.y
        : Rover.Target.y;
    var inBoundsHighX =
      highX + myBuffer > Map.TopRight.x ? highX + myBuffer : Map.TopRight.x;
    var inBoundsHighY =
      highY + myBuffer < Map.TopRight.y ? highY + myBuffer : Map.TopRight.y;
    var topRight = (x: inBoundsHighX, y: inBoundsHighY);
    return topRight;
  }

  private (int x, int y) getBottomLeft(int myBuffer)
  {
    var lowX =
      Rover.StartingLocation.x < Rover.Target.x
        ? Rover.StartingLocation.x
        : Rover.Target.x;

    var lowY =
      Rover.StartingLocation.y < Rover.Target.y
        ? Rover.StartingLocation.y
        : Rover.Target.y;

    var inBoundsLowX = lowX - myBuffer > 0 ? lowX - myBuffer : 0;
    var inBoundsLowY = lowY - myBuffer > 0 ? lowY - myBuffer : 0;

    var bottomLeft = (x: inBoundsLowX, y: inBoundsLowY);
    return bottomLeft;
  }
}
