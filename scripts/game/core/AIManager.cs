using Godot;
using MessagePack;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Threading;

public partial class AIManager : Node3D
{
    public List<HordeAgent> agentPool = new();
    public List<HordeAgent> controlledNPCs = new();
    public GOBasePlayerCharacter localPlayer;
    private GameModeOptions options;
    private Dictionary<Vector3I, List<HordeAgent>> grid = new(); //only used by thread
    public static List<Vector3> path = new();
    public System.Threading.Mutex _gridMutex = new();

    internal void GameStartAsHost()
    {
        Logging.Log($"Starting server-side AI manager", "AIManager");
        options = Global.gameState.gameModeManager.options;
    }

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
        SpawnAgents(hordeSpawnLocation, targetLocation);
        //RPCManager.RPC(this, "SpawnAgents", [hordeSpawnLocation, targetLocation]);
    }

    public void SpawnAgents(Transform3D hordeSpawnLocation, Vector3 targetLocation)
    {
        path = CalculatePath(new Vector3(hordeSpawnLocation.Origin.X, hordeSpawnLocation.Origin.Y+1.0f, hordeSpawnLocation.Origin.Z), new Vector3(targetLocation.X, targetLocation.Y+1.0f, targetLocation.Z));
        var agentPoolSnapshot = agentPool.ToList();
        for(int i = 0; i < hordeSize && i < agentPoolSnapshot.Count(); i ++)
        {
            currentHordeSize++;
            //spawn the agent and set its path
            agentPoolSnapshot[i].SpawnAgent(hordeSpawnLocation.Origin, i).UpdatePath(path);
        }
    }

    public void CreateAgentPool(int agentPoolCount = 300)
    {
        agentPool.Clear();
        controlledNPCs.Clear();
        if(Global.Lobby.bIsLobbyHost)
        {
            for(int i = 0; i < agentPoolCount; i++)
            {
                GameObjectConstructorData data = new(GameObjectType.HordeAgent);
                data.spawnTransform = Transform3D.Identity;
                data.spawnTransform.Origin = new Vector3(0,-100,0);
                data.paramList.Add(HordeAgentState.NONE);
                Global.gameState.Auth_SpawnObject(GameObjectType.HordeAgent, data);
            }
        }
    }
    public override void _Ready()
    {
        base._Ready();
        GD.Print("hello");
        _avoidanceThread = new(AvoidanceLoop);
        _avoidanceThread.Start();
    }

    public void NewRound()
    {
        evacuationStarted = false;
        currentHordeCooldown = 10; //TODO set this to a real number
        grid = new();
        agentPool = new();
        controlledNPCs = new();
        CreateAgentPool();
    }


    public void UpdateLocalPlayer(GOBasePlayerCharacter localPlayer)
    {
        this.localPlayer = localPlayer;
    }

    public void MoveAgentCell(HordeAgent agent, Vector3I oldCell, Vector3I newCell) //main thread?
    {
        _gridMutex.WaitOne();
        if (grid.ContainsKey(oldCell))
            grid[oldCell].Remove(agent);

        if (!grid.ContainsKey(newCell))
            grid[newCell] = new List<HordeAgent>();

        grid[newCell].Add(agent);
        _gridMutex.ReleaseMutex();
    }

    public List<Vector3> CalculatePath(Vector3 start, Vector3 goal)
    {
        var navMap = GetWorld3D().NavigationMap;
        var pathPoints = NavigationServer3D.MapGetPath(navMap, start, goal, true);

        return new List<Vector3>(pathPoints);
    }


    public List<HordeAgent> GetNeighbors(HordeAgent agent) //thread
    {
        List<HordeAgent> neighbors = new();
        Vector3I cell = agent.currentCell;

        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++) //we probably could ignore y, but second floors of buildings and such
                for (int dz = -1; dz <= 1; dz++)
                {
                    Vector3I neighborCell = cell + new Vector3I(dx, dy, dz);
                    _gridMutex.WaitOne();
                    if (grid.ContainsKey(neighborCell))
                    {
                        foreach (var other in grid[neighborCell]) //we may move where a agent is while calculating our new location
                        {
                            if (other == agent) continue;
                            neighbors.Add(other);
                        }
                    }
                    _gridMutex.ReleaseMutex();
                }
        return neighbors;
    }

    bool evacuationStarted;
    double currentHordeCooldown = 9999;
    double hordeCooldown = 120;
    double evacuationHordeCooldown = 10;
    bool announcedHorde = false;
    int hordeSize = 0;
    bool hordeActive = false;
    public int currentHordeSize = 0;
    public override void _PhysicsProcess(double delta)
    {
        if(Global.Lobby.bIsLobbyHost)
        {
            currentHordeCooldown -= delta;
            //decide if we want to updatepath and set target and start
            if(currentHordeCooldown <= 30 && currentHordeCooldown > 0 && !announcedHorde && !Global.gameState.gameModeManager.evacuationStarted)
            {
                RPCManager.RPC(Global.gameState.gameModeManager, "TriggerSwarmIncomingEvent", []);
                announcedHorde = true;
            }
            if(currentHordeCooldown <= 0)
            {
                Logging.Log("Spawn Horde", "AIManager");
                hordeActive = true;
                //we should play a sound like L4D
                GD.Print("RPC swarm started");
                RPCManager.RPC(Global.gameState.gameModeManager, "TriggerSwarmStartedEvent", []);
                //Global.gameState.gameModeManager.TriggerSwarmStartedEvent();
                if(Global.gameState.gameModeManager.evacuationStarted)
                {
                    currentHordeCooldown = evacuationHordeCooldown;
                    hordeSize = 5 + Global.gameState.gameModeManager.numPlayers * 3;
                }
                else
                {
                    currentHordeCooldown = hordeCooldown;
                    hordeSize = 200; //Global.gameState.gameModeManager.numPlayers * 20; //TODO testing should probably just be like 20 per player? (min 50?) (max 300)
                }

                int maxChunkSize = 50;

                int chunkCount = (hordeSize + maxChunkSize - 1) / maxChunkSize;
                int chunkSize = hordeSize/chunkCount;
                for (int i = 0; i < chunkCount; i++)
                {
                    SpawnHorde(chunkSize);
                }

                announcedHorde = false;
            }
            //TODO
            if(currentHordeSize <= 10 && hordeActive)
            {
                hordeActive = false;
                GD.Print("Swarm Ended: " + currentHordeSize);
                RPCManager.RPC(Global.gameState.gameModeManager, "TriggerSwarmDefeatedEvent", []);
            }
        }
        if(Global.gameState.gameModeManager.options.warehouseRobots)
        {
            
        }
        //apply threading
        foreach(var agent in controlledNPCs)
        {
            if (_resultsBuffer.ContainsKey(agent))
            {
                agent.ApplyThreadedSteering(_resultsBuffer[agent], (float)delta);
            }
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
    }


    public void EvacuationStarted()
    {
        currentHordeCooldown = 5;
        evacuationStarted = true;
    }


    private Thread _avoidanceThread;
    private bool _running = true;
    private Dictionary<HordeAgent, Vector3> _resultsBuffer = new();
    private System.Threading.Mutex _mutex = new();

    private void AvoidanceLoop() {
        while (_running) 
        {
            var localResults = new Dictionary<HordeAgent, Vector3>();

            foreach (HordeAgent agent in controlledNPCs.ToList()) 
            {
                if(agent.state==HordeAgentState.SWARM || agent.state == HordeAgentState.SIMPLECHASE)
                {
                    localResults[agent] = ComputeAgentMoveThreaded(agent);
                }
            }

            _mutex.WaitOne();
            _resultsBuffer = localResults;
            _mutex.ReleaseMutex();

            OS.DelayMsec(1); // prevents CPU hogging
        }
    }

    private float waypointThreshold = 20.0f;
    public static Vector3 ComputeAgentMoveThreaded(HordeAgent agent)
    {
        agent._mutex.WaitOne();

        List<HordeAgent> neighbors = Global.gameState.AIManager.GetNeighbors(agent);
        
        // 1. Path following (look-ahead)
        Vector3 target = agent.path[Math.Min(agent.currentIndex + agent.lookAheadDist, agent.path.Count - 1)];
        Vector3 pathDir = (target - agent.SnapshotPosition).Normalized();

        // Separation
        Vector3 separation = Vector3.Zero;
        foreach (var neighbor in neighbors)
        {
            Vector3 diff = agent.SnapshotPosition - neighbor.SnapshotPosition;
            diff.Y = 0; //we dont want them flying away to spread out
            float neighbordist = diff.Length();
            if (neighbordist < agent.separationRadius && neighbordist > 0)
            {
                separation += diff.Normalized() / neighbordist;
            }
        }
        if (separation.Length() > 1.0f)
            separation = separation.Normalized();

        // Cohesion
        Vector3 cohesion = Vector3.Zero;
        if (neighbors.Count > 0)
        {
            Vector3 center = Vector3.Zero;
            foreach (var neighbor in neighbors)
                center += neighbor.SnapshotPosition;
            center /= neighbors.Count;
            cohesion = (center - agent.SnapshotPosition).Normalized();
        }

        // Combine forces
        Vector3 steering =
            pathDir * agent.pathWeight +
            separation * agent.sepWeight +
            cohesion * agent.cohWeight;
            
        if (steering.LengthSquared() > 0.001f)
            steering = steering.Normalized();

        agent._mutex.ReleaseMutex();

        return steering;
    }


        // if(distanceLastCheck < 0.5 && path.Last().DistanceSquaredTo(GlobalPosition) > 20)
    //     {
    //         //GD.Print("Stuck");
    //         //state = HordeAgentState.IDLE;
    //         stuck = true;
    //         //positionOneSecondAgo = new();
    //         //distanceLastCheck = 1;
    //         //currentIndex--;
    //     }
    //     else if(distanceLastCheck < 0.5 && path.Last().DistanceSquaredTo(GlobalPosition) < 20)
    //     {
    //         //GD.Print("GO IDLE");
    //         state = HordeAgentState.IDLE;
    //         //TODO change behavior to generator behavior
    //         return;
    //     }
    //     else
    //     {
    //         stuck = false;
    //     }



    
}
public struct AgentSnapshot {
    public int id;
    public Vector3 position;
}
