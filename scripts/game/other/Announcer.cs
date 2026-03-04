using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using MessagePack;

[GlobalClass]
public partial class Announcer : GOBaseStaticBody
{
    [Export] public AnimationPlayer animationPlayer;
    [Export] public AudioStreamPlayer3D audioStreamPlayerSiren;
    [Export] public AudioStreamPlayer3D audioStreamPlayerMusic;
    [Export] public AudioStreamPlayer3D audioStreamPlayerAlert;
    bool evacuationStarted;
    public AnnouncerState announcerState;
    public override void _Ready()
    {
        base._Ready();
        GameModeManager.EvacuationStarted += EvacuationStarted;
        GameModeManager.SwarmIncoming += SwarmIncoming;
        GameModeManager.SwarmStarted += SwarmStarted;
        GameModeManager.SwarmDefeated += SwarmDefeated;
        GameModeManager.GeneratorSafe += GeneratorSafe;
        GameModeManager.GeneratorUnderAttack += GeneratorUnderAttack;
    }
    public override void _Notification(int what)
    {
        if (what == NotificationPredelete)
        {
            GameModeManager.EvacuationStarted -= EvacuationStarted;
            GameModeManager.SwarmIncoming -= SwarmIncoming;
            GameModeManager.SwarmStarted -= SwarmStarted;
            GameModeManager.SwarmDefeated -= SwarmDefeated;
            GameModeManager.GeneratorSafe -= GeneratorSafe;
            GameModeManager.GeneratorUnderAttack -= GeneratorUnderAttack;
        }
    }



    public void SwarmIncoming()
    {
        announcerState = AnnouncerState.HORDE;
        animationPlayer.Play("swarmIncoming");
        audioStreamPlayerSiren.Stream = GD.Load<AudioStream>("res://assets/audio/announcer/alarm_citizen_loop1.wav");
        audioStreamPlayerSiren.Play();
    }

    public void SwarmStarted()
    {
        if(announcerState == AnnouncerState.HORDE)
        {
            announcerState = AnnouncerState.NONE;
            animationPlayer.Stop();
            audioStreamPlayerSiren.Stop();
        }
        audioStreamPlayerMusic.Stream = GD.Load<AudioStream>("res://assets/audio/music/horde/Hordedrums.mp3");
        audioStreamPlayerMusic.Play();
    }

    public async void SwarmDefeated()
    {
        await FadeOut(audioStreamPlayerMusic, 10f);
    }

    public async Task FadeOut(AudioStreamPlayer3D player, float duration = 1f)
    {
        float startVolume = player.VolumeDb;
        float time = 0f;

        while (time < duration)
        {
            time += (float)GetProcessDeltaTime();
            float t = time / duration;
            player.VolumeDb = Mathf.Lerp(startVolume, -80f, t); // -80 dB = silent
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }

        player.Stop();
        player.VolumeDb = startVolume; // optional reset
    }


    public void EvacuationStarted()
    {
        announcerState = AnnouncerState.EVACUATION;
        evacuationStarted = true;
        animationPlayer.Play("evacuationStart");
        audioStreamPlayerSiren.Stream = GD.Load<AudioStream>("res://assets/audio/announcer/alarm_citizen_loop1.wav");
        audioStreamPlayerSiren.Play();
    }

    public void GeneratorSafe()
    {
        // announcerState = AnnouncerState.NONE;
        // animationPlayer.Stop();
        // audioStreamPlayerSiren.Stop();
    }
    public void GeneratorUnderAttack()
    {
        if(announcerState != AnnouncerState.EVACUATION)
        {
            announcerState = AnnouncerState.GENERATOR;
            //animationPlayer.Play("generatorUnderAttack");
            //audioStreamPlayerSiren.Stream = GD.Load<AudioStream>("res://assets/audio/announcer/alarm_citizen_loop1.wav");
            audioStreamPlayerSiren.Stream = GD.Load<AudioStream>("res://assets/audio/announcer/baseunderattacksc.mp3");
            audioStreamPlayerSiren.Play();
        }
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

public enum AnnouncerState
{
    NONE,
    HORDE,
    GENERATOR,
    EVACUATION,
}