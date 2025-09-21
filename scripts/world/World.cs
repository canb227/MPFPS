using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[GlobalClass]
public partial class World : Node3D
{
    private MultiplayerSpawner StaticLevelSpawner;
    private Node3D StaticLevel;

    private Node3D Players;

    private MultiplayerSpawner EntitySpawner;
    private Node3D Entities;

    private Dictionary<string, string> RegisteredStaticLevels = new()
    {
        { "Friendly Level Name", "Godot res:// path of the level's scene" },

        { "platform","res://scenes/world/debugPlatform.tscn" },
        { "flat" , "res://scenes/world/debugFlat.tscn" },











    };

    private Dictionary<string, string> RegisteredEntities = new()
    {
        { "Friendly Entity Name", "Godot res:// path of the entity's scene" },
        { "ball", "res://scenes/Entities/ball.tscn" },
    };

    public override void _Ready()
    {
        Players = new();
        Players.Name = "Players";
        AddChild(Players);

        InitStaticLevelSpawner();
        InitEntitySpawner();
    }

    public void ChangeLevel(string levelName, bool hardReset = false)
    {
        if (!Multiplayer.IsServer())
        {
            Logging.Warn("I am a Non-server that is trying to change the level! Are you sure you're doing this right?", "World");
        }
        if (RegisteredStaticLevels.ContainsKey(levelName))
        {
            if (hardReset)
            {
                throw new NotImplementedException();
            }
            else
            {
                foreach (Node n in StaticLevel.GetChildren())
                {
                    StaticLevel.RemoveChild(n);
                    n.QueueFree();
                }
                StaticLevel.CallDeferred(MethodName.AddChild, GD.Load<PackedScene>(RegisteredStaticLevels[levelName]).Instantiate());
            }
        }
        else
        {
            Logging.Error($"Can't change the level: No level with name {levelName} is registered!", "World");
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SpawnPlayer(int id)
    {
        Player player = ResourceLoader.Load<PackedScene>("res://scenes/Entities/player/Player.tscn").Instantiate<Player>();
        player.playerID = id;
        player.Name = id.ToString();
        Players.AddChild(player);
    }

    public void SpawnEntityByName(string name)
    {
        if (!Multiplayer.IsServer())
        {
            Logging.Warn("I am a Non-server that is trying to Spawn an entity! Are you sure you're doing this right?", "World");
        }
        if (RegisteredEntities.ContainsKey(name))
        {
            Entities.CallDeferred(MethodName.AddChild, [GD.Load<PackedScene>(RegisteredEntities[name]).Instantiate(),true]);
        }
        else
        {
            Logging.Error($"Can't change the level: No level with name {name} is registered!", "World");
        }
    }

    public void SpawnEntity(Node3D entity)
    {
        if (!Multiplayer.IsServer())
        {
            Logging.Warn("I am a Non-server that is trying to spawn an entity! Are you sure you're doing this right?", "World");
        }
        Entities.CallDeferred(MethodName.AddChild,[entity,true]);
    }

    public Vector3 GetRandomSpawnLocation(int teamID)
    {
        Random rng = new Random();
        int height = rng.Next(1, 10);
        return new Vector3(0, height, 0);
    }

    private void InitStaticLevelSpawner()
    {
        StaticLevel = new();
        StaticLevel.Name = "StaticLevel";
        AddChild(StaticLevel);

        StaticLevelSpawner = new();
        StaticLevelSpawner.Name = "StaticLevelSpawner";
        AddChild(StaticLevelSpawner);
        StaticLevelSpawner.SpawnPath = StaticLevelSpawner.GetPathTo(StaticLevel);
        StaticLevelSpawner.Spawned += SLSpawner_Spawned;
        StaticLevelSpawner.Despawned += SLSpawner_Despawned;

        foreach(string path in RegisteredStaticLevels.Values)
        {
            StaticLevelSpawner.AddSpawnableScene(path);
        }
    }
    private void SLSpawner_Despawned(Node node)
    {
        Logging.Log($"Authority just despawned static level!", "World");
    }

    private void SLSpawner_Spawned(Node node)
    {
        Logging.Log($"Authority just spawned static level!", "World");
    }

    private void InitEntitySpawner()
    {
        Entities = new();
        Entities.Name = "Entities";
        AddChild(Entities);

        EntitySpawner = new();
        EntitySpawner.Name = "EntitySpawner";
        AddChild(EntitySpawner);
        EntitySpawner.SpawnPath = EntitySpawner.GetPathTo(Entities);
        EntitySpawner.Spawned += ESpawner_Spawned;
        EntitySpawner.Despawned += ESpawner_Despawned;

        foreach (string path in RegisteredEntities.Values)
        {
            EntitySpawner.AddSpawnableScene(path);
        }
    }

    private void ESpawner_Despawned(Node node)
    {
        Logging.Log($"Authority just despawned entity!", "World");
    }

    private void ESpawner_Spawned(Node node)
    {
        Logging.Log($"Authority just spawned entity!", "World");
    }
}

