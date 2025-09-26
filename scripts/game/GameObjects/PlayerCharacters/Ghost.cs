
using Godot;
using ImGuiNET;
using MessagePack;
using System;

[GlobalClass]
public partial class Ghost : GOBasePlayerCharacter
{
    public override ulong controllingPlayerID { get; set; }

    public override int team { get; set; }

    public override PlayerInputData input { get; set; }

    public override Node3D cameraParent { get; set; }

    private PlayerCamera cam { get; set; }

    public override void _Ready()
    {
        base._Ready();
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
            localVelocity.Z = moveZ * 6;
        }

        if (moveX == 0)
        {
            localVelocity.X = 0f;
        }
        else
        {
            localVelocity.X = moveX * 6;
        }

        Vector3 globalVelocity = Transform.Basis * localVelocity;

        if (input.inputs["JUMP"])
        {
            globalVelocity.Y = 1 * 4;
        }
        else if (input.inputs["CROUCH"])
        {
            globalVelocity.Y = -1 * 4;
        }


        Velocity = globalVelocity;
        MoveAndSlide();
    }

    public float camXRotMax = 85;
    public float camXRotMin = -85;

    public override void PerFrameAuth(double delta)
    {
        if (Global.DrawDebugScreens)
        {
            ImGui.Begin("PC Debug");
            ImGui.Text("InputMvVector: " + input.MovementInputVector.ToString());
            ImGui.Text("InputLookVector: " + input.LookInputVector.ToString());
            foreach (var actionEntry in input.inputs)
            {
                ImGui.Text($"{actionEntry.Key}:{actionEntry.Value}");
            }
            ImGui.End();
        }


}

    public override void PerTickLocal(double delta)
    {

    }

    public override void PerFrameLocal(double delta)
    {

    }

    protected override void ConfigureInput()
    {
        input = Global.gameState.PlayerInputs[controllingPlayerID];
    }

    protected override void CreateAndConfigureCamera()
    {
        cameraParent = GetNode<Node3D>("cameraParent");
        PlayerCamera cam = new();
        cameraParent.AddChild(cam);
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
}

[MessagePackObject]
public struct GhostUpdate
{
    [Key(0)]
    public Vector3 Position;

    [Key(1)]
    public Vector3 Rotation;
}