using Godot;
using Steamworks;
using System;
using System.Collections.Generic;
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

    public int hordeSize = 100;
    public void SpawnHorde()
    {
        var hordeSpawnLocation = MapManager.GetHordeSpawnTransform();

        var agentPoolSnapshot = agentPool.ToList();
        for(int i = 0; i < hordeSize && i < agentPoolSnapshot.Count(); i ++)
        {
            agentPoolSnapshot[i].SpawnAgent(hordeSpawnLocation.Origin);
        }
    }

    public void CreateAgentPool(int agentPoolCount = 200)
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

    float oldX = 0;
    float oldZ = 0;
    public void UpdateAllAgentPaths()
    {
        //need to actually set start and target
        // Random rand = new Random();
        // float x = (float)(rand.NextDouble() * 80 - 40);
        // float z = (float)(rand.NextDouble() * 80 - 40);
        var livingPlayers = Global.gameState.gameModeManager.basicPlayers
            .Where(p => p.Value.state == CharacterState.Living)
            .Select(p => p.Value)
            .ToList();

        BasicPlayerCharacter chosen = null;
        if (livingPlayers.Count > 0)
        {
            var rand = new Random();
            int index = rand.Next(livingPlayers.Count);
            chosen = livingPlayers[index];
        }

        RPCManager.RPC(this, "UpdateAgentPathOnClient", [chosen.GlobalPosition.X,chosen.GlobalPosition.Z]);
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void UpdateAgentPathOnClient(float x, float z)
    {
        path = CalculatePath(new Vector3(oldX, 1, oldZ), new Vector3(x, 1.2f, z));
        oldX = x;
        oldZ = z;
        foreach(var agent in controlledNPCs)
        {
            agent.UpdatePath(path);
        }
    }


    double updatePathTimer = 10;
    double pathUpdateWaitTime = 60;
    public override void _PhysicsProcess(double delta)
    {
        if(Global.gameState.gameModeManager.options.warehouseRobots)
        {
            if(Global.Lobby.bIsLobbyHost)
            {
                updatePathTimer -= delta;
                //decide if we want to updatepath and set target and start
                if(updatePathTimer <= 0)
                {
                    GD.Print("Updating All Agents Path");
                    SpawnHorde();
                    UpdateAllAgentPaths();
                    updatePathTimer = pathUpdateWaitTime;
                }
            }
        }
    }

    // public void SetGlobalAITarget(Node3D target)
    // {
    //     foreach (HordeAgent npc in controlledNPCs)
    //     {
    //         npc.MovementTarget = target;
    //     }
    // }



}