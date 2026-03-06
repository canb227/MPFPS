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
    [Export] Area3D insideHelicopterArea { get; set; }
    [Export] AnimationPlayer animationPlayer { get; set; }
    [Export] PathFollow3D pathFollow3D { get; set; }
    [Export] MeshInstance3D rearRotorMesh { get; set; }
    [Export] MeshInstance3D frontRotorMesh { get; set; }
    [Export] Hurtbox frontRotorHurtbox { get; set; }
    [Export] Hurtbox rearRotorHurtbox { get; set; }
    [Export] public MeshInstance3D _outline;
    private bool outlineDesiredState;

    private float currentSpeed = 0f;
    private bool flyaway { get; set; }
    private float targetSpeed = 5f;   // max speed
    private float accelFactor = 0.5f;
    public bool started { get; set; }
    public bool isSpinning { get; set; }

    public override void _Ready()
    {
        base._Ready();
        GameModeManager.EvacuationStarted += EvacuationStarted;
        GameModeManager.EvacuationEnded += EvacuationEnded;
        Global.gameState.gameModeManager.helicopter = this;
        _outline.Visible = false;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationPredelete)
        {
            GameModeManager.EvacuationStarted -= EvacuationStarted;
            GameModeManager.EvacuationEnded -= EvacuationEnded;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        if (flyaway)
        {
            float factor = 1f - Mathf.Exp(-accelFactor * (float)delta);
            currentSpeed += (targetSpeed - currentSpeed) * factor;

            Vector3 up = Transform.Basis.Y;
            Vector3 forward = Transform.Basis.Z * 0.5f;
            Vector3 movement = (up + forward).Normalized() * currentSpeed;

            GlobalTranslate(movement * (float)delta);
        }

        //outline control
        if(outlineDesiredState)
        {
            if(Global.gameState.AIManager.localPlayer != null && this.GlobalPosition.DistanceSquaredTo(Global.gameState.AIManager.localPlayer.GlobalPosition) < 100f)
            {
                _outline.Visible = false;
            }
            else
            {
                _outline.Visible = true;
            }
        }
        else
		{
			_outline.Visible = false;
		}
    }
    

    

    public void SetHighlighted(bool enabled)
    {
        outlineDesiredState = enabled;
        _outline.Visible = enabled;
    }

    public void EvacuationStarted()
    {
        StartHelicopter();
    }

    public async void EvacuationEnded()
    {
        RPCManager.RPC(this, "HelicopterLeave" , []);
        if (Global.Lobby.bIsLobbyHost)
        {
            await ToSignal(GetTree().CreateTimer(5), SceneTreeTimer.SignalName.Timeout);
            //check who is inside
            List<BasicPlayerCharacter> basicPlayerCharacters = new();
            var overlaps = insideHelicopterArea.GetOverlappingBodies();
            GD.Print("OVERLAP HELICOPTER COUNT" + overlaps.Count);
            foreach (var body in overlaps)
            {
                if (body is BasicPlayerCharacter player)
                {
                    basicPlayerCharacters.Add(player);
                }
            }
            Global.gameState.gameModeManager.EvacuationLeft(basicPlayerCharacters);
        }
        Global.gameState.gameModeManager.helicopter.SetHighlighted(true);
    }

    public void StartHelicopter()
    {
        if (!started)
        {
            rearRotorAudio.Play();
            frontRotorAudio.Play();
            InteriorAudio.Play();
            isSpinning = true;
            frontRotorHurtbox.active = true;
            rearRotorHurtbox.active = true;
            animationPlayer.Play("ramp_down");
        }
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void HelicopterLeave()
    {
        animationPlayer.Play("ramp_up");
        flyaway = true;
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
