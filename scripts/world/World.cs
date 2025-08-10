using Godot;
using NetworkMessages;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;


public partial class World : Node3D
{
    bool IsServer = false;
    Dictionary<ulong,Entity> entities = new();

    private ulong tick = 0;

    public override void _Ready()
    {
        Global.world = this;
        Logging.Log($"Game world ready for commands.","World");
        SetPhysicsProcess(false);
    }

    public void StartWorld()
    {
        SetPhysicsProcess(true);
    }

    public void LoadScene(string sceneName)
    {
        if (Global.SceneLoader.getScenePathFromName(sceneName, out string scenePath)) 
        {
            AddChild(ResourceLoader.Load<PackedScene>(scenePath).Instantiate<Node3D>());
        }
    }

    public override void _Process(double delta)
    {

    }

    public override void _PhysicsProcess(double delta)
    {
        tick++;
    }
     
    public ulong GetTick()
    {
        return tick;
    }
}

