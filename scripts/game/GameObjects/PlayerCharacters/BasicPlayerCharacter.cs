
using Godot;
using ImGuiNET;
using MessagePack;
using Steamworks;
using System;
using System.Linq;

public enum CharacterState
{
    Living,
    Missing,
    Dead
}
[GlobalClass]
public partial class BasicPlayerCharacter : GOBasePlayerCharacter, IsDamagable, HasInventory
{
    [Export] public AudioStreamPlayer3D ourVoiceSpeaker;
    [Export] public AudioStreamPlayer3D characterSFX;
    [Export] public AudioStreamPlayer3D movementSFX;
    public CharacterSoundManager characterSoundManager = new();
    public float maxHealth { get; set; } = 100;
    public float currentHealth { get; set; } = 100;
    public Inventory inventory { get; set; } = new();
    public IsInventoryItem equipped { get; set; }
    public CharacterState state { get; set; }
    public float camXRotMax = 85;
    public float camXRotMin = -85;
    public float baseSpeed = 3;
    public float acceleration = 1;
    public float deceleration = 1;
    public float finalSpeed;
    private Vector3 jumpVelocity = new Vector3(0, 5, 0);
    private bool airbrake = false;



    //this is basically our constructor
    public override bool InitFromData(GameObjectConstructorData data)
    {
        Global.gameState.gameModeManager.basicPlayers.Add(authority, this);
        base.InitFromData(data);
        return true;
    }
    public override void _Ready()
    {
        base._Ready();
        priority = 100;

        rayCast = new();
        rayCast.TargetPosition = new Vector3(0, 0, -10);
        rayCast.CollideWithBodies = true;
        camera.AddChild(rayCast);
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

    //various input functions, PerTickAuth is the main loop
    public override void PerTickAuth(double delta)
    {
        //we wrap each one because an input could kill the character meaning the later calls have no input anymore
        if (input != null)
        {
            HandleNonMovementInput(delta);
        }
        if (input != null)
        {
            HandleEquippedPassthruInput(delta);
        }
        if (input != null)
        {
            HandleMovementInputAndPhysics(delta);
            lastTickActions = input.actions;
        }
    }

    public override void PerFrameShared(double delta)
    {
        if (input != null)
        {
            HandleMouseLook(delta);
        }
    }

    private void HandleNonMovementInput(double delta)
    {
        if (!lastTickActions.HasFlag(ActionFlags.Use) && input.actions.HasFlag(ActionFlags.Use))
        {
            if (rayCast.IsColliding())
            {
                if (rayCast.GetCollider() is IsInventoryItem s)
                {
                    Pickup(s);
                }
                else if (rayCast.GetCollider() is IsInteractable i)
                {
                    i.Local_OnInteract(id);
                }
            }
        }


        if (!lastTickActions.HasFlag(ActionFlags.ProneToggle) && input.actions.HasFlag(ActionFlags.ProneToggle))
        {
            TakeDamage(20, 0, PainSoundType.Generic);
        }
    }

    private void HandleEquippedPassthruInput(double delta)
    {
        if (equipped != null)
        {
            equipped.HandleInput(input.actions);
        }

    }

    private void HandleMovementInputAndPhysics(double delta)
    {
        Velocity = HandleYAxis(Velocity, delta);

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
            if (moveZ != 0)
            {
                localVelocity.Z = GetClampedVelocity(localVelocity.Z, moveZ, acceleration, finalSpeed);
            }
            if (moveX != 0)
            {
                localVelocity.X = GetClampedVelocity(localVelocity.X, moveX, acceleration, finalSpeed);
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

            //we are moving enough and on the ground so calculate footsteps
            if (Math.Abs(localVelocity.Z) + Math.Abs(localVelocity.X) > 0.0f)
            {
                characterSoundManager.rpc_PlayMovementSound(movementSFX, MovementSoundType.Generic, false);
            }
            // else
            // {
            //     nextStepTiming
            // }
        }

        Velocity = PCUtils.GlobalizeVector(this, localVelocity);
        MoveAndSlide();
    }

    private float GetDeceleratedVelocity(float vel, float decel)
    {
        return vel > 0 ? Math.Max(vel - decel, 0) : Math.Min(vel + decel, 0);
    }

    private float GetClampedVelocity(float vel, float move, float accel, float max)
    {
        return Math.Clamp(vel + (move > 0 ? accel : -accel), -max, max);
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
                characterSoundManager.rpc_PlayMovementSound(movementSFX, MovementSoundType.Generic, true);
            }
        }
        return globalVelocity;
    }

    private void HandleMouseLook(double delta)
    {
        if (Input.MouseMode == Input.MouseModeEnum.Captured)
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

    public override void PerFrameAuth(double delta)
    {
        if (Global.DrawDebugScreens)
        {
            ImGui.Begin("PC Debug");
            ImGui.Text("InputMvVector: " + input.MovementInputVector.ToString());
            ImGui.Text("InputLookVector: " + input.LookInputVector.ToString());
            ImGui.Text($"Actions flag: {input.actions}");
            ImGui.End();
        }
    }

    public override Camera3D GetCamera()
    {
        return camera;
    }

    public override string GenerateStateString()
    {
        return MessagePackSerializer.ConvertToJson(GenerateStateUpdate());
    }

    //Equipment Functions

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public override void Pickup(IsInventoryItem item)
    {
        if (item is GOBaseInventoryItem i)
        {
            if (inventory.HasGroup(i.category))
            {
                InventoryGroup group = inventory.GetGroup(i.category);
                if (group.CanStoreOrReplaceItem(item))
                {
                    group.StoreOrReplaceItem(item, out IsInventoryItem replaced);
                    if (replaced != null)
                    {
                        (replaced as Node3D).Reparent(Global.gameState.GameObjectNodeParent);
                        replaced.OnDropped(controllingPlayerID);
                    }

                }
            }
            if (IsMe())
            {
                i.Reparent(firstPersonEquipmentAttachmentPoint, false);
            }
            else
            {
                i.Reparent(thirdPersonEquipmentAttachmentPoint, false);
            }
            i.OnPickup(controllingPlayerID);
        }

    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public override void Equip(InventoryGroupCategory category, int index = 0)
    {
        if (inventory.GetGroup(category) == null || inventory.GetGroup(category).GetItem() == null)
        {
            Logging.Error($"Cannot equip item!", "BasicPlayer");
            return;
        }
        if (equipped != null)
        {
            equipped.OnUnequipped(controllingPlayerID);
            equipped = null;
        }
        IsInventoryItem item = inventory.GetGroup(category).GetItemAt(index);
        if (item is GOBaseInventoryItem i)
        {
            equipped = i;
            i.OnEquipped(controllingPlayerID);
        }
    }

    public void EquipNext()
    {
        Equip(inventory.groups[inventory.GetNextIndex(equipped.category)].category);
    }

    public void EquipPrevious()
    {

    }

    public void DropEquipped()
    {

    }

    //Character State Functions (Health, Stun, Death, etc) \\

    public void TakeDamage(float damage, ulong byID, PainSoundType soundType)
    {
        RPCManager.RPC(this, "rpc_TakeDamage", [damage,byID,soundType]);
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void rpc_TakeDamage(float damage, ulong byID, PainSoundType soundType)
    {
        if (state == CharacterState.Living)
        {
            currentHealth -= damage;
            characterSoundManager.rpc_PlayDamageSound(characterSFX, soundType);
            Logging.Log($"{damage} Damage Taken, {currentHealth} Health Remains", "BasicPlayerCharacter");
            if (controllingPlayerID == Global.steamid)
            {
                Global.ui.inGameUI.PlayerUIManager.UpdateHealthUI((int)currentHealth, (int)maxHealth); ;
            }
            if (currentHealth <= 0)
            {
                Logging.Log($"{authority} PlayerCharacter has died", "BasicPlayerCharacter");
                rpc_OnDeath();
            }
        }
        else
        {
            Logging.Log("Tried to deal damage to already dead character: " + authority, "BasicPlayerCharacter");
        }
    }

    public void OnDeath()
    {
        RPCManager.RPC(this, "rpc_TakeDamage", []);
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void rpc_OnDeath()
    {
        characterSoundManager.PlayDeathSound(characterSFX);
        inventory.DropAllItems();
        state = CharacterState.Missing;
        currentHealth = 0;
        Global.ui.inGameUI.ScoreBoardUI.PlayerDied(authority);
        ulong tempControllingPlayerID = controllingPlayerID;
        ReleaseControl();
        Global.gameState.gameModeManager.ghostPlayers[tempControllingPlayerID].TakeControl(tempControllingPlayerID);
    }

    

    public void OnFound()
    {
        if (state != CharacterState.Dead)
        {
            state = CharacterState.Dead;
            Global.ui.inGameUI.ScoreBoardUI.PlayerFound(authority);
        }

    }

    public override void Assignment(Team team, Role role)
    {
        this.team = team;
        this.role = role;
    }


    //Control
    protected override void OnControlTaken(ulong byID)
    {
        if (byID == Global.steamid)
        {
            Logging.Log("Enabling Player UI " + byID, "BasicPlayerCharacter");
            Global.ui.inGameUI.PlayerUIManager.ShowPlayerUI(authority);
        }
    }

    protected override void OnControlReleased()
    {
        if (controllingPlayerID == Global.steamid)
        {
            Logging.Log("Disabling Player UI " + controllingPlayerID, "BasicPlayerCharacter");
            Global.ui.inGameUI.PlayerUIManager.HidePlayerUI();
        }
    }
}


[MessagePackObject]
public struct BasicPlayerStateUpdate
{
    [Key(0)]
    public Vector3 Position;

    [Key(1)]
    public Vector3 Rotation;

}