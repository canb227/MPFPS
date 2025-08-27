using Godot;
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

    public ulong defaultAuthority;

    private ulong tick = 0;
    private Node meta = null;
    private Node3D root= null;

    public override void _Ready()
    {
        Global.world = this;
        Logging.Log($"Game world ready for commands.","World");
        SetPhysicsProcess(false);
    }
    
    public Entity FindEntity(ulong eid)
    {
        entities.TryGetValue(eid, out var entity);
        return entity;
    }
    public void StartWorld()
    {
        SetPhysicsProcess(true);
    }

    public void LoadRootSceneByPath(string scenePath)
    {
        root = ResourceLoader.Load<PackedScene>(scenePath).Instantiate<Node3D>();
        AddChild(root);
        meta = root.GetNode<Node>("meta");
    }

    public void LoadRootSceneByName(string sceneName)
    {
        if (Global.SceneLoader.getScenePathFromName(sceneName, out string scenePath)) 
        {
            LoadRootSceneByPath(scenePath);
        }
    }

    public void Tick(double delta)
    {
        tick++;
    }

    public ulong GetTick()
    {
        return tick;
    }

    public void PerFrame(double delta)
    {

    }
    public void SpawnEntityRequest(ulong eid, Vector3 pos)
    {
        if (defaultAuthority==Global.steamid)
        {
            SpawnEntityRequest ser = new SpawnEntityRequest();
            ser.eid = eid;
            ser.pos = pos;
            //Global.GameSession.BroadcastStruct(ser, NetType.ENTITY_SPAWN);
        }
        else
        {
            SpawnEntityRequest ser = new SpawnEntityRequest();
            ser.eid = eid;
            ser.pos = pos;
            //send to server
        }


    }
    internal void StartGame()
    {
        
    }
}

struct SpawnEntityRequest
{
    public ulong eid;
    public Vector3 pos;
}