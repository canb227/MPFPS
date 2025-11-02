using Godot;
using ImGuiNET;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


[GlobalClass]
public partial class SwarmRobotPlayerCharacter : GOBasePlayerCharacter, IsDamagable
{
    [Export] public Node3D root;
    [Export] public CollisionShape3D collider;
    [Export] private Area3D meleeArea;
    [Export] public AnimationPlayer animationPlayer;
    [Export] public AudioStreamPlayer3D hitSoundAudioStreamPlayer;
    [Export] public AudioStreamPlayer3D genericSFX;
    [Export] public AudioStreamPlayer3D movementSFX;
    [Export] public Camera3D camera;
    public virtual PlayerInputData input { get; set; }
    public ActionFlags lastTickActions { get; set; }
    public float maxHealth { get; set; } = 100;
    public float currentHealth { get; set; } = 100;
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
        Logging.Log($"Spawned new SwarmRobotPlayerCharacter Controlled by: {authority}", "SwarmRobotPlayerCharacter");
    }

    public override bool InitFromData(GameObjectConstructorData data)
    {
        base.InitFromData(data);
        //base player calls take control
        return true;
    }

    public override void PerTickAuth(double delta)
    {
        base._PhysicsProcess(delta);
        if (input != null)
        {
            HandleNonMovementInput(delta);
        }  
        if (input != null)
        {
            HandleMovementInputAndPhysics(delta);
            lastTickActions = input.actions;
        }

    }

    private void HandleNonMovementInput(double delta)
    {
        if(attackCooldown > 0)
        {
            attackCooldown -= delta;
        }
        if(attackCooldown <= 0)
        {
            if (!lastTickActions.HasFlag(ActionFlags.Fire) && input.actions.HasFlag(ActionFlags.Fire))
            {
                animationPlayer.Play("attack");
            }
        }
    }



    public override void ProcessStateUpdate(byte[] _update)
    {
        BasicPlayerStateUpdate update = MessagePackSerializer.Deserialize<BasicPlayerStateUpdate>(_update);
        GlobalRotation = update.Rotation;
        GlobalPosition = update.Position;
    }

    public override byte[] GenerateStateUpdate()
    {
        BasicPlayerStateUpdate update = new BasicPlayerStateUpdate();
        update.Rotation = GlobalRotation;
        update.Position = GlobalPosition;
        return MessagePackSerializer.Serialize(update);
    }

    public override string GenerateStateString()
    {
        return MessagePackSerializer.ConvertToJson(GenerateStateUpdate());
    }

    public float camXRotMax = 85;
    public float camXRotMin = -85;
    public float baseSpeed = 6;
    public float groundAcceleration = 1f;
    public float airAcceleration = 0.4f; 
    public float deceleration = 1;
    public float finalSpeed;
    private Vector3 jumpVelocity = new Vector3(0, 6, 0);
    private bool airbrake = false;

    public override void PerFrameAuth(double delta)
    {
        if (Global.DrawDebugScreens)
        {
            ImGui.Begin("RobotPC Debug");
            ImGui.Text("InputMvVector: " + input.MovementInputVector.ToString());
            ImGui.Text("InputLookVector: " + input.LookInputVector.ToString());
            ImGui.Text($"Actions flag: {input.actions}");
            ImGui.End();
        }
    }

    private void HandleMovementInputAndPhysics(double delta)
    {
        Velocity = HandleYAxis(Velocity, delta);

        Vector3 localVelocity = CalculateLocalVelocity();
        Velocity = PCUtils.GlobalizeVector(this, localVelocity);
        PushAwayRigidBodies();
        MoveAndSlide();
    }

    private Vector3 HandleYAxis(Vector3 globalVelocity, double delta)
    {
        if (!IsOnFloor())
        {
            globalVelocity.Y -= ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle() * (float)delta;
        }

        if (input.actions.HasFlag(ActionFlags.Jump))
        {
            if (IsOnFloor())
            {
                globalVelocity += jumpVelocity;
            }
        }
        return globalVelocity;
    }

    private Vector3 CalculateLocalVelocity()
    {
        Vector3 localVelocity = PCUtils.LocalizeVector(this, Velocity);

        finalSpeed = baseSpeed;
        if (input.actions.HasFlag(ActionFlags.Sprint))
        {
            finalSpeed = baseSpeed * 2;
        }

        //get input vectors
        Vector2 normalizedInput = input.MovementInputVector.Normalized();
        float moveZ = normalizedInput.X;
        float moveX = normalizedInput.Y;

        // whether user z value is in opposite direction of current velocity
        bool antiInput = (localVelocity.Z > 1 && moveZ < 0) || (localVelocity.Z < -1 && moveZ > 0);

        //airbrake prevents further air movement once youve cancelled your Z movement
        if (!IsOnFloor() && (antiInput || airbrake))
        {
            airbrake = true;
            localVelocity.X = 0;
            localVelocity.Z = 0;
        }
        else
        {
            //reset airbrake when on ground
            airbrake = false;

            //accelerate directions
            float accel = IsOnFloor() ? groundAcceleration : airAcceleration;

            // accelerate directions
            if (moveZ != 0)
            {
                localVelocity.Z = GetClampedVelocity(localVelocity.Z, moveZ, accel, finalSpeed);
            }
            if (moveX != 0)
            {
                localVelocity.X = GetClampedVelocity(localVelocity.X, moveX, accel, finalSpeed);
            }

        }

        //apply deceleration
        if (IsOnFloor())
        {
            if (moveZ == 0)
            {
                localVelocity.Z = GetDeceleratedVelocity(localVelocity.Z, deceleration);
            }
            if (moveX == 0)
            {
                localVelocity.X = GetDeceleratedVelocity(localVelocity.X, deceleration);
            }
        }

        return localVelocity;
    }

    private float GetDeceleratedVelocity(float vel, float decel)
    {
        return vel > 0 ? Math.Max(vel - decel, 0) : Math.Min(vel + decel, 0);
    }

    private float GetClampedVelocity(float vel, float move, float accel, float max)
    {
        return Math.Clamp(vel + (move > 0 ? accel : -accel), -max, max);
    }

    private float PushForceScalar = 1.0f;
    private float Mass = 80.0f;
    private void PushAwayRigidBodies()
    {
        for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            KinematicCollision3D CollisionData = GetSlideCollision(i);

            GodotObject UnkObj = CollisionData.GetCollider();

            if (UnkObj is RigidBody3D)
            {
                RigidBody3D Obj = UnkObj as RigidBody3D;

                // Objects with more mass than us should be harder to push.
                // But doesn't really make sense to push faster than we are going
                float MassRatio = Mathf.Min(1.0f, Mass / Obj.Mass);

                // Optional add: Don't push object at all if it's 4x heavier or more
                if (MassRatio < 0.25f) continue;

                Vector3 PushDir = -CollisionData.GetNormal();
                PushDir.Y = 0;

                // How much velocity the object needs to increase to match player velocity in the push direction
                float VelocityDiffInPushDir = Velocity.Dot(PushDir) - Obj.LinearVelocity.Dot(PushDir);

                // Only count velocity towards push dir, away from character
                VelocityDiffInPushDir = Mathf.Max(0.0f, VelocityDiffInPushDir);

                PushDir.Y = 0; // Don't push object from above/below

                float PushForce = MassRatio * PushForceScalar;
                Obj.ApplyImpulse(PushDir * VelocityDiffInPushDir * PushForce, CollisionData.GetPosition() - Obj.GlobalPosition);
            }
        }
    }

    public override void PerTickLocal(double delta)
    {
    }

    public override void PerFrameLocal(double delta)
    {
    }

    public override void PerTickShared(double delta)
    {
        if (input != null)
        {
            if (input.actions.HasFlag(ActionFlags.Jump))
            {
                if (IsOnFloor())
                {
                    characterSoundManager.PlayMovementSound(movementSFX, MovementSoundType.Generic, true);
                }
            }
            else if (IsOnFloor() && Math.Abs(Velocity.Z) + Math.Abs(Velocity.X) > 0.0f)
            {
                characterSoundManager.PlayMovementSound(movementSFX, MovementSoundType.Generic, false);
            }
        }
    }

    public override void PerFrameShared(double delta)
    {
        if (input != null)
        {
            HandleMouseLook(delta);
        }
    }

    private void HandleMouseLook(double delta)
    {
        bool lockLook = false;
        if (Input.MouseMode == Input.MouseModeEnum.Captured && !lockLook)
        {
            float mouseX = input.LookInputVector.X * 5 * ((float)delta);
            float mouseY = input.LookInputVector.Y * 5 * ((float)delta);

            float newXRot = camera.RotationDegrees.X - mouseY;
            float newYRot = RotationDegrees.Y - mouseX;

            if (newXRot > camXRotMax) { newXRot = camXRotMax; }
            if (newXRot < camXRotMin) { newXRot = camXRotMin; }

            camera.RotationDegrees = new Vector3(newXRot, camera.RotationDegrees.Y, camera.RotationDegrees.Z);
            RotationDegrees = new Vector3(RotationDegrees.X, newYRot, RotationDegrees.Z);
        }
        input.LookInputVector = Vector2.Zero; // Reset the mouse relative accumulator after applying it to the rotation
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
        genericSFX.Stream = GD.Load<AudioStream>(ambientSounds[index]);
        genericSFX.Play();
    }

    private void ResetTimer()
    {
        // Random interval between 7–20 seconds
        ambientTimer = 7f + (float)rand.NextDouble() * 13f;
    }

    private double attackCooldown = 0;
    private const float MeleeRange = 2.0f; // tweak as needed

    public void Attack()
    {
        attackCooldown = 3.0;

        // Get all overlapping bodies
        foreach (var body in meleeArea.GetOverlappingBodies())
        {
            if (body is IsDamagable dmg)
            {
                dmg.TakeDamage(10, id, PainSoundType.Generic);
            }
        }
    }

    public void TakeDamage(float damage, ulong byID, PainSoundType soundType, int VolumeDb = 0)
    {
        //only the authority can tell people they took damage (host is auth for robots)
        RPCManager.RPC(this, "rpc_TakeDamage", [damage,byID,soundType]);
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void rpc_TakeDamage(float damage, ulong byID, PainSoundType soundType, int VolumeDb = 0)
    {
        currentHealth -= damage;
        characterSoundManager.PlayDamageSound(hitSoundAudioStreamPlayer, soundType, VolumeDb);
        //Logging.Log($"{damage} Damage Taken, {currentHealth} Health Remains", "SwarmRobotPlayerCharacter");
        if (currentHealth <= 0 && Global.steamid == authority) //only authority can say it died
        {
            Logging.Log($"{id} SwarmRobotPlayerCharacter has died", "SwarmRobotPlayerCharacter");
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
        characterSoundManager.PlayDamageSound(hitSoundAudioStreamPlayer, PainSoundType.Generic);
        currentHealth = 0;
        root.Visible = false;
        collider.Disabled = true;
        Global.gameState.gameModeManager.playerStats[byID].RobotKills++;
        //remove ourselves and add a timed ragdoll
    }

    public void TakeStunDamage(float damage, ulong byID, PainSoundType soundType, int VolumeDb = 0)
    {
        Logging.Log("We Take Stun Damage as damage", "SwarmRobotPlayerCharacter");
        TakeDamage(damage, byID, soundType, VolumeDb);
    }

    public override void HandleVisualRayCast(double delta)
    {
        //we dont want to have visual raycast show us player name and similar
    }

    public override void Assignment(Team team, Role role)
    {
        
    }

    public override Camera3D GetCamera()
    {
        return camera;
    }

    public override void Pickup(IsInventoryItem item)
    {
        throw new NotImplementedException();
    }

    public override void Equip(InventoryGroupCategory category, int index = 0)
    {
        throw new NotImplementedException();
    }

    protected override void OnControlTaken(ulong byID)
    {
        if (byID == Global.steamid)
        {
            Logging.Log("Enabling ROBOT OVERLAY UI (we dont have one yet xd)" + byID, "SwarmRobotPlayerCharacter");
            //Global.ui.inGameUI.PlayerUIManager.ShowPlayerUI(authority);
        }
    }

}


[MessagePackObject]
public struct SwarmRobotPlayerCharacterStateUpdate
{
    [Key(0)]
    public Vector3 Position;

    [Key(1)]
    public Vector3 Rotation;

}