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
    [Export] public Area3D headArea;
    [Export] public Area3D bodyArea;
    [Export] public HordeAgentState state = HordeAgentState.NONE;
    [Export] private Area3D meleeArea;
    [Export] public AnimationPlayer animationPlayer;
    [Export] public AudioStreamPlayer3D genericSFX;
    public float maxHealth { get; set; } = 20;
    public float currentHealth { get; set; } = 20;
    private Transform3D targetNetworkTransform;
    private Vector3 targetPosition;
    private float stateUpdateAge;

    //new navigation stuff
    private float cellSize = 1f;
    public Vector3I currentCell;
    private float midRange = 25f;
    private float nearRange = 15f;
    private int updateCounter = 0;
    private static int computeBucket = 0; // shared across agents
    private int myBucket;
    private int bucketCount = 8;

    public override void _Ready()
    {
        base._Ready();
        Global.gameState.AIManager.agentPool.Add(this);
        meleeArea.BodyEntered += Attack;
        root.Visible = false;
        bodyArea.Monitorable = false;
        headArea.Monitorable = false;
        state = HordeAgentState.NONE;
        Position = new Vector3(0, 1, 0);
        priority = 1;
        myBucket = computeBucket++ % bucketCount;
        UpdateGridLocation();
        Logging.Log($"Spawned new HordeRobot with initial state: {state}", "HordeAgent");


        //debug for stuck detection, terrible for performance, uncomment in the pathfinding also
        // var mesh = GetNode<MeshInstance3D>("pCube1");

        // // Duplicate the material so this robot has its own instance
        // var original = mesh.GetActiveMaterial(0);
        // var unique = (Material)original.Duplicate();

        // mesh.SetSurfaceOverrideMaterial(0, unique);

    }

    public HordeAgent SpawnAgent(Vector3 spawnPosition)
    {
        state = HordeAgentState.SWARM;
        currentHealth = maxHealth;
        root.Visible = true;
        Global.gameState.AIManager.agentPool.Remove(this);
        Global.gameState.AIManager.controlledNPCs.Add(this);
        //GlobalTransform = new Transform3D(Basis.Identity, spawnPosition);
        //ResetPhysicsInterpolation();
        targetPosition = spawnPosition;
        UpdateGridLocation();
        return this;
    }

    public override void _Process(double delta)
    {
        if (Global.DrawDebugScreens)
        {
            ImGui.Begin("path");
            ImGui.Text($"Pathfinding Debug for: {Name}");
            ImGui.Text($"Self Pos: {GlobalPosition}");
            ImGui.Text($"");
            ImGui.End();
        }
        if(attackCooldown > 0)
        {
            attackCooldown -= delta;
        }
    }

    // public override void PerTickAuth(double delta)
    // {
    //     base.PerTickAuth(delta);
    //     switch (state)
    //     {
    //         case HordeAgentState.NONE:
    //             break;
    //         case HordeAgentState.IDLE:
    //             break;
    //         case HordeAgentState.SWARM:
    //             break;
    //         case HordeAgentState.SIMPLECHASE:
    //             break;
    //         default:
    //             break;
    //     }
    // }

    private double deltaAccumulator = 0;
    private bool triedApplyStatePacket;
    private double timeSincePathUpdate = 0;
    private double pathUpdateRate = 3;
    private int tickIndex;

    
    private void RecalculatePath(double delta)
    {
        //Distance to target
        BasicPlayerCharacter bpc = GetNearestAlivePlayer(GlobalPosition);
        if (bpc == null) return;

        float distToTarget = (bpc.GlobalPosition - GlobalPosition).Length();

        //Distance between path endpoint and player
        float distPathEndToPlayer = path.Count > 0
            ? (path[path.Count - 1] - bpc.GlobalPosition).Length()
            : distToTarget;
        pathUpdateRate = Mathf.Clamp(
            distToTarget * 0.05f + distPathEndToPlayer * 0.1f,
            0.25f, 3.0f
        );

        if (timeSincePathUpdate >= pathUpdateRate)
        {
            timeSincePathUpdate = 0f;

            var navMap = GetWorld3D().NavigationMap;
            var pathPoints = NavigationServer3D.MapGetPath(
                navMap,
                GlobalPosition,
                bpc.GlobalPosition,
                true
            );
            path = new List<Vector3>(pathPoints);
        }
    }

    public override void PerTickAuth(double delta)
    {
        tickIndex++;
        switch (state)
        {
            case HordeAgentState.NONE:
                break;
            case HordeAgentState.IDLE:
                break;
            case HordeAgentState.SWARM:
                PerTickAgent(delta);
                break;
            case HordeAgentState.SIMPLECHASE:
                timeSincePathUpdate += (float)delta;
                //bucket scheduling
                if (tickIndex % bucketCount == myBucket)
                {
                    RecalculatePath(delta);
                }
                PerTickAgent(delta);
                break;
            default:
                break;
        }
        
    }

    private void PerTickAgent(double delta)
    {
        stateUpdateAge += (float)delta;
        base.PerTickAuth(delta);
        // Distance to local player
        Vector3 playerPos = Global.gameState.AIManager.localPlayer.GlobalPosition;
        float dist = (GlobalPosition - playerPos).Length();

        // Decide update frequency
        if (dist > midRange)
        {
            bodyArea.Monitorable = false;
            headArea.Monitorable = false;
            meleeArea.Monitoring = false;
        } 
        else if (dist > nearRange)
        {
            bodyArea.Monitorable = true;
            headArea.Monitorable = true;
            meleeArea.Monitoring = false;
        } 
        else 
        {
            bodyArea.Monitorable = true;
            headArea.Monitorable = true;
            meleeArea.Monitoring = true;
        }

        deltaAccumulator += delta;
        updateCounter = 0;
        if(path != null)
        {
            //if our location is too far from the fresh networked state we teleport to the correct origin once
            if(!triedApplyStatePacket)
            {
                triedApplyStatePacket = true;
                if(Transform.Origin.DistanceSquaredTo(targetNetworkTransform.Origin) > 5)
                {
                    GD.Print("TELEPORT TO NETWORK TRANSFORM");
                    Transform = targetNetworkTransform;
                }
            }
            else
            {
                MoveAgent(deltaAccumulator);
                UpdateGridLocation();
            }
        }
        //attempt to attack, look for overlapping bodies and attack if off cooldown
        if(meleeArea.Monitoring)
        {
            AttackTick();
        }

        deltaAccumulator = 0;
        LerpAgent((float)delta, 1);
    }

    private void LerpAgent(float deltaF, float ticksPerUpdate)
    {
        //lerp towards targetPosition
        Position = Position.Lerp(targetPosition, 60.0f * deltaF * (1/ticksPerUpdate));
    }

    public BasicPlayerCharacter GetNearestAlivePlayer(Vector3 agentPos)
    {
        BasicPlayerCharacter nearest = null;
        float nearestDistSq = float.MaxValue;

        foreach (var kv in Global.gameState.gameModeManager.basicPlayers)
        {
            var player = kv.Value;
            if (player.state != CharacterState.Living) continue; // skip dead

            float distSq = (player.GlobalPosition - agentPos).LengthSquared();
            if (distSq < nearestDistSq)
            {
                nearestDistSq = distSq;
                nearest = player;
            }
        }

        return nearest; // null if none alive
    }
    

    private float separationRadius = 1;
    private int lookAheadDist = 0;
    private float pathWeight = 1;
    private float cohWeight = 1;
    private float sepWeight = 2;
    private float avoidWeight = 30;
    private float networkWeight = 2;
    private float speed = 4;
    private float navMeshSnapTolerance = 0.1f;
    private int currentIndex = 0;
    private float waypointThreshold = 20.0f;
    private List<Vector3> path;
    private bool stuck;
    private Vector3 positionOneSecondAgo;
    private float positionTimer;
    private float distanceLastCheck = 999;

    private void MoveAgent(double delta)
    {
        if(distanceLastCheck < 0.5 && path.Last().DistanceSquaredTo(GlobalPosition) > 20)
        {
            //GD.Print("Stuck");
            //state = HordeAgentState.IDLE;
            stuck = true;
            //positionOneSecondAgo = new();
            //distanceLastCheck = 1;
            //currentIndex--;
        }
        else if(distanceLastCheck < 0.5 && path.Last().DistanceSquaredTo(GlobalPosition) < 20)
        {
            //GD.Print("GO IDLE");
            state = HordeAgentState.IDLE;
            //TODO change behavior to generator behavior
            return;
        }
        else
        {
            stuck = false;
        }
        float deltaF = (float)delta;
        List<HordeAgent> neighbors = Global.gameState.AIManager.GetNeighbors(this);

        var space = GetWorld3D().DirectSpaceState;

        
        //avoidance
        Vector3 origin = GlobalPosition;
        // Vector3 forward = (targetPosition - GlobalPosition).Normalized();
        // float rayLength = 1.5f;
        // uint obstacleMask = (1 << 0);
        // var hitForward = space.IntersectRay(new PhysicsRayQueryParameters3D {
        //     From = origin,
        //     To = origin + forward * rayLength,
        //     CollisionMask = obstacleMask,
        // });
        // Vector3 avoidance = forward;
        // if(hitForward.Count > 0)
        // {
        //     avoidance = ComputeAvoidanceScoredFan(origin, forward, rayLength, 7, 60f);
        // }

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
        if (separation.Length() > 1.0f)
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

        //we track state update freshness
        //if the stateupdate is new enough we add it in as a weighting for target location
        Vector3 networkOrigin = Vector3.Zero;
        if(stateUpdateAge < 0.10f)
        {
            networkOrigin = targetNetworkTransform.Origin;
        }

        //var wallRepulsion = ComputeWallRepulsion(origin, 0.4f, 1.0f);
        //float wallWeight = 3.0f;

        // Combine forces
        Vector3 steering =
            //avoidance * avoidWeight + 
            pathDir * pathWeight +
            separation * sepWeight +
            cohesion * cohWeight +
            networkOrigin * networkWeight;// +
            //wallRepulsion * wallWeight;
            

        if (steering.LengthSquared() > 0.001f)
            steering = steering.Normalized();

        //var space = GetWorld3D().DirectSpaceState;

        Vector3 candidate = GlobalPosition + steering * deltaF * speed;
        // Raycast from current position to candidate
        var query = new PhysicsRayQueryParameters3D
        {
            From = GlobalPosition,
            To = candidate,
            CollisionMask = (1 << 0), // set to walls/obstacles only
        };

        var hit = space.IntersectRay(query);

        if (hit.Count > 0 && !stuck)
        {
            // Obstacle detected: clamp to hit position
            targetPosition =  GlobalPosition; //- steering * deltaF * speed * 1;
        }
        else
        {
            // Free path
            targetPosition = candidate;
        }


        //targetPosition = candidate;

        if ((path[currentIndex] - GlobalPosition).LengthSquared() < waypointThreshold && currentIndex < path.Count - 1)
        {
            if(HasLineOfSight(origin, path[currentIndex+1]))
            {
                currentIndex++;
            }
        }
        Vector3 moveDir = (targetPosition - GlobalPosition).Normalized();
        if (moveDir.LengthSquared() > 0.001f)
        {
            Vector3 targetForward = moveDir;
            Vector3 currentForward = -GlobalTransform.Basis.Z; // forward in Godot
            Vector3 newForward = currentForward.Lerp(targetForward, 0.1f).Normalized();

            LookAt(GlobalPosition + newForward, Vector3.Up);
        }
        positionTimer += (float)delta;
        if(positionTimer >= 3f)
        {
            positionTimer = 0;
            distanceLastCheck = positionOneSecondAgo.DistanceSquaredTo(GlobalPosition);
            positionOneSecondAgo = GlobalPosition;
        }

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


    //typically attack is handeled by AttackTick, this is just for onbody enter for snappier results, on edge cases
    public void Attack(Node body)
    {
        if(attackCooldown <= 0)
        {
            attackCooldown = 3.0;
            genericSFX.Play();
            AttackBody(body);
        }
    }

    public void AttackTick()
    {
        if(attackCooldown <= 0)
        {
            var overlappingBodies = meleeArea.GetOverlappingBodies();
            if(overlappingBodies.Any())
            {
                attackCooldown = 3.0;
                genericSFX.Play();
                foreach (var body in overlappingBodies)
                {
                    AttackBody(body);
                } 
            }       
        }
    }

    private void AttackBody(Node body)
    {
        if (body is IsDamagable dmg)
        {
            if (body is BasicPlayerCharacter basicPlayerCharacter)
            {
                if (basicPlayerCharacter.knockedOut)
                {
                    return;
                }
            }
            Random rand = new();
            //even though attacking is being run by everybody, takedamage only works if you are the authority
            //this means we get the performance advantage of toggling colliders
            //and if people are close together you hear the attacks go off based on your truth
            //its imperfect but reduces network traffic while maintaining majority of sync
            dmg.TakeStunDamage(16+rand.Next(3), id, PainSoundType.None);
            dmg.TakeDamage(4+rand.Next(3), id, PainSoundType.Generic);
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
        this.state = message.state;
        // HordeAgentStateMessage message = MessagePackSerializer.Deserialize<HordeAgentStateMessage>(update);
        // this.targetNetworkTransform = message.transform;
        // stateUpdateAge = 0;
        // triedApplyStatePacket = false;
        // //this.MovementTarget = Global.instance.GetNode<Node3D>(message.targetNodePath);
        // this.state = message.state;
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
        //&& Global.steamid == authority) //only authority can say it died
        if (currentHealth <= 0) //we now allow anybody to publish damage because each client manages the swarm robots locally, very easy to cheat WARNING
        {
            Logging.Log($"{id} SwarmRobot has died", "SwarmRobot");
            OnDeath(byID);
        }
    }

    public void OnDeath(ulong byID)
    {
        //only the authority can tell people they died (host is auth for robots)
        // if (Global.steamid == authority)
        // {
        RPCManager.RPC(this, "rpc_OnDeath", [byID]);
        // }
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void rpc_OnDeath(ulong byID)
    {
        // ProcessMode = ProcessModeEnum.Disabled;
        currentHealth = 0;
        root.Visible = false;
        bodyArea.Monitorable = false;
        headArea.Monitorable = false;
        state = HordeAgentState.NONE;
        if (byID != 0)
        {
            Global.gameState.gameModeManager.playerStats[byID].RobotKills++;
        }
        Position = new Vector3(-999, -999, -999);
        UpdateGridLocation();
        Global.gameState.AIManager.agentPool.Add(this);
        Global.gameState.AIManager.controlledNPCs.Remove(this);
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

    void SetAgentColor(Color color)
    {
        var mesh = GetNode<MeshInstance3D>("pCube1");
        var mat = mesh.GetActiveMaterial(0) as StandardMaterial3D;

        if (mat != null)
        {
            mat.AlbedoColor = color;
        }
    }

    Vector3 ComputeAvoidanceScoredFan(
        Vector3 origin,
        Vector3 forward,
        float rayLength,
        int rays,
        float maxAngleDeg)
    {
        var space = GetWorld3D().DirectSpaceState;
        uint obstacleMask = 1 << 0;

        float bestScore = -Mathf.Inf;
        Vector3 bestDir = -forward; // fallback

        for (int i = 0; i < rays; i++)
        {
            float t = (float)i / (rays - 1); // 0 → 1
            float angle = Mathf.Lerp(0, maxAngleDeg, t);

            foreach (float sign in new float[] { 1f, -1f })
            {
                float ang = angle * sign;
                Vector3 dir = forward.Rotated(Vector3.Up, Mathf.DegToRad(ang));

                var hit = space.IntersectRay(new PhysicsRayQueryParameters3D {
                    From = origin,
                    To = origin + dir * rayLength,
                    CollisionMask = obstacleMask,
                });

                float hitDist = hit.Count > 0
                    ? ((Vector3)hit["position"] - origin).Length()
                    : rayLength;

                float anglePenalty = Mathf.Abs(ang) * 0.02f; // tune weight

                float score = hitDist - anglePenalty;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestDir = dir;
                }
            }
        }

        return bestDir;
    }

    Vector3 ComputeWallRepulsion(Vector3 origin, float radius, float pushStrength)
    {
        var space = GetWorld3D().DirectSpaceState;
        uint obstacleMask = 1 << 0;

        // Create a small sphere shape
        SphereShape3D sphere = new SphereShape3D();
        sphere.Radius = radius;

        var shapeParams = new PhysicsShapeQueryParameters3D
        {
            Shape = sphere,
            Transform = new Transform3D(Basis.Identity, origin),
            CollisionMask = obstacleMask,
            CollideWithBodies = true,
            CollideWithAreas = true
        };

        // Query for overlaps
        var results = space.IntersectShape(shapeParams, 8);

        if (results.Count == 0)
            return Vector3.Zero;

        // Compute push direction
        Vector3 push = Vector3.Zero;

        foreach (var hit in results)
        {
            Vector3 point = (Vector3)hit["point"];
            Vector3 normal = (Vector3)hit["normal"];

            // Push away from the wall
            push += normal * pushStrength;
        }

        return push;
    }
    bool HasLineOfSight(Vector3 from, Vector3 to)
    {
        var space = GetWorld3D().DirectSpaceState;
        uint obstacleMask = 1 << 0;

        var hit = space.IntersectRay(new PhysicsRayQueryParameters3D {
            From = from,
            To = to,
            CollisionMask = obstacleMask,
        });

        return hit.Count == 0;
    }




}

[MessagePackObject]
public struct HordeAgentStateMessage
{
    [Key(0)]
    public Transform3D transform;

    [Key(1)]
    public HordeAgentState state;

}


public enum HordeAgentState
{
    NONE,
    IDLE,
    SWARM,
    SIMPLECHASE,
}