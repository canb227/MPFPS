
using Godot;
using ImGuiNET;
using MessagePack;
using System;

[GlobalClass]
public partial class BasicPlayer : GOBasePlayerCharacter, IsDamagable, HasInventory
{
    public override ulong controllingPlayerID { get; set; }

    public override Team team { get; set; }
    public override Role role { get; set; }
    public override PlayerInputData input { get; set; }



    private PlayerCamera cam { get; set; }

    public RayCast3D rayCast { get; set; }
    public float maxHealth { get; set; }
    public float currentHealth { get; set; }
    public Inventory inventory { get; set; }
    public IsInventoryItem equipped { get; set; }

    public override void _Ready()
    {
        base._Ready();
        priority = 100;

        rayCast = new();
        rayCast.TargetPosition = new Vector3(0, 0, -50);
        rayCast.CollideWithBodies = true;
        AddChild(rayCast);
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
    public float speed = 6;
    private Vector3 jumpVelocity;

    public override void PerTickAuth(double delta)
    {
        HandleMouseLook(delta);
        HandleNonMovementInput(delta);
        HandleEquippedPassthruInput(delta);
        HandleMovementInputAndPhysics(delta);
    }

    private void HandleMovementInputAndPhysics(double delta)
    {
        Velocity = HandleYAxis(Velocity);

        Vector3 localVelocity = PCUtils.LocalizeVector(this, Velocity);

        if (input.actions.HasFlag(ActionFlags.Sprint))
        {
            speed *= 2;
        }

        float moveZ = input.MovementInputVector.X;
        float moveX = input.MovementInputVector.Y;

        if (moveZ == 0)
        {
            localVelocity.Z = 0f;
        }
        else
        {
            localVelocity.Z = moveZ * speed;
        }

        if (moveX == 0)
        {
            localVelocity.X = 0f;
        }
        else
        {
            localVelocity.X = moveX * speed;
        }

        Velocity = PCUtils.GlobalizeVector(this,localVelocity);
        MoveAndSlide();
    }

    private Vector3 HandleYAxis(Vector3 globalVelocity)
    {
        if (!IsOnFloor())
        {
            globalVelocity.Y -= ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
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
        equipped.HandleInput(input.actions);
    }

    private void HandleNonMovementInput(double delta)
    {
        if (input.actions.HasFlag(ActionFlags.Use))
        {
            if (rayCast.IsColliding())
            {
                if (rayCast.GetCollider() is IsInteractable i)
                {
                    if (rayCast.GetCollider() is IsInventoryItem s)
                    {
                        s.OnPickup(id);
                        inventory.StoreItem(s);
                    }
                    else
                    {
                        i.OnInteract(id);
                    }
                }
            }
        }
    }

    private void HandleMouseLook(double delta)
    {
        if (Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            float mouseY = input.LookInputVector.Y * 5 * ((float)delta);
            float newXRot = RotationDegrees.X - mouseY;

            if (newXRot > camXRotMax) { newXRot = camXRotMax; }
            if (newXRot < camXRotMin) { newXRot = camXRotMin; }

            RotationDegrees = new Vector3(newXRot, (float)(RotationDegrees.Y - input.LookInputVector.X * 5 * delta), RotationDegrees.Z);
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

    public override void PerTickLocal(double delta)
    {

    }

    public override void PerFrameLocal(double delta)
    {

    }

    protected override void CreateAndConfigureCamera()
    {
        lookRotationNode = GetNode<Node3D>("cameraParent");
        PlayerCamera cam = new();
        lookRotationNode.AddChild(cam);
        this.cam = cam;
        Global.ui.SwitchFullScreenUI("BasePlayerHUD");
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override Camera3D GetCamera()
    {
        return cam;
    }

    public override string GenerateStateString()
    {
        return MessagePackSerializer.ConvertToJson(GenerateStateUpdate());
    }

    public void OnDamage(float damage, ulong byID)
    {
        currentHealth -= damage;
    }

    public void OnZeroHealth(float damage, ulong byID)
    {
        destroyed = true;
    }

    public void EquipNext()
    {
        throw new NotImplementedException();
    }

    public void EquipPrevious()
    {
        throw new NotImplementedException();
    }

    public void DropEquipped()
    {
        throw new NotImplementedException();
    }

    public void Equip(InventoryGroupCategory category, int index = 0)
    {
        throw new NotImplementedException();
    }

    public override void Assignment(Team team, Role role)
    {
        this.team = team;
        this.role = role;
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