using Godot;
using ImGuiNET;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//used for experimental interpolation code
public struct NetState
{
    public Vector3 Position;
    public double arrivalTime;
}



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
    public Vector3 targetPosition;
    private float stateUpdateAge;

    //new navigation stuff
    public Vector3I currentCell;
    private float midRange = 25f;
    private float nearRange = 15f;
    private int updateCounter = 0;
    public bool recomputePath;

    //threading info
    public Vector3 SnapshotPosition;
    public System.Threading.Mutex _mutex = new();

    public override void _Ready()
    {
        base._Ready();
        Global.gameState.AIManager.agentPool.Add(this);
        meleeArea.BodyEntered += Attack;
        root.Visible = false;
        bodyArea.Monitorable = false;
        headArea.Monitorable = false;
        state = HordeAgentState.NONE;
        priority = 1;
        if(Global.gameState.AIManager._gridMutex.WaitOne(1))
        {
            UpdateGridLocation();
            Global.gameState.AIManager._gridMutex.ReleaseMutex();
        }

        //Logging.Log($"Spawned new HordeRobot with initial state: {state}", "HordeAgent");


        //debug for stuck detection, terrible for performance, uncomment in the pathfinding also
        // var mesh = GetNode<MeshInstance3D>("pCube1");

        // // Duplicate the material so this robot has its own instance
        // var original = mesh.GetActiveMaterial(0);
        // var unique = (Material)original.Duplicate();

        // mesh.SetSurfaceOverrideMaterial(0, unique);

    }

    public void SpawnAgent(Vector3 spawnPosition, int index, HordeAgent leader)
    {
        this.leader = leader;
        currentHealth = maxHealth;
        root.Visible = true;
        Global.gameState.AIManager.agentPool.Remove(this);
        if(Global.gameState.AIManager._mutex.WaitOne(1))
        {
            Global.gameState.AIManager.controlledNPCs.Add(this);
            Global.gameState.AIManager._mutex.ReleaseMutex(); 
        }

        //GlobalTransform = new Transform3D(Basis.Identity, spawnPosition);
        //ResetPhysicsInterpolation();
            // Tuning parameters
        float spacing = 0.5f; // Distance between agents
        float goldenAngle = 2.39996f; // Radians (~137.5 degrees)

        // Calculate offset based on index
        float radius = spacing * Mathf.Sqrt(index);
        float angle = index * goldenAngle;

        Vector3 offset = new Vector3(
            Mathf.Cos(angle) * radius,
            0, // Keep them on the same floor level
            Mathf.Sin(angle) * radius
        );

        // Apply the offset to the base spawn position
        GlobalPosition = spawnPosition + offset;
        SnapshotPosition = GlobalPosition;

        if(Global.gameState.AIManager._gridMutex.WaitOne(1))
        {
            UpdateGridLocation();
            Global.gameState.AIManager._gridMutex.ReleaseMutex(); 
        }

        state = HordeAgentState.SWARM;
    }

    public override void _Process(double delta)
    {
        if(attackCooldown > 0)
        {
            attackCooldown -= delta;
        }
    }

    private double deltaAccumulator = 0;
    private bool triedApplyStatePacket;
    private double timeSincePathUpdate = 0;
    private double pathUpdateRate = 3;
    private int tickIndex;
    private HordeAgent leader;
    public bool isLeader;

    public void UpdateLeader(HordeAgent leader)
    {
        this.leader = leader;
    }

    
    public List<Vector3> RecalculatePath()
    {
        //Distance to target
        BasicPlayerCharacter bpc = GetNearestAlivePlayer(GlobalPosition);
        if (bpc == null) return new List<Vector3>();
        
        var navMap = GetWorld3D().NavigationMap;
        var pathPoints = NavigationServer3D.MapGetPath(
            navMap,
            GlobalPosition,
            new Vector3(bpc.GlobalPosition.X, bpc.GlobalPosition.Y+1.0f, bpc.GlobalPosition.Z),
            true
        );
        return new List<Vector3>(pathPoints);
    }

    public override void PerTickShared(double delta)
    {
        base.PerFrameShared(delta);

        if(Global.gameState.AIManager.evacuationStarted)
        {
            state = HordeAgentState.SIMPLECHASE;
        }

        switch (state)
        {
            case HordeAgentState.NONE:
                root.Visible = false;
                break;
            case HordeAgentState.IDLE:
                //root.Visible = false;
                break;
            case HordeAgentState.SWARM:
                root.Visible = true;
                PerTickAgentShared(delta);
                break;
            case HordeAgentState.SIMPLECHASE:
                root.Visible = true;
                PerTickAgentShared(delta);
                break;
            default:
                break;
        }
    }


    public override void PerTickAuth(double delta)
    {
        SnapshotPosition = GlobalTransform.Origin;
        tickIndex++;
        switch (state)
        {
            case HordeAgentState.NONE:
                break;
            case HordeAgentState.IDLE:
                break;
            case HordeAgentState.SWARM:
                PerTickAgentAuth(delta);
                break;
            case HordeAgentState.SIMPLECHASE:
                PerTickAgentAuth(delta);
                break;
            default:
                break;
        }
        
    }

    public override void PerFrameLocal(double delta)
    {
        base.PerFrameShared(delta);
        PerFrameStateInterpolation(delta);
    }


    private void PerTickAgentShared(double delta)
    {
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

        //attempt to attack, look for overlapping bodies and attack if off cooldown
        if(meleeArea.Monitoring)
        {
            AttackTick();
        }
    }
    

    private void PerTickAgentAuth(double delta)
    {
        stateUpdateAge += (float)delta;
        base.PerTickAuth(delta);

        // Distance to all players for priority assignment
        float dist = 999;
        foreach(BasicPlayerCharacter playerCharacter in Global.gameState.gameModeManager.basicPlayers.Values)
        {
            float tempDist = (GlobalPosition - playerCharacter.GlobalPosition).Length();
            if(tempDist < dist)
            {
                dist = tempDist;
            }
        }
        
        // Decide update frequency
        if (dist > midRange)
        {
            priority = 1;
        } 
        else if (dist > nearRange)
        {
            priority = 7;
        } 
        else 
        {
            priority = 15;
        }

        deltaAccumulator += delta;
        updateCounter = 0;
        if(deltaAccumulator >= 0.4f)
        {
            if(isLeader && Global.gameState.gameModeManager.evacuationStarted)
            {
                path = RecalculatePath();
            }
            deltaAccumulator = 0;
        }
    }
    
    private Vector3 lastInterpPos;

    // Store this globally or per-entity to keep track of fractional time
    private double fractionalTick; 
    private Vector3 vel;
    private Queue<NetState> buffer = new Queue<NetState>();
    private const int MAX_BUFFER_SIZE = 32;
    private double interpolationDelay = 0.3; //300ms delay TODO tweak or make dynamic based on queue?

    public void AddNetworkState(Vector3 pos, double incomingArrivalTime)
    {
        buffer.Enqueue(new NetState
        {
            Position = pos,
            arrivalTime = incomingArrivalTime
        });

        // Keep the queue size under control
        while (buffer.Count > MAX_BUFFER_SIZE)
        {
            GD.PushWarning("HordeAgent Buffer flooded!");
        }
    }

    public void PerFrameStateInterpolation(double delta)
    {
        if (buffer.Count < 2) return;

        double currentTime = Time.GetUnixTimeFromSystem();
        double targetRenderTime = currentTime - interpolationDelay;

        // Convert to list for easy index access during search
        var states = buffer.ToList();

        NetState stateA = default; 
        NetState stateB = default; 
        bool found = false;

        for (int i = states.Count - 1; i >= 1; i--)
        {
            NetState newer = states[i];
            NetState older = states[i - 1];

            if (newer.arrivalTime >= targetRenderTime && older.arrivalTime <= targetRenderTime)
            {
                stateB = newer;
                stateA = older;
                found = true;
                break;
            }
        }

        if (found)
        {
            double timeGap = stateB.arrivalTime - stateA.arrivalTime;
            float t = timeGap > 0 ? (float)((targetRenderTime - stateA.arrivalTime) / timeGap) : 1f;
            t = Mathf.Clamp(t, 0, 1);

            Vector3 interpPos = stateA.Position.Lerp(stateB.Position, t);
            
            vel = (interpPos - GlobalPosition) / (float)delta;
            GlobalPosition = interpPos;

            if (vel.LengthSquared() > 0.01f)
                SmoothRotateY(vel, (float)delta);

            //remove states that are older than stateA, as we won't need them anymore
            while (buffer.Count > 0 && buffer.Peek().arrivalTime < stateA.arrivalTime)
            {
                buffer.Dequeue();
            }
        }
        else if (targetRenderTime > states.Last().arrivalTime)
        {
            GlobalPosition += vel * (float)delta;
        }
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
    

    public float separationRadius = 1;
    public int lookAheadDist = 0;
    public float pathWeight = 1;
    public float cohWeight = 1;
    public float sepWeight = 2;
    public float avoidWeight = 30;
    public float networkWeight = 2;
    private float navMeshSnapTolerance = 0.1f;
    public int currentIndex = 0;
    private float waypointThreshold = 20.0f; //deprecated
    public List<Vector3> path;
    public bool stuck;
    private Vector3 positionOneSecondAgo;
    private float positionTimer;
    private float distanceLastCheck = 999;
    public Vector3 velocity = Vector3.Zero;
   
    public float TurnSpeed = 10.0f; // Higher = snappier

    public void SmoothRotateY(Vector3 velocity, float deltaF)
    {
        if (velocity.LengthSquared() > 0.01f)
        {
            float targetAngle = (float)Math.PI + Mathf.Atan2(velocity.X, velocity.Z);
            float currentAngle = Rotation.Y;
            float nextAngle = Mathf.LerpAngle(currentAngle, targetAngle, (float)deltaF * TurnSpeed);
            Rotation = new Vector3(0, nextAngle, 0);
        }
    }
    
    public void UpdatePath(List<Vector3> path)
    {
        this.path = path;
        currentIndex = 0;
        positionTimer = 0;
    }

    public void UpdateGridLocation()
    {
        Vector3I cell = new Vector3I(
            Mathf.FloorToInt(GlobalPosition.X / Global.gameState.AIManager.cellSize),
            Mathf.FloorToInt(GlobalPosition.Y / Global.gameState.AIManager.cellSize),
            Mathf.FloorToInt(GlobalPosition.Z / Global.gameState.AIManager.cellSize)
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
        message.transformOrigin = SnapshotPosition; //TODO does snapshot really work?
        //message.targetNodePath = Global.instance.GetPathTo(MovementTarget);
        message.state = state;

        return MessagePackSerializer.Serialize(message);   
    }

    public override void ProcessStateUpdate(byte[] update)
    {
        HordeAgentStateMessage message = MessagePackSerializer.Deserialize<HordeAgentStateMessage>(update);
        AddNetworkState(message.transformOrigin, Time.GetUnixTimeFromSystem());
        this.targetPosition = message.transformOrigin;
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
            //Logging.Log($"{id} SwarmRobot has died", "SwarmRobot");
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
        root.Visible = false;
        bodyArea.Monitorable = false;
        headArea.Monitorable = false;
        state = HordeAgentState.NONE;
        currentHealth = maxHealth;
        if (byID != 0)
        {
            Global.gameState.gameModeManager.playerStats[byID].RobotKills++;
        }
        Position = new Vector3(Position.X, -10, Position.Z);
        if(Global.gameState.AIManager._gridMutex.WaitOne(1))
        {
            UpdateGridLocation();
            Global.gameState.AIManager._gridMutex.ReleaseMutex();
        }

        Global.gameState.AIManager.agentPool.Add(this);
        if(Global.gameState.AIManager._mutex.WaitOne(1))
        {
            Global.gameState.AIManager.controlledNPCs.Remove(this);
            Global.gameState.AIManager._mutex.ReleaseMutex();
        }


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
    
    public bool HasLineOfSight(Vector3 from, Vector3 to)
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

    public void ApplyThreadedSteering(Vector3 steering, float delta)
    {
        path = leader.path;
        var temppath = path.ToList();
        if(temppath == null || temppath.Count == 0)
        {
            GD.Print("temppath null");
        }
        if(distanceLastCheck < 0.5 && temppath.Last().DistanceSquaredTo(GlobalPosition) > 20)
        {
            stuck = true;
        }
        else if(distanceLastCheck < 0.5 && temppath.Last().DistanceSquaredTo(GlobalPosition) < 20)
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
        float accel = 5.0f;
        float speed = 30.0f;
        float followSpeed = 11.0f;
        Vector3 candidate = GlobalPosition + steering * delta * speed;

        var space = GetWorld3D().DirectSpaceState;
        var query = new PhysicsRayQueryParameters3D
        {
            From = GlobalPosition,
            To = candidate,
            CollisionMask = (1 << 0),
        };

        var hit = space.IntersectRay(query);
        targetPosition = candidate;

        Vector3 desiredVel = (targetPosition - GlobalPosition) * followSpeed;
        velocity = velocity.Lerp(desiredVel, 1f - Mathf.Exp(-accel * delta));

        if (hit.Count > 0 && !stuck)
        {
            Vector3 wallNormal = (Vector3)hit["normal"];
            velocity = velocity.Slide(wallNormal) + (wallNormal * 0.2f);
        }

        SmoothRotateY(velocity, delta);
        GlobalPosition += velocity * delta;

        if(currentIndex > temppath.Count - 1)
        {
            currentIndex = 0;
        }
        if ((temppath[currentIndex] - GlobalPosition).LengthSquared() < waypointThreshold &&
            currentIndex < temppath.Count - 1 &&
            HasLineOfSight(GlobalPosition, temppath[currentIndex + 1]))
        {
            currentIndex++;
        }
        UpdateGridLocation();
        positionTimer += (float)delta;
        if(positionTimer >= 3f)
        {
            positionTimer = 0;
            distanceLastCheck = positionOneSecondAgo.DistanceSquaredTo(GlobalPosition);
            positionOneSecondAgo = GlobalPosition;
        }
    }


}

[MessagePackObject]
public struct HordeAgentStateMessage
{
    [Key(0)]
    public Vector3 transformOrigin;
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
