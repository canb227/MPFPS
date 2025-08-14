using Godot;

/// <summary>
/// Our entry point after our autoloads (Global) finish loading
/// </summary>
public partial class Main : Node
{

    public override void _Ready()
    {

        //Create the game Console and add it to the scene tree so it can reference Godot information
        Console console = new Console();
        console.Name = "InGameConsole";
        AddChild(console);

        //Start the network up and add it to the scene tree so it ticks once per frame 
        Global.snetwork = new SimpleNetworking();
        AddChild(Global.snetwork);

        //Fire up the game world and add it to the scenetree to organize  3d nodes
        Global.world = new World();
        Global.world.Name = "World";
        AddChild(Global.world);

    }
    public override void _Process(double delta)
    {
        
    }

    public override void _Notification(int what)
    {
        //app gets a close request (like hitting the (X) button in windows)
        //Does not fire if process gets killed - you cant really do anything about that
        if (what == NotificationWMCloseRequest)
        {
            //lobal.network.NetworkCleanup();
            GetTree().Quit(); // default behavior
        }
    }

    

}
