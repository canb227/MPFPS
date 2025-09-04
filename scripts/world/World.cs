using GameMessages;
using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


public partial class World : Node3D
{

    public Dictionary<ulong, GameObject> entities = new();
    public Dictionary<ulong, Controller> controllers = new();

    public ulong defaultAuthority;

    public Vector3 defaultSpawnLocation = new Vector3(0,0,0);

    private ulong tick = 0;
    private Node meta = null;
    private Node3D root = null;

    public override void _Ready()
    {
        Global.world = this;
        Logging.Log($"Game world ready for commands.", "World");
        SetPhysicsProcess(false);
        entities[0] = new GameObject();
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


    internal void LoadWorld()
    {
        if (Global.GameSession.sessionOptions.DEBUG_DIRECTLOADMAP)
        {
            int mapIndex = Global.GameSession.sessionOptions.DEBUG_DIRECTLOADMAPINDEX;
            string scenePath = DebugScreen.directLoadMap_mapPaths[mapIndex];           
            LoadRootSceneByPath(scenePath);
        }
    }

    internal void InGameStart()
    {
        
    }

    internal ulong AssignNewID()
    {
        Random rng = new Random();
        ulong id = 0;
        while (entities.ContainsKey(id))
        {
            id = (ulong)rng.NextInt64();
        }
        return id;
    }

    internal void SpawnGameObject(SpawnGameObjectData msg)
    {
        GameObject obj = GameObjectLoader.LoadGameObjectInstance(msg.ObjName);
        obj.SetUID(msg.WithID);
        obj.Name = msg.ObjName;
        entities[obj.GetUID()] = obj;
        obj.Position = new Vector3(msg.SpawnX, msg.SpawnY, msg.SpawnZ);
        AddChild(obj);
    }
}