var game = new Game();

public class Fountain
{
    public bool IsOn { get; private set; }

    public void TurnFountainOn()
    {
        IsOn = true;
    }
}

public class Game
{
    public Player Player { get; }
    public World World { get; private set; }
    public Fountain Fountain { get; }

    private CommandProcessor _commandProcessor;

    public Game()
    {
        _commandProcessor = new CommandProcessor();

        Player = new Player();
        Fountain = new Fountain();

        CreateGame();
        Run();
    }

    private void CreateGame()
    {
        var worldBuilder = new WorldBuilder();
        ConsoleHelper.WriteLine("Enter the size of the game you would like");
        ConsoleHelper.WriteLine("1: Small");
        ConsoleHelper.WriteLine("2: Medium");
        ConsoleHelper.WriteLine("3: Large");

        var size = Convert.ToInt32(ConsoleHelper.ReadLine());
        World = size switch
        {
            1 => worldBuilder.CreateSmallGame(),
            2 => worldBuilder.CreateMediumGame(),
            3 => worldBuilder.CreateLargeGame(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public void Run()
    {
        do
        {
            ConsoleHelper.FillLine('=');
            ConsoleHelper.WriteLine($"You are in the room at (Row = {Player.PosX}, Col = {Player.PosY})");

            _commandProcessor.ProcessCommand("look", this);
            ConsoleHelper.WriteLine("What would you like to do?");
            _commandProcessor.ProcessCommand(Console.ReadLine(), this);
        } while (!CheckWinCondition() && IsAlive());
    }

    private bool CheckWinCondition()
    {
        if (!Fountain.IsOn || World.GetRoomAt(Player.PosX, Player.PosY).RoomType != RoomType.Entrance) return false;

        ConsoleHelper.WriteLine("You Win!");
        return true;
    }

    private bool IsAlive()
    {
        if (!Player.IsAlive)
        {
            ConsoleHelper.WriteLine("You fall in a pit and perish");
            return false;
        }

        return true;
    }
}


public class CommandProcessor
{
    private readonly List<ICommand> _commands = new List<ICommand>()
    {
        new LookCommand(),
        new MoveCommand(),
        new EnableFountain()
    };

    public void ProcessCommand(string userCommand, Game game)
    {
        foreach (var command in _commands)
        {
            command.Execute(userCommand, game);
        }
    }
}

public class WorldBuilder
{
    public World CreateSmallGame()
    {
        return new World(0, 0, 4, 1, 1);
    }

    public World CreateMediumGame()
    {
        return new World(0, 0, 6, 2, 1);
    }

    public World CreateLargeGame()
    {
        return new World(0, 0, 8, 4, 2);
    }
}

public class World
{
    private readonly int _worldSize;
    private readonly Room[,] _grid;

    public World(int playerStartX, int playerStartY, int worldSize, int numberOfPits, int numberOfMaelstroms)
    {
        _worldSize = worldSize;
        _grid = new Room[_worldSize, _worldSize];

        PopulateGrid(playerStartX, playerStartY, numberOfPits, numberOfMaelstroms);
    }

    public void MoveRoom(Room room, int toX, int toY)
    {
        var roomToBeReplaced = GetRoomWithClamp(toX, toY);
        _grid[room.PosX, room.PosY] = new Room(room.PosX, room.PosY, RoomType.Empty);
        _grid[roomToBeReplaced.PosX, roomToBeReplaced.PosY] = room;
        
    }

    public List<RoomType> GetAdjacentDangers(int x, int y)
    {
        var adjacentRooms = new List<Room>
        {
            GetRoomAt(x, y - 1), //North
            GetRoomAt(x + 1, y - 1), //NorthEast
            GetRoomAt(x + 1, y), //East
            GetRoomAt(x + 1, y + 1), //SouthEast
            GetRoomAt(x, y + 1), //South
            GetRoomAt(x - 1, y - 1), //SouthWest
            GetRoomAt(x - 1, y), //West
            GetRoomAt(x - 1, y + 1) //NorthWest
        };

        return adjacentRooms
            .Where(r=>r != null)
            .Where(r => r.RoomType != RoomType.Empty)
            .Select(r => r.RoomType)
            .ToList();
    }

    public Room GetRoomAt(int x, int y)
    {
        if (x > _grid.GetLength(0) - 1 || x < 0) return null;
        if (y > _grid.GetLength(1) - 1 || y < 0) return null;

        return _grid[x, y];
    }

    public Room GetRoomWithClamp(int x, int y)
    {
        if (x < 0) x = 0;
        if (x > _worldSize) x = _worldSize - 1;
        
        if (y < 0) y = 0;
        if (y > _worldSize) y = _worldSize - 1;

        return _grid[x, y];
    }

    private void CreateRoomAtRandomLocation(RoomType roomType)
    {
        var random = new Random();
        int x = 0;
        int y = 0;

        while (_grid[x, y] != null || (x == 0 && y == 0))
        {
            x = random.Next(0, _worldSize);
            y = random.Next(0, _worldSize);
        }

        _grid[x, y] = new Room(x, y,roomType);
    }

    private void PopulateGrid(int playerStartX, int playerStartY, int numberOfPits, int numberOfMaelstroms)
    {
        var startingRoom = new Room(playerStartX, playerStartY, RoomType.Entrance);
        CreateRoomAtRandomLocation(RoomType.Fountain);

        for (int i = 0; i < numberOfPits; i++)
        {
            CreateRoomAtRandomLocation(RoomType.Pit);
        }
        
        for(int i=0; i< numberOfMaelstroms; i++)
            CreateRoomAtRandomLocation(RoomType.Maelstrom);

        _grid[playerStartX, playerStartY] = startingRoom;

        for (int row = 0; row < _grid.GetLength(0); row++)
        for (int col = 0; col < _grid.GetLength(1); col++)
        {
            if (_grid[row, col] != null)
                continue;

            _grid[row, col] = new Room(row, col, RoomType.Empty);
        }
    }
}

public record Room(int PosX, int PosY, RoomType RoomType);


public class Player()
{
    public int PosX { get; private set; } = 0;
    public int PosY { get; private set; } = 0;

    public bool IsAlive { get; private set; } = true;

    public bool Move(World gameWorld, Direction direction)
    {
        var newRoomPosition = direction switch
        {
            Direction.North => (PosY - 1, PosX),
            Direction.East => (PosY, PosX + 1),
            Direction.South => (PosY + 1, PosX),
            Direction.West => (PosY, PosX - 1)
        };

        var newRoom = gameWorld.GetRoomAt(newRoomPosition.Item2, newRoomPosition.Item1);

        if (newRoom == null)
            return false;

        switch (newRoom.RoomType)
        {
            case RoomType.Pit:
                IsAlive = false;
                break;
            case RoomType.Maelstrom:
                newRoom = gameWorld.GetRoomWithClamp(PosX + 1, PosY - 2);
                gameWorld.MoveRoom(gameWorld.GetRoomAt(PosX, PosY), PosX - 2, PosY + 1);
                break;
        }

        PosY = newRoom.PosY;
        PosX = newRoom.PosX;

        return true;
    }
}

public enum RoomType
{
    Empty,
    Fountain,
    Entrance,
    Pit,
    Maelstrom
}

public interface ISense
{
    public void DisplaySense(Game game);
}

public class Hear : ISense
{
    public void DisplaySense(Game game)
    {
        if (game.World.GetRoomAt(game.Player.PosX, game.Player.PosY).RoomType != RoomType.Fountain) return;

        ConsoleHelper.WriteLine(game.Fountain.IsOn
            ? "You hear the rushing waters from the Fountain of Objects. It has been reactivated!"
            : "You hear water dripping in this room. The Fountain of Objects is here!");
    }
}

public class See : ISense
{
    public void DisplaySense(Game game)
    {
        if (game.World.GetRoomAt(game.Player.PosX, game.Player.PosY).RoomType != RoomType.Entrance) return;

        ConsoleHelper.WriteLine("You see light in this room coming from outside the cavern. This is the entrance.");
    }
}

public class Feel : ISense
{
    public void DisplaySense(Game game)
    {
        var adjacentDangers = game.World.GetAdjacentDangers(game.Player.PosX, game.Player.PosY);

        if (adjacentDangers.Contains(RoomType.Pit))
        {
            ConsoleHelper.WriteLine("You feel a draft. There is a pit in a nearby room.");
        }
    }
}

public class MaelstromSense : ISense
{
    public void DisplaySense(Game game)
    {
        var adjacentDangers = game.World.GetAdjacentDangers(game.Player.PosX, game.Player.PosY);

        if (adjacentDangers.Contains(RoomType.Maelstrom))
        {
            ConsoleHelper.WriteLine("You hear the growling and groaning of a maelstrom nearby.");
        }
    }
}

public class Smell : ISense
{
    public void DisplaySense(Game game)
    {
    }
}


public interface ICommand
{
    public void Execute(string userCommand, Game game);
}

public class EnableFountain : ICommand
{
    private readonly string _userCommand = "enable fountain";

    public void Execute(string userCommand, Game game)
    {
        if (userCommand != _userCommand) return;

        game.Fountain.TurnFountainOn();
    }
}

public class MoveCommand : ICommand
{
    private readonly string _userCommand = "move";

    public void Execute(string userCommand, Game game)
    {
        var split = userCommand.Split(" ");
        if (!IsThisCommand(split[0])) return;

        if (split.Length == 1)
        {
            ConsoleHelper.WriteLine("You must enter a direction");
            return;
        }
        else if (split.Length > 2)
        {
            ConsoleHelper.WriteLine("You must enter a single direction to move in");
            return;
        }

        var direction = split[1] switch
        {
            "north" => Direction.North,
            "south" => Direction.South,
            "east" => Direction.East,
            "west" => Direction.West
        };

        var wasValidMove = game.Player.Move(game.World, direction);
        if (!wasValidMove) ConsoleHelper.WriteLine("There is a wall there");
    }

    private bool IsThisCommand(string userCommand)
    {
        return userCommand == _userCommand;
    }
}

public enum Direction
{
    North,
    South,
    East,
    West
}

public class LookCommand : ICommand
{
    private readonly ISense[] _senses = [new Hear(), new See(), new Smell(), new Feel(), new MaelstromSense()];
    private readonly string _userCommand = "look";

    public void Execute(string userCommand, Game game)
    {
        if (!IsThisCommand(userCommand)) return;

        foreach (var sense in _senses)
            sense.DisplaySense(game);
    }

    private bool IsThisCommand(string userCommand)
    {
        return userCommand == _userCommand;
    }
}

public static class ConsoleHelper
{
    public static void FillLine(char characterToFill)
    {
        int currentPosition = 0;
        while (currentPosition <= Console.WindowWidth - 1)
        {
            Console.Write(characterToFill);
            currentPosition++;
        }

        WriteLine();
    }

    public static void WriteLine(string text = "")
    {
        Console.WriteLine(text);
    }

    public static string ReadLine()
    {
        return Console.ReadLine();
    }
}