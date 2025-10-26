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
    public int swarmCooldownMax = 300;
    public int swarmCooldownMin = 240;
    public double currentSwarmCooldown;
    Random rand = new Random();
    public bool announcedSwarm = false;

    public void PrepareRound(int numPlayers)
    {
        robotSwarmMaxSize = numPlayers * 10;
        robotSwarmMinSize = numPlayers;
        currentSwarmCooldown = 120; //randomize or make 2 mins
    }

    public void PerTick(double delta)
    {
        currentSwarmCooldown -= delta;
        if(currentSwarmCooldown <= 30 && currentSwarmCooldown > 0 && !announcedSwarm) //30 second warning
        {
            Global.gameState.gameModeManager.TriggerSwarmIncomingEvent();
            announcedSwarm = true;
        }
        if(currentSwarmCooldown <= 0)
        {
            Global.gameState.gameModeManager.TriggerSwarmStartedEvent();
            SpawnSwarm();
            currentSwarmCooldown = swarmCooldownMin + (swarmCooldownMax - swarmCooldownMin) * rand.NextDouble();
            announcedSwarm = false;
        }
    }

    public void SpawnSwarm()
    {
        int randomSize = (int)((robotSwarmMaxSize - robotSwarmMinSize) * rand.NextDouble());
        robotSwarmSize = robotSwarmMinSize + randomSize; //swarm is somewhere between min and max size
        Transform3D baseTransform = MapManager.GetHordeSpawnTransform();

        for (int i = 0; i < robotSwarmSize; i++)
        {
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

            float radius = robotSwarmSize/10f; // adjust based on swarm size
            float angle = (float)(rand.NextDouble() * Math.PI * 2);
            float dist = (float)(rand.NextDouble() * radius);

            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * dist,
                0, // keep Y the same so they’re on the ground
                Mathf.Sin(angle) * dist
            );

            Transform3D spawnTransform = baseTransform;
            spawnTransform.Origin += offset;

            data.spawnTransform = spawnTransform;

            Global.gameState.Auth_SpawnObject(GameObjectType.SwarmRobot, data);
        }
    }

}

