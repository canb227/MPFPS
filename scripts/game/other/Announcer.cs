using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using MessagePack;

[GlobalClass]
public partial class Announcer : GOBaseStaticBody
{
    [Export] public AnimationPlayer animationPlayer;
    [Export] public AudioStreamPlayer3D audioStreamPlayerSiren;
    bool evacuationStarted;
    public override void _Ready()
    {
        base._Ready();
        GameModeManager.EvacuationStarted += EvacuationStarted;
        GameModeManager.SwarmIncoming += SwarmIncoming;
        GameModeManager.SwarmStarted += SwarmStarted;
    }
    public override void _Notification(int what)
    {
        if (what == NotificationPredelete)
        {
            GameModeManager.EvacuationStarted -= EvacuationStarted;
            GameModeManager.SwarmIncoming -= SwarmIncoming;
            GameModeManager.SwarmStarted -= SwarmStarted;
        }
    }

    public void SwarmIncoming()
    {
        animationPlayer.Play("swarmIncoming");
        audioStreamPlayerSiren.Call("play_stream", GD.Load<AudioStream>("res://assets/audio/announcer/alarm_citizen_loop1.wav"), 0f, 0f, 1f);
    }
    public void SwarmStarted()
    {
        if(!evacuationStarted)
        {
            animationPlayer.Stop();
            audioStreamPlayerSiren.Stop();
        }
    }

    public void EvacuationStarted()
    {
        evacuationStarted = true;
        animationPlayer.Play("evacuationStart");
        audioStreamPlayerSiren.Call("play_stream", GD.Load<AudioStream>("res://assets/audio/announcer/alarm_citizen_loop1.wav"), 0f, 0f, 1f);
    }
    public override string GenerateStateString()
    {
        return $"I am a announcer :)";
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
        
    }
    public override void ProcessStateUpdate(byte[] update)    
    {
        
    }
}