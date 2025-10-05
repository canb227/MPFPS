
using Godot;
using ImGuiNET;
using MessagePack;
using System;

[GlobalClass]
public partial class BasicPlayer : GOBasePlayerCharacter, IsDamagable, HasInventory
{

    private PlayerCamera cam { get; set; }

    public RayCast3D rayCast { get; set; }
    public float maxHealth { get; set; }
    public float currentHealth { get; set; }
    public Inventory inventory { get; set; }
    public IsInventoryItem equipped { get; set; }
    public ActionFlags lastTickActions { get; set; }

    public override void _Ready()
    {
        base._Ready();
        priority = 100;

        rayCast = new();
        rayCast.TargetPosition = new Vector3(0, 0, -10);
        rayCast.CollideWithBodies = true;
        cameraLocationNode.AddChild(rayCast);
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
    public float baseSpeed = 6;
    public float finalSpeed;
    private Vector3 jumpVelocity = new Vector3(0,5,0);

    public override void PerTickAuth(double delta)
    {
        HandleMouseLook(delta);
        HandleNonMovementInput(delta);
        HandleEquippedPassthruInput(delta);
        HandleMovementInputAndPhysics(delta);
        lastTickActions = input.actions;
    }

    private void HandleMovementInputAndPhysics(double delta)
    {
        Velocity = HandleYAxis(Velocity,delta);
        
        Vector3 localVelocity = PCUtils.LocalizeVector(this, Velocity);

        finalSpeed = baseSpeed;
        if (input.actions.HasFlag(ActionFlags.Sprint))
        {
            finalSpeed = baseSpeed * 2;
        }

        float moveZ = input.MovementInputVector.X;
        float moveX = input.MovementInputVector.Y;

        if (moveZ == 0)
        {
            localVelocity.Z = 0f;
        }
        else
        {
            localVelocity.Z = moveZ * finalSpeed;
        }

        if (moveX == 0)
        {
            localVelocity.X = 0f;
        }
        else
        {
            localVelocity.X = moveX * finalSpeed;
        }

        Velocity = PCUtils.GlobalizeVector(this,localVelocity);
        MoveAndSlide();
    }

    private Vector3 HandleYAxis(Vector3 globalVelocity,double delta)
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
                    s.OnPickup(id);
                    inventory.StoreItem(s);
                }
                else if (rayCast.GetCollider() is IsInteractable i)
                {
                    i.Local_OnInteract(id);
                }

            }
        }
    }

    private void HandleMouseLook(double delta)
    {
        if (Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            float mouseX = input.LookInputVector.X * 5 * ((float)delta);
            float mouseY = input.LookInputVector.Y * 5 * ((float)delta);

            float newXRot = cameraLocationNode.RotationDegrees.X - mouseY;
            float newYRot = RotationDegrees.Y - mouseX;

            if (newXRot > camXRotMax) { newXRot = camXRotMax; }
            if (newXRot < camXRotMin) { newXRot = camXRotMin; }

            cameraLocationNode.RotationDegrees = new Vector3(newXRot, cameraLocationNode.RotationDegrees.Y, cameraLocationNode.RotationDegrees.Z);
            lookRotationNode.RotationDegrees = cameraLocationNode.RotationDegrees;
            RotationDegrees = new Vector3(RotationDegrees.X,newYRot, RotationDegrees.Z);
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

        cam = new();
        cameraLocationNode.AddChild(cam);

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

    public void TakeDamage(float damage, ulong byID)
    {
        currentHealth -= damage;
        if (currentHealth < 0)
        {

        }
    }

    public void EquipNext()
    {

    }

    public void EquipPrevious()
    {

    }

    public void DropEquipped()
    {

    }

    public void Equip(InventoryGroupCategory category, int index = 0)
    {

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