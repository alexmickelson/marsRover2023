using static System.Linq.Enumerable;


var service = new GameService();

var game = new GamePlayer(service);


var gameId = args[0];

System.Console.WriteLine($"Registering game: {gameId}");
await game.Register(gameId, "Alex");

game.Verbose = true;
game.PlayGame();

while (true)
{
  Thread.Sleep(10000);
}