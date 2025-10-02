
using Godot;
using ImGuiNET;
using MessagePack;
using System;

[GlobalClass]
public partial class Tony : GOBasePlayerCharacter
{
    //SteamID of the player currently controlling this character
    public override ulong controllingPlayerID { get; set; }

    //Currently unused
    public override Team team { get; set; }

    //auto-generated cached reference to Global.gameState.playerInputs[controllingPlayerID]
    public override PlayerInputData input { get; set; }

    //Node that holds the camera. Is only needed if your camera rotation applies rotation to something seperate than the entire character
    //like a head
    public override Node3D lookRotationNode { get; set; }

    //reference to the camera attached to this character
    private PlayerCamera cam { get; set; }
    public override Node3D thirdPersonModel { get; set; }
    public override Node3D firstPersonModel { get; set; }
    public override Node3D cameraLocationNode { get; set; }
    public override Role role { get; set; }

    //Runs as soon as this character is added to the Godot SceneTree
    public override void _Ready()
    {
        base._Ready();
        priority = 100;
    }

    //Objects are in charge of managing their own netcode - and get to format and handle their update packets as they see fit.
    //Objects will typically get/generate state updates every 1-5 ticks

    //In this function, the object consumes a blob of bytes that represents the authoratative state of the object
    //Only run on machines of players that are not controlling this object
    public override void ProcessStateUpdate(byte[] _update)
    {
        TonyUpdatePacket update = MessagePackSerializer.Deserialize<TonyUpdatePacket>(_update);
        GlobalRotation = update.Rotation;
        GlobalPosition = update.Position;
    }

    //In this function, the object generates a blob of bytes that represents the authoratative state of the object
    //This one is only run on the machine of the player controlling this object
    public override byte[] GenerateStateUpdate()
    {
        TonyUpdatePacket update = new TonyUpdatePacket();
        update.Rotation = GlobalRotation;
        update.Position = GlobalPosition;
        return MessagePackSerializer.Serialize(update);
    }

    //This is Godot's _PhysicsProcess(double delta) for all intents and purposes
    //Any code here is run once per tick (60 ticks per second) - but only on the machine of the player controlling it.
    //Changes to the physical state of the character should be done here
    public override void PerTickAuth(double delta)
    {
        //Godot CharacterBody has a Velocity variable you can get/set, but its in global space and thats annoying
        //The below just takes our current velocity and rotates it so that forward (Z Axis) is forward, even as we rotate the character.
        Vector3 localVelocity = PCUtils.GetLocalVelocity(this);




        //modify the localVelocity based on input - see the Actions Enum in InputMapManager (instance in Global.InputMap)


        if (!IsOnFloor())
        {
            localVelocity.Y -= (float)(9.8 * delta);
        }
        else
        {
            if (input.actions.HasFlag(ActionFlags.Jump))
            {
                localVelocity.Y = 10;
            }
        }


            //After we're done modifiying the local velocity vector, we have to transform it back to global, thats what Godot expects.
            Velocity = PCUtils.GlobalizeVector(this, localVelocity);

        //Built-in Godot function, steps the physics one tick for this object by using the Velocity variable.
        MoveAndSlide();
    }

    //This is Godot's _Process(double delta) for all intents and purposes
    //Any code here is run once per frame - but only on the machine of the player controlling it.
    public override void PerFrameAuth(double delta)
    {

        
        
        //Right Bracket (']') toggles all debug screens, you can add your own here.
        if (Global.DrawDebugScreens)
        {
            ImGui.Begin("PC (Tony) Debug");
            ImGui.Text("InputMvVector: " + input.MovementInputVector.ToString());
            ImGui.Text("InputLookVector: " + input.LookInputVector.ToString());
            ImGui.Text($"Actions flag: {input.actions}");
            ImGui.End();
        }
    }

    //Runs once per tick, but only on machines that are not controlling this character
    //For network prediction and remediation purposes, can be ignored
    public override void PerTickLocal(double delta)
    {
    }

    //Runs once per frame, but only on machines that are not controlling this character
    //For network prediction and remediation purposes, can be ignored
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
        throw new NotImplementedException();
    }

    public override void PerTickShared(double delta)
    {
        throw new NotImplementedException();
    }

    public override void PerFrameShared(double delta)
    {
        throw new NotImplementedException();
    }
}

[MessagePackObject]
public struct TonyUpdatePacket
{
    [Key(0)]
    public Vector3 Position;

    [Key(1)]
    public Vector3 Rotation;
}