using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[GlobalClass]
public partial class SwarmManager : Node
{
    public int robotSwarmMinSize;
    public int robotSwarmSize;
    public int robotSwarmMaxSize;
    public int swarmCooldownMax;
    public int swarmCooldownMin;
    public double currentSwarmCooldown = 999;
    Random rand = new Random();
    public bool announcedSwarm = false;
    private bool evacuationStarted;
    private int robotsPerTick = 1;

    public override void _Ready()
    {
        GameModeManager.EvacuationStarted += EvacuationStarted;
    }

    public void PrepareRound(int numPlayers)
    {
        swarmCooldownMax = 300;
        swarmCooldownMin = 240;
        robotSwarmMaxSize = (int) (numPlayers * 10 * Global.gameState.gameModeManager.options.hordeSizeMultiplier);
        robotSwarmMinSize = (int) (numPlayers * 7 * Global.gameState.gameModeManager.options.hordeSizeMultiplier);
        currentSwarmCooldown = 5; //TODO should be like 120
        evacuationStarted = false;
    }

    public void EvacuationStarted()
    {
        Logging.Log("Start Evacuation Swarms", "SwarmManager");
        swarmCooldownMin = 5;
        swarmCooldownMax = 15;
        robotSwarmMaxSize = Mathf.CeilToInt(robotSwarmMaxSize / 4 * Global.gameState.gameModeManager.options.endgameHordeSizeMultiplier);
        robotSwarmMinSize = Mathf.CeilToInt(robotSwarmMinSize / 4 * Global.gameState.gameModeManager.options.endgameHordeSizeMultiplier);
        //spawn faster but smaller
        evacuationStarted = true;
    }
    int robotsSpawned = 0;
    public void PerTick(double delta)
    {
        currentSwarmCooldown -= delta;
        if (currentSwarmCooldown <= 30 && currentSwarmCooldown > 0 && !announcedSwarm && !evacuationStarted) //30 second warning
        {
            Global.gameState.gameModeManager.TriggerSwarmIncomingEvent();
            announcedSwarm = true;
        }
        if (currentSwarmCooldown <= 0)
        {
            Global.gameState.gameModeManager.TriggerSwarmStartedEvent();
            SpawnSwarm();
            currentSwarmCooldown = swarmCooldownMin + (swarmCooldownMax - swarmCooldownMin) * rand.NextDouble();
            announcedSwarm = false;
        }

        robotsSpawned = 0;
        if(robotSwarmSize > 0)
        {
            while (robotsSpawned < robotsPerTick)
            {
                SpawnRobot();
                robotSwarmSize--;
                robotsSpawned++;
            }
        }
    }

    public void SpawnRobot()
    {
        float radius = 5f;
        GameObjectConstructorData data = new(GameObjectType.SwarmRobot);
        var livingPlayers = Global.gameState.gameModeManager.basicPlayers
            .Where(p => p.Value.state == CharacterState.Living)
            .ToList();

        if (livingPlayers.Count > 0)
        {
            int index = rand.Next(livingPlayers.Count);
            Node3D chosen = livingPlayers[index].Value as Node3D;
            if (chosen != null)
            {
                data.paramList.Add(Global.instance.GetPathTo(chosen).ToString());
            }
        }

        data.paramList.Add(SwarmRobotState.SIMPLECHASE);
        float angle = (float)(rand.NextDouble() * 2 * Math.PI);
        float r = radius * Mathf.Sqrt((float)rand.NextDouble()); // sqrt for uniform density

        Vector3 offset = new Vector3(
            Mathf.Cos(angle) * r,
            0,
            Mathf.Sin(angle) * r
        );
        Transform3D spawnTransform = MapManager.GetHordeSpawnTransform();
        spawnTransform.Origin += offset;
        data.spawnTransform = spawnTransform;

        Global.gameState.Auth_SpawnObject(GameObjectType.SwarmRobot, data);
    }
    
    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void SpawnPlayerRobot(ulong steamID)
    {
        if(steamID == Global.steamid)
        {
                  float radius = 5f;
            float angle = (float)(rand.NextDouble() * 2 * Math.PI);
            float r = radius * Mathf.Sqrt((float)rand.NextDouble()); 
            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * r,
                0,
                Mathf.Sin(angle) * r
            );
            Transform3D spawnTransform = MapManager.GetHordeSpawnTransform();
            spawnTransform.Origin += offset;

            GameObjectConstructorData data = new(GameObjectType.SwarmRobotPlayer);
            data.spawnTransform = MapManager.GetHordeSpawnTransform();
            data.paramList.Add(true);
            Global.gameState.Auth_SpawnObject(GameObjectType.SwarmRobotPlayer, data);
            //spawn player as a robot  
        }
    }

    public void SpawnSwarm()
    {
        int randomSize = (int)((robotSwarmMaxSize - robotSwarmMinSize) * rand.NextDouble());
        robotSwarmSize = robotSwarmMinSize + randomSize; //swarm is somewhere between min and max size
        foreach (var playerSteamID in Global.gameState.gameModeManager.deadPlayers)
        {
            RPCManager.RPC(this, "SpawnPlayerRobot", [playerSteamID]);
        }
    }
}

