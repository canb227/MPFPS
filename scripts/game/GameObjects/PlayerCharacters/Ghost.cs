
using Godot;
using ImGuiNET;
using MessagePack;
using System;

[GlobalClass]
public partial class Ghost : GOBasePlayerCharacter
{
    public override ulong controllingPlayerID { get; set; }

    public override Team team { get; set; }

    public override PlayerInputData input { get; set; }
    public ActionFlags lastTickActions { get; set; }

    public override Node3D lookRotationNode { get; set; }

    private PlayerCamera cam { get; set; }

    public RayCast3D rayCast { get; set; }
    public override Node3D thirdPersonModel { get; set; }
    public override Node3D firstPersonModel { get; set; }
    public override Node3D cameraLocationNode { get; set; }
    public override Role role { get; set; }

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
        GhostUpdate update = MessagePackSerializer.Deserialize<GhostUpdate>(_update);
        GlobalRotation = update.Rotation;
        GlobalPosition = update.Position;
    }

    public override byte[] GenerateStateUpdate()
    {
        GhostUpdate update = new GhostUpdate();
        update.Rotation = GlobalRotation;
        update.Position = GlobalPosition;
        return MessagePackSerializer.Serialize(update);
    }


    public float camXRotMax = 85;
    public float camXRotMin = -85;
    public float speed = 6;

    public override void PerTickAuth(double delta)
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

        if (!lastTickActions.HasFlag(ActionFlags.Use) && input.actions.HasFlag(ActionFlags.Use))
        {
            if (rayCast.IsColliding())
            {
                if (rayCast.GetCollider() is IsInteractable i)
                {
                    i.OnInteract(id);
                }
            }
        }

        if (input.actions.HasFlag(ActionFlags.Sprint))
        {
            speed = 12;
        }

        float moveZ = input.MovementInputVector.X;
        float moveX = input.MovementInputVector.Y;

        Vector3 localVelocity = Transform.Basis.Inverse() * Velocity;

        localVelocity.Y = 0;

        if (moveZ==0)
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

        Vector3 globalVelocity = Transform.Basis * localVelocity;

        if (input.actions.HasFlag(ActionFlags.Jump))
        {
            globalVelocity.Y = 1 * speed * .66f;
        }
        else if (input.actions.HasFlag(ActionFlags.Crouch))
        {
            globalVelocity.Y = -1 * speed * .66f;
        }


        Velocity = globalVelocity;
        MoveAndSlide();
        lastTickActions = input.actions;
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
        //if (Input.MouseMode == Input.MouseModeEnum.Captured)
        //{
        //    float mouseY = input.LookInputVector.Y * 5 * ((float)delta);
        //    float newXRot = RotationDegrees.X - mouseY;

        //    if (newXRot > camXRotMax) { newXRot = camXRotMax; }
        //    if (newXRot < camXRotMin) { newXRot = camXRotMin; }

        //    RotationDegrees = new Vector3(newXRot, (float)(RotationDegrees.Y - input.LookInputVector.X * 5 * delta), RotationDegrees.Z);
        //}
        //input.LookInputVector = Vector2.Zero; // Reset the mouse relative accumulator after applying it to the rotation

        //float moveZ = input.MovementInputVector.X;
        //float moveX = input.MovementInputVector.Y;

        //Vector3 localVelocity = Transform.Basis.Inverse() * Velocity;

        //localVelocity.Y = 0;

        //if (moveZ == 0)
        //{
        //    localVelocity.Z = 0f;
        //}
        //else
        //{
        //    localVelocity.Z = moveZ * 6;
        //}

        //if (moveX == 0)
        //{
        //    localVelocity.X = 0f;
        //}
        //else
        //{
        //    localVelocity.X = moveX * 6;
        //}

        //Vector3 globalVelocity = Transform.Basis * localVelocity;

        //if (input.actions.HasFlag(Actions.Jump))
        //{
        //    globalVelocity.Y = 1 * 4;
        //}
        //else if (input.actions.HasFlag(Actions.Crouch))
        //{
        //    globalVelocity.Y = -1 * 4;
        //}


        //Velocity = globalVelocity;
        //MoveAndSlide();
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

    public override void Assignment(Team team, Role role)
    {
        this.team = team;
        this.role = role;
    }

    public override void PerTickShared(double delta)
    {

    }

    public override void PerFrameShared(double delta)
    {

    }
}

[MessagePackObject]
public struct GhostUpdate
{
    [Key(0)]
    public Vector3 Position;

    [Key(1)]
    public Vector3 Rotation;

}