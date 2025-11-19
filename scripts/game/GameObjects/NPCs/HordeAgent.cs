using Godot;
using ImGuiNET;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


[GlobalClass]
public partial class HordeAgent : GOBaseHordeNPC, IsDamagable
{
    //add robot walking sounds/ambient sounds, add robot kills to end of round screen, spawn waves of ai, etc, 
    [Export] public Node3D root;
    [Export] public CollisionShape3D collider;
    [Export] public CollisionShape3D collider2;
    [Export] public HordeAgentState state = HordeAgentState.NONE;
    [Export] private Area3D meleeArea;
    [Export] public AnimationPlayer animationPlayer;
    [Export] public AudioStreamPlayer3D genericSFX;
    public float maxHealth { get; set; } = 50;
    public float currentHealth { get; set; } = 50;

    //new navigation stuff
    private float cellSize = 3f;
    public Vector3I currentCell;
    private float midRange = 10f;
    private float nearRange = 5f;
    private int updateCounter = 0;

    public override void _Ready()
    {
        base._Ready();
        Global.gameState.AIManager.controlledNPCs.Add(this);

        Logging.Log($"Spawned new HordeRobot with initial state: {state} and target: {MovementTarget.Name}", "SwarmRobot");

    }

    public override void _Process(double delta)
    {
        if (Global.DrawDebugScreens)
        {
            ImGui.Begin("path");
            ImGui.Text($"Pathfinding Debug for: {Name}");
            ImGui.Text($"Target: {MovementTarget.Name}");
            ImGui.Text($"Target Pos: {MovementTarget.GlobalPosition}");
            ImGui.Text($"Self Pos: {GlobalPosition}");
            ImGui.Text($"");
            ImGui.End();
        }
    }

    public override void PerTickAuth(double delta)
    {
        base._PhysicsProcess(delta);
        switch (state)
        {
            case HordeAgentState.NONE:
                break;
            case HordeAgentState.IDLE:
                break;
            case HordeAgentState.WANDER:
                break;
            case HordeAgentState.SIMPLECHASE:
                break;
            default:
                break;
        }
    }

    private double deltaAccumulator = 0;
    public override void PerTickShared(double delta)
    {
        base.PerTickShared(delta);
        // Distance to local player
        Vector3 playerPos = Global.gameState.AIManager.localPlayer.GlobalPosition;
        float dist = (GlobalPosition - playerPos).Length();

        // Decide update frequency
        int updateRate = 1; // every tick
        if (dist > midRange) updateRate = 15; // update every 15 ticks
        else if (dist > nearRange) updateRate = 5; // update every 5 ticks

        updateCounter++;
        deltaAccumulator += delta;
        if (updateCounter >= updateRate)
        {
            updateCounter = 0;
            if(path != null)
            {
                MoveAgent(deltaAccumulator);
                UpdateGridLocation();
            }
            deltaAccumulator = 0;
        }
    }

    private float separationRadius = 1;
    private int lookAheadDist = 2;
    private float pathWeight = 1;
    private float cohWeight = 1;
    private float sepWeight = 2;
    private float avoidWeight = 1;
    private float speed = 5;
    private float navMeshSnapTolerance = 0.1f;
    private int currentIndex = 0;
    private float waypointThreshold = 0.5f;
    private List<Vector3> path;
    private void MoveAgent(double delta)
    {
        float deltaF = (float)delta;
        List<HordeAgent> neighbors = Global.gameState.AIManager.GetNeighbors(this);
        // 1. Path following (look-ahead)
        Vector3 target = path[Math.Min(currentIndex + lookAheadDist, path.Count - 1)];
        Vector3 pathDir = (target - GlobalPosition).Normalized();

        // Separation
        Vector3 separation = Vector3.Zero;
        foreach (var neighbor in neighbors)
        {
            Vector3 diff = GlobalPosition - neighbor.GlobalPosition;
            diff.Y = 0; //we dont want them flying away to spread out
            float neighbordist = diff.Length();
            if (neighbordist < separationRadius && neighbordist > 0)
            {
                separation += diff.Normalized() / neighbordist;
            }
        }
        separation = separation.Normalized();

        // // Alignment
        // Vector3 avgVel = Vector3.Zero;
        // foreach (var neighbor in neighbors)
        // {
        //     avgVel += neighbor.Velocity;
        // }
        // if (neighbors.Count > 0)
        //     avgVel /= neighbors.Count;

        // Vector3 alignment = avgVel.Normalized();

        // Cohesion
        Vector3 cohesion = Vector3.Zero;
        if (neighbors.Count > 0)
        {
            Vector3 center = Vector3.Zero;
            foreach (var neighbor in neighbors)
                center += neighbor.GlobalPosition;
            center /= neighbors.Count;
            cohesion = (center - GlobalPosition).Normalized();
        }

        // Combine forces
        Vector3 steering =
            pathDir * pathWeight +
            separation * sepWeight +
            cohesion * cohWeight; // +
            //alignment * alignWeight;
            

        if (steering.LengthSquared() > 0.001f)
            steering = steering.Normalized();


        // Obstacle avoidance (navmesh based) (OFF FOR TESTING TODO)
        var navMap = GetWorld3D().NavigationMap;
        Vector3 candidate = GlobalPosition + new Vector3(steering.X * deltaF * speed, steering.Y * deltaF * speed, steering.Z * deltaF * speed);

        // Snap to navmesh
        Vector3 closest = NavigationServer3D.MapGetClosestPoint(navMap, candidate);

        // Check validity
        float dist = (closest - candidate).Length();
        float tolerance = speed * deltaF * 0.5f; // dynamic tolerance
        if (dist < tolerance)
        {
            GlobalPosition = candidate; // valid move
        }
        else
        {
            GlobalPosition = closest;   // clamp to navmesh
        }

        //GlobalPosition += new Vector3(steering.X * deltaF * speed, steering.Y * deltaF * speed, steering.Z * deltaF * speed);
        if ((target - GlobalPosition).LengthSquared() < waypointThreshold)
            currentIndex++;
    }
    
    public void UpdatePath(List<Vector3> path)
    {
        this.path = path;
        currentIndex = 0;
    }


    private void UpdateGridLocation()
    {
        Vector3I cell = new Vector3I(
            Mathf.FloorToInt(GlobalPosition.X / cellSize),
            Mathf.FloorToInt(GlobalPosition.Y / cellSize),
            Mathf.FloorToInt(GlobalPosition.Z / cellSize)
        );

        if (cell != currentCell)
        {
            Global.gameState.AIManager.MoveAgentCell(this, currentCell, cell);
            currentCell = cell;
        }
    }

    private double attackCooldown = 0;
    private const float MeleeRange = 1.7f; // tweak as needed

    private void TryAttack(double delta)
    {
        attackCooldown -= delta;
        if (MovementTarget == null || attackCooldown > 0)
            return;

        float dist = GlobalTransform.Origin.DistanceTo(MovementTarget.GlobalTransform.Origin);
        if (dist <= MeleeRange)
        {
            if (MovementTarget is IsDamagable dmg)
            {
                if (MovementTarget is BasicPlayerCharacter basicPlayerCharacter)
                {
                    if (!basicPlayerCharacter.knockedOut)
                    {
                        Attack();
                    }
                }
                else
                {
                    Attack();
                }
            }
        }
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void Attack()
    {
        attackCooldown = 3.0;
        genericSFX.Play();
        // Get all overlapping bodies
        foreach (var body in meleeArea.GetOverlappingBodies())
        {
            if (body is IsDamagable dmg)
            {
                if (body is BasicPlayerCharacter basicPlayerCharacter)
                {
                    if (basicPlayerCharacter.knockedOut)
                    {
                        continue;
                    }
                }
                dmg.TakeStunDamage(25, id, PainSoundType.None);
                dmg.TakeDamage(5, id, PainSoundType.Generic);
            }
        }
    }

    public override bool InitFromData(GameObjectConstructorData data)
    {
        base.InitFromData(data);
        this.state = (HordeAgentState)data.paramList[0];
        return true;
    }

    public override byte[] GenerateStateUpdate()
    {
        HordeAgentStateMessage message = new HordeAgentStateMessage();
        message.transform = this.GlobalTransform;
        //message.targetNodePath = Global.instance.GetPathTo(MovementTarget);
        message.state = this.state;

        return MessagePackSerializer.Serialize(message);   
    }

    public override void ProcessStateUpdate(byte[] update)
    {
        HordeAgentStateMessage message = MessagePackSerializer.Deserialize<HordeAgentStateMessage>(update);
        this.Transform = message.transform;
        //this.MovementTarget = Global.instance.GetNode<Node3D>(message.targetNodePath);
        this.state = message.state;
    }

    public void TakeDamage(float damage, ulong byID, PainSoundType soundType, int VolumeDb = 0)
    {
        //only the authority can tell people they took damage (host is auth for robots)
        RPCManager.RPC(this, "rpc_TakeDamage", [damage,byID,soundType,VolumeDb]);
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void rpc_TakeDamage(float damage, ulong byID, PainSoundType soundType, int VolumeDb = 0)
    {
        currentHealth -= damage;
        //Logging.Log($"{damage} Damage Taken, {currentHealth} Health Remains", "SwarmRobot");
        if (currentHealth <= 0 && Global.steamid == authority) //only authority can say it died
        {
            Logging.Log($"{id} SwarmRobot has died", "SwarmRobot");
            OnDeath(byID);
        }
    }

    public void OnDeath(ulong byID)
    {
        //only the authority can tell people they died (host is auth for robots)
        if (Global.steamid == authority)
        {
            RPCManager.RPC(this, "rpc_OnDeath", [byID]);
        }
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void rpc_OnDeath(ulong byID)
    {
        currentHealth = 0;
        root.Visible = false;
        collider.Disabled = true;
        collider2.Disabled = true;
        state = HordeAgentState.NONE;
        if (byID != 0)
        {
            Global.gameState.gameModeManager.playerStats[byID].RobotKills++;
        }
        QueueFree();
        //add a timed ragdoll
    }

    public void TakeStunDamage(float damage, ulong byID, PainSoundType soundType, int VolumeDb = 0)
    {
        TakeDamage(damage, byID, soundType, VolumeDb);
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void rpc_TakeStunDamage(float damage, ulong byID, PainSoundType soundType, int VolumeDb = 0)
    {
        rpc_TakeDamage(damage, byID, soundType, VolumeDb);
    }

}

[MessagePackObject]
public struct HordeAgentStateMessage
{
    [Key(0)]
    public Transform3D transform;

    //[Key(1)]
    //public string targetNodePath;

    [Key(1)]
    public HordeAgentState state;

}


public enum HordeAgentState
{
    NONE,
    IDLE,
    WANDER,
    SIMPLECHASE,
}