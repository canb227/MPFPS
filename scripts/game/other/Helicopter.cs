using System.Collections.Generic;
using Godot;
using Godot.Collections;
using MessagePack;

[GlobalClass]
public partial class Helicopter : GOBaseStaticBody
{
    [Export] Helicopter helicopterRoot { get; set; }
    [Export] AudioStreamPlayer3D rearRotorAudio { get; set; }
    [Export] AudioStreamPlayer3D frontRotorAudio { get; set; }
    [Export] AudioStreamPlayer3D InteriorAudio { get; set; }
    [Export] Announcer announcer { get; set; }
    [Export] Area3D insideHelicopterArea { get; set; }
    [Export] AnimationPlayer animationPlayer { get; set; }
    [Export] PathFollow3D pathFollow3D { get; set; }
    [Export] MeshInstance3D rearRotorMesh { get; set; }
    [Export] MeshInstance3D frontRotorMesh { get; set; }

    public bool started { get; set; }
    public bool isSpinning { get; set; }

    public override void _Ready()
    {
        base._Ready();
        GameModeManager.EvacuationStarted += EvacuationStarted;
        GameModeManager.EvacuationEnded += EvacuationEnded;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationPredelete)
        {
            GameModeManager.EvacuationStarted -= EvacuationStarted;
            GameModeManager.EvacuationEnded -= EvacuationEnded;
        }
    }

    public void EvacuationStarted()
    {
        StartHelicopter();
    }

    public async void EvacuationEnded()
    {
        HelicopterLeave();
        await ToSignal(GetTree().CreateTimer(5), SceneTreeTimer.SignalName.Timeout);
        //check who is inside
        List<BasicPlayerCharacter> basicPlayerCharacters = new();
        var overlaps = insideHelicopterArea.GetOverlappingBodies();
        foreach (var body in overlaps)
        {
            if (body is BasicPlayerCharacter player)
            {
                basicPlayerCharacters.Add(player);
            }
        }
        Global.gameState.gameModeManager.EvacuationLeft(basicPlayerCharacters);
    }

    public void StartHelicopter()
    {
        if (!started)
        {
            rearRotorAudio.Play();
            frontRotorAudio.Play();
            InteriorAudio.Play();
            isSpinning = true;
            animationPlayer.Play("ramp_down");
        }
    }
    
    public void HelicopterLeave()
    {
        animationPlayer.Play("ramp_up");
    }

    public override string GenerateStateString()
    {
        return $"I am the helicopter :)";
    }
    public override byte[] GenerateStateUpdate()
    {
        return new byte[0];
    }

    public override void PerFrameAuth(double delta)
    {
        
    }
    public override void PerFrameLocal(double delta) 
    {
        
    }
    public override void PerFrameShared(double delta) 
    {
        
    }
    public override void PerTickAuth(double delta)  
    {

    }
    public override void PerTickLocal(double delta)
    {

    }
    public override void PerTickShared(double delta)
    {
        //spin the helicopter blades
        if(isSpinning)
        {
            rearRotorMesh.RotateObjectLocal(new Vector3(0,0,1), 30f * (float)delta);
            frontRotorMesh.RotateObjectLocal(new Vector3(0,0,1), 30f * (float)delta);
        }
    }
    public override void ProcessStateUpdate(byte[] update)    
    {
        
    }
}
