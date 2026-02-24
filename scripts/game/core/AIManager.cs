using Godot;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;

public partial class AIManager : Node3D
{
    public List<HordeAgent> agentPool = new();
    public List<HordeAgent> controlledNPCs = new();
    public GOBasePlayerCharacter localPlayer;
    private GameModeOptions options;
    private Dictionary<Vector3I, List<HordeAgent>> grid = new();
    public List<Vector3> path = new();

    internal void GameStartAsHost()
    {
        Logging.Log($"Starting server-side AI manager", "AIManager");
        options = Global.gameState.gameModeManager.options;
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void SpawnHorde(int hordeSize)
    {
        var hordeSpawnLocation = MapManager.GetHordeSpawnTransform();
        Vector3 targetLocation = new();
        if(evacuationStarted)
        {
            if(Global.gameState.gameModeManager.helicopter != null)
            {
                targetLocation = Global.gameState.gameModeManager.helicopter.GlobalPosition;
            }
            else
            {
                GD.PushError("HELICOPTER IS NULL");
                targetLocation = new Vector3(20, 0, 28); //just for debug
            }
        }
        else
        {
            if(Global.gameState.gameModeManager.generator != null)
            {
                targetLocation = Global.gameState.gameModeManager.generator.GlobalPosition;
            }
            else
            {
                GD.PushError("GENERATOR IS NULL");
                targetLocation = new Vector3(20, 0, 28); //just for debug
            }
        }
        path = CalculatePath(new Vector3(hordeSpawnLocation.Origin.X, hordeSpawnLocation.Origin.Y+1.0f, hordeSpawnLocation.Origin.Z), new Vector3(targetLocation.X, targetLocation.Y+1.0f, targetLocation.Z));

        var agentPoolSnapshot = agentPool.ToList();
        for(int i = 0; i < hordeSize && i < agentPoolSnapshot.Count(); i ++)
        {
            //spawn the agent and set its path
            agentPoolSnapshot[i].SpawnAgent(hordeSpawnLocation.Origin).UpdatePath(path);
        }
    }

    public void CreateAgentPool(int agentPoolCount = 300)
    {
        agentPool.Clear();
        controlledNPCs.Clear();
        for(int i = 0; i < agentPoolCount; i++)
        {
            GameObjectConstructorData data = new(GameObjectType.HordeAgent);
            data.spawnTransform = Transform3D.Identity;
            data.spawnTransform.Origin = new Vector3(0,0,0);
            data.paramList.Add(HordeAgentState.NONE);
            Global.gameState.Auth_SpawnObject(GameObjectType.HordeAgent, data);
        }
    }
    public override void _Ready()
    {
        base._Ready();
    }

    public void NewRound()
    {
        evacuationStarted = false;
        currentHordeCooldown = 10;
        grid = new();
        agentPool = new();
        controlledNPCs = new();
        CreateAgentPool();
    }


    public void UpdateLocalPlayer(GOBasePlayerCharacter localPlayer)
    {
        this.localPlayer = localPlayer;
    }

    public void MoveAgentCell(HordeAgent agent, Vector3I oldCell, Vector3I newCell)
    {
        if (grid.ContainsKey(oldCell))
            grid[oldCell].Remove(agent);

        if (!grid.ContainsKey(newCell))
            grid[newCell] = new List<HordeAgent>();

        grid[newCell].Add(agent);
    }

    public List<Vector3> CalculatePath(Vector3 start, Vector3 goal)
    {
        var navMap = GetWorld3D().NavigationMap;
        var pathPoints = NavigationServer3D.MapGetPath(navMap, start, goal, true);

        return new List<Vector3>(pathPoints);
    }


    public List<HordeAgent> GetNeighbors(HordeAgent agent)
    {
        List<HordeAgent> neighbors = new();
        Vector3I cell = agent.currentCell;

        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++) //we probably could ignore y, but second floors of buildings and such
                for (int dz = -1; dz <= 1; dz++)
                {
                    Vector3I neighborCell = cell + new Vector3I(dx, dy, dz);
                    if (grid.ContainsKey(neighborCell))
                    {
                        foreach (var other in grid[neighborCell])
                        {
                            if (other == agent) continue;
                            float dist = (other.GlobalPosition - agent.GlobalPosition).Length();
                            neighbors.Add(other);
                        }
                    }
                }
        return neighbors;
    }

    public void UpdateAllAgentPaths()
    {
        Vector3 targetPosition = Global.gameState.gameModeManager.generator.GlobalPosition;

        RPCManager.RPC(this, "UpdateAgentsPathOnClient", [targetPosition.X,targetPosition.Z]);
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void UpdateAgentsPathOnClient(float x_start, float z_start, float x, float z)
    {
        path = CalculatePath(new Vector3(x_start, 1.2f, z_start), new Vector3(x, 1.2f, z));
        foreach(var agent in controlledNPCs)
        {
            agent.UpdatePath(path);
        }
    }

    bool evacuationStarted;
    double currentHordeCooldown = 9999;
    double hordeCooldown = 120;
    double evacuationHordeCooldown = 10;
    bool announcedHorde = false;
    int hordeSize = 0;
    public override void _PhysicsProcess(double delta)
    {
        if(Global.gameState.gameModeManager.options.warehouseRobots)
        {
            if(Global.Lobby.bIsLobbyHost)
            {
                currentHordeCooldown -= delta;
                //decide if we want to updatepath and set target and start
                if(currentHordeCooldown <= 30 && currentHordeCooldown > 0 && !announcedHorde && !Global.gameState.gameModeManager.evacuationStarted)
                {
                    Global.gameState.gameModeManager.TriggerSwarmIncomingEvent();
                    announcedHorde = true;
                }
                if(currentHordeCooldown <= 0)
                {
                    Logging.Log("Spawn Horde", "AIManager");
                    //we should play a sound like L4D
                    Global.gameState.gameModeManager.TriggerSwarmStartedEvent();
                    if(Global.gameState.gameModeManager.evacuationStarted)
                    {
                        currentHordeCooldown = evacuationHordeCooldown;
                        hordeSize = 5 + Global.gameState.gameModeManager.numPlayers * 3;
                    }
                    else
                    {
                        currentHordeCooldown = hordeCooldown;
                        hordeSize = 200 + Global.gameState.gameModeManager.numPlayers * 10; //TODO testing
                    }

                    int maxChunkSize = 50;

                    int chunkCount = (hordeSize + maxChunkSize - 1) / maxChunkSize;
                    int chunkSize = hordeSize/chunkCount;
                    for (int i = 0; i < chunkCount; i++)
                    {
                        RPCManager.RPC(this, "SpawnHorde", [chunkSize]);
                    }

                    announcedHorde = false;
                }
            }
        }
    }

    public void EvacuationStarted()
    {
        currentHordeCooldown = 5;
        evacuationStarted = true;
    }
}