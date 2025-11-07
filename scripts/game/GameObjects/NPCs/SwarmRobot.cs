using Godot;
using ImGuiNET;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


[GlobalClass]
public partial class SwarmRobot : GOBaseNPC, IsDamagable
{
    //add robot walking sounds/ambient sounds, add robot kills to end of round screen, spawn waves of ai, etc, 
    [Export] public Node3D root;
    [Export] public CollisionShape3D collider;
    [Export] public SwarmRobotState state = SwarmRobotState.NONE;
    [Export] private Area3D meleeArea;
    [Export] public AnimationPlayer animationPlayer;
    [Export] public AudioStreamPlayer3D genericSFX;
    [Export] public AudioStreamPlayer3D ambientSFX;
    public float maxHealth { get; set; } = 50;
    public float currentHealth { get; set; } = 50;
    private float currentSpeed = 10;
    private CharacterSoundManager characterSoundManager;
    string[] ambientSounds =
    {
        "res://assets/audio/enemies/combine_button_locked.wav",
        "res://assets/audio/enemies/combine_button1.wav",
        "res://assets/audio/enemies/combine_button2.wav",
        "res://assets/audio/enemies/combine_button3.wav",
        "res://assets/audio/enemies/combine_button5.wav",
        "res://assets/audio/enemies/combine_button7.wav",
    };
    private int lastAmbientIndex = -1;
    float ambientTimer = 10;
    Random rand = new Random();

    public override void _Ready()
    {
        base._Ready();

        characterSoundManager = new();
        navAgent.PathDesiredDistance = 0.5f;
        navAgent.TargetDesiredDistance = 0.5f;
        Logging.Log($"Spawned new SwarmRobot with initial state: {state} and target: {MovementTarget.Name}", "SwarmRobot");

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
            ImGui.Text($"Pathfind Next Target: {navAgent.GetNextPathPosition()}");
            ImGui.Text($"");
            ImGui.End();
        }
    }

    public override void PerTickAuth(double delta)
    {
        base._PhysicsProcess(delta);
        switch (state)
        {
            case SwarmRobotState.NONE:
                break;
            case SwarmRobotState.IDLE:
                break;
            case SwarmRobotState.WANDER:
                break;
            case SwarmRobotState.SIMPLECHASE:
                UpdateTarget(delta);
                TryAttack(delta);
                ambientTimer -= (float)delta;
                if (ambientTimer <= 0f)
                {
                    PlayRandomAmbience();
                    ResetTimer();
                }
                if (MovementTarget != null)
                {
                    Vector3 velocity = Velocity;
                    navAgent.TargetPosition = MovementTarget.GlobalPosition;

                    if (!IsOnFloor())
                    {
                        velocity.Y -= (float)(10 * delta);
                    }
                    else
                    {
                        if (navAgent.IsNavigationFinished())
                        {
                            if (IsOnFloor())
                            {
                                velocity.Y = currentSpeed;
                            }
                            Vector3 currentAgentPosition = GlobalTransform.Origin;
                            Vector3 newVel = currentAgentPosition.DirectionTo(MovementTarget.GlobalPosition) * 2;
                            velocity.X = newVel.X;
                            velocity.Z = newVel.Z;
                        }
                        else
                        {
                            Vector3 currentAgentPosition = GlobalTransform.Origin;
                            Vector3 nextPathPosition = navAgent.GetNextPathPosition();
                            Vector3 newVel = currentAgentPosition.DirectionTo(nextPathPosition) * 2;
                            velocity.X = newVel.X;
                            velocity.Z = newVel.Z;
                        }
                    }

                    Velocity = velocity;
                    if (Velocity.Length() > 0.1f)
                    {
                        Vector3 forward = new Vector3(Velocity.X, 0, Velocity.Z).Normalized();
                        // Current facing direction
                        Vector3 currentForward = -GlobalTransform.Basis.Z;
                        // Interpolate between current and target
                        Vector3 newForward = currentForward.Slerp(forward, (float)(delta * 5.0));
                        LookAt(GlobalPosition + newForward, Vector3.Up);
                    }


                    MoveAndSlide();
                }
                break;
            default:
                break;
        }

    }

    public override void PerTickShared(double delta)
    {
        base.PerTickShared(delta);
    }

    private void PlayRandomAmbience()
    {
        if (ambientSounds.Length == 0) return;

        int index;
        do
        {
            index = rand.Next(ambientSounds.Length);
        } while (index == lastAmbientIndex && ambientSounds.Length > 1);

        lastAmbientIndex = index;
        ambientSFX.Stream = GD.Load<AudioStream>(ambientSounds[index]);
        ambientSFX.Play();
        //genericSFX.Call("play_stream", GD.Load<AudioStream>(ambientSounds[index]), 0f, 0f, 1f);
    }

    private void ResetTimer()
    {
        // Random interval between 7–20 seconds
        ambientTimer = 7f + (float)rand.NextDouble() * 13f;
    }
    
    
    private double retargetTimer = 0;
    private double retargetInterval = 3.0; // will randomize a bit

    private void UpdateTarget(double delta)
    {
        retargetTimer -= delta;
        if (retargetTimer <= 0)
        {
            retargetTimer = 3.0 + GD.RandRange(-1.0, 1.0); // 2–4 seconds

            // Find closest player
            Node3D closest = null;
            float closestDist = float.MaxValue;

            foreach (var player in GetTree().GetNodesInGroup("players")) // put your GOBasePlayer in "players" group
            {
                if (player is BasicPlayerCharacter p)
                {
                    if (p.state == CharacterState.Living && !p.knockedOut)
                    {
                        float dist = GlobalTransform.Origin.DistanceTo(p.GlobalTransform.Origin);
                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            closest = p;
                        }
                    }
                }
            }

            if (closest != null)
            {
                MovementTarget = closest;
                currentSpeed = 10f + (float)rand.NextDouble() * 5f;
            }
                
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
                        RPCManager.RPC(this, "Attack", []);
                    }
                }
                else
                {
                    RPCManager.RPC(this, "Attack", []);
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
                if (MovementTarget is BasicPlayerCharacter basicPlayerCharacter)
                {
                    if (!basicPlayerCharacter.knockedOut)
                    {
                        dmg.TakeStunDamage(20, id, PainSoundType.None);
                        dmg.TakeDamage(10, id, PainSoundType.Generic);
                    }
                }
            }
        }
    }




    public override bool InitFromData(GameObjectConstructorData data)
    {
        base.InitFromData(data);
        this.MovementTarget = Global.instance.GetNode<Node3D>((String)data.paramList[0]);
        navAgent.TargetPosition = MovementTarget.GlobalPosition;
        this.state = (SwarmRobotState)data.paramList[1];
        return true;
    }

    public override byte[] GenerateStateUpdate()
    {
        SwarmRobotStateMessage message = new SwarmRobotStateMessage();
        message.transform = this.GlobalTransform;
        message.velocity = this.Velocity;
        message.targetNodePath = Global.instance.GetPathTo(MovementTarget);
        message.state = this.state;

        return MessagePackSerializer.Serialize(message);   
    }

    public override void ProcessStateUpdate(byte[] update)
    {
        SwarmRobotStateMessage message = MessagePackSerializer.Deserialize<SwarmRobotStateMessage>(update);
        this.Transform = message.transform;
        this.Velocity = message.velocity;
        this.MovementTarget = Global.instance.GetNode<Node3D>(message.targetNodePath);
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
        //characterSoundManager.PlayDamageSound(hitSoundAudioStreamPlayer, soundType, VolumeDb);
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
        //characterSoundManager.PlayDamageSound(hitSoundAudioStreamPlayer, PainSoundType.Generic);
        currentHealth = 0;
        root.Visible = false;
        collider.Disabled = true;
        state = SwarmRobotState.NONE;
        if(byID != 0)
        {
            Global.gameState.gameModeManager.playerStats[byID].RobotKills++;
        }
        //remove ourselves and add a timed ragdoll
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
public struct SwarmRobotStateMessage
{
    [Key(0)]
    public Transform3D transform;

    [Key(1)]
    public Vector3 velocity;

    [Key(2)]
    public string targetNodePath;

    [Key(3)]
    public SwarmRobotState state;

}


public enum SwarmRobotState
{
    NONE,
    IDLE,
    WANDER,
    SIMPLECHASE,
}