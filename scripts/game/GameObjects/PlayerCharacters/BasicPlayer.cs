
using Godot;
using ImGuiNET;
using MessagePack;
using System;
using System.Linq;

public enum CharacterState
{
    Living,
    Missing,
    Dead
}
[GlobalClass]
public partial class BasicPlayer : GOBasePlayerCharacter, IsDamagable, HasInventory
{
    public float maxHealth { get; set; }
    public float currentHealth { get; set; }
    public Inventory inventory { get; set; } = new();
    public IsInventoryItem equipped { get; set; }
    public CharacterState state { get; set; }

    public override void _Ready()
    {
        base._Ready();
        priority = 100;

        rayCast = new();
        rayCast.TargetPosition = new Vector3(0, 0, -10);
        rayCast.CollideWithBodies = true;
        camera.AddChild(rayCast);


    }

    private void SetupInventory()
    {
        GameState.GameObjectConstructorData data = new(GameObjectType.Hands);
        data.paramList.Add(id);
        Global.gameState.Auth_SpawnObject(GameObjectType.Hands, data);
    }


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
                    group.StoreOrReplaceItem(item,out IsInventoryItem replaced);
                    if (replaced != null)
                    {
                        (replaced as Node3D).Reparent(Global.gameState.nodeGameObjects);
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

    [RPCMethod(mode =RPCMode.SendToAllPeers)]
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


    public float camXRotMax = 85;
    public float camXRotMin = -85;
    public float baseSpeed = 3;
    public float acceleration = 1;
    public float deceleration = 1;
    public float finalSpeed;
    private Vector3 jumpVelocity = new Vector3(0, 5, 0);
    private bool airbrake = false;



    public override void PerTickAuth(double delta)
    {
        if (input != null)
        {
            HandleNonMovementInput(delta);
            HandleEquippedPassthruInput(delta);
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
            }
        }
        return globalVelocity;
    }

    private void HandleEquippedPassthruInput(double delta)
    {
        if (equipped != null)
        {
            equipped.HandleInput(input.actions);
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
        if (input.actions.HasFlag(ActionFlags.ScoreBoard))
        {
            //could play a looking at wrist animation or something
        }
        if (input.actions.HasFlag(ActionFlags.Prone))
        {

        }
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

    protected override void SetupLocalPlayerCharacter()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;

        SetupInventory();
    }

    public override Camera3D GetCamera()
    {
        return camera;
    }

    public override string GenerateStateString()
    {
        return MessagePackSerializer.ConvertToJson(GenerateStateUpdate());
    }

    public void TakeDamage(float damage, ulong byID)
    {
        currentHealth -= damage;
        if (controllingPlayerID == Global.steamid)
        {
            Global.ui.inGameUI.PlayerUIManager.UpdateHealthUI((int)currentHealth, (int)maxHealth); ;
        }
        if (currentHealth < 0)
        {
            OnDeath();
        }
    }

    public void OnDeath()
    {
        Global.ui.inGameUI.ScoreBoardUI.AddDeadWorkerPlayerRow(authority);
        if (controllingPlayerID == Global.steamid)
        {
            Global.ui.inGameUI.PlayerUIManager.Visible = false;
        }
    }


    public override void Assignment(Team team, Role role)
    {
        this.team = team;
        this.role = role;
    }

    public override void Respawn()
    {
        base.Respawn();
        Global.ui.inGameUI.ScoreBoardUI.AddLivingWorkerPlayerRow(authority);
    }



    public override bool InitFromData(GameState.GameObjectConstructorData data)
    {
        base.InitFromData(data);
        Global.gameState.gameModeManager.basicPlayers.Add(this);
        return true;
    }

    public override void ResetCharacterInfo()
    {
        maxHealth = 100;
        currentHealth = 100;
        SetupInventory();
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