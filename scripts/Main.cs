using Godot;

/// <summary>
/// Our entry point after our autoloads (Global) finish loading
/// </summary>
public partial class Main : Node
{

    public override void _Ready()
    {
        //Create the Network Manager and add it to the scene tree so it can tick per frame
        Global.network = new();
        Global.network.Name = "NetworkManager";
        AddChild(Global.network);

        //Create the game Console and add it to the scene tree so it can reference Godot information
        Console console = new Console();
        console.Name = "InGameConsole";
        AddChild(console);

        Global.world = new World();
        Global.world.Name = "world";
        AddChild(Global.world);
        
    }

    

}
