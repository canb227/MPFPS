using Godot;
using System;

public enum SoundType
{
    Generic,
    Fire,
    Bullet,
}
public partial class CharacterSoundManager : Node
{
    public void PlayDamageSound(AudioStreamPlayer3D audioStream, SoundType soundType)
    {
        RPCManager.RPC(this, "rpc_PlayDamageSound", [audioStream, soundType]);
    }
    
    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void rpc_PlayDamageSound(AudioStreamPlayer3D audioStream, SoundType soundType)
    {
        if (soundType == SoundType.Generic)
        {
            PlayGenericPainSound(audioStream);
        }
        if (soundType == SoundType.Bullet)
        {
            PlayBulletPainSound(audioStream);
        }
        if (soundType == SoundType.Fire)
        {
            PlayFirePainSound(audioStream);
        }
    }

    public void PlayDeathSound(AudioStreamPlayer3D audioStream)
    {
        // Put your audio file paths in an array
        string[] deathSounds =
        {
            "res://assets/audio/character/pl_pain5.wav",
            "res://assets/audio/character/pl_pain6.wav",
            "res://assets/audio/character/pl_pain7.wav"
        };

        // Pick one at random
        Random rand = new();
        int index = rand.Next(deathSounds.Length);

        // Load and play it
        audioStream.Stream = GD.Load<AudioStream>(deathSounds[index]);
        audioStream.Play();
    }

    private void PlayGenericPainSound(AudioStreamPlayer3D audioStream)
    {
        // Put your audio file paths in an array
        string[] painSounds =
        {
            "res://assets/audio/character/pl_pain5.wav",
            "res://assets/audio/character/pl_pain6.wav",
            "res://assets/audio/character/pl_pain7.wav"
        };

        // Pick one at random
        Random rand = new();
        int index = rand.Next(painSounds.Length);

        // Load and play it
        audioStream.Stream = GD.Load<AudioStream>(painSounds[index]);
        audioStream.Play();
    }

    private void PlayFirePainSound(AudioStreamPlayer3D audioStream)
    {
        // Put your audio file paths in an array
        if(audioStream.Playing)
        {
            return;
        }
        string[] painSounds =
        {
            "res://assets/audio/character/pl_burnpain1.wav",
            "res://assets/audio/character/pl_burnpain2.wav",
            "res://assets/audio/character/pl_burnpain3.wav"
        };

        // Pick one at random
        Random rand = new();
        int index = rand.Next(painSounds.Length);

        // Load and play it
        audioStream.Stream = GD.Load<AudioStream>(painSounds[index]);
        audioStream.Play();
    }

    private void PlayBulletPainSound(AudioStreamPlayer3D audioStream)
    {
        // Put your audio file paths in an array
        string[] painSounds =
        {
            "res://assets/audio/character/flesh_impact_bullet1.wav",
            "res://assets/audio/character/flesh_impact_bullet2.wav",
            "res://assets/audio/character/flesh_impact_bullet3.wav"
        };

        // Pick one at random
        Random rand = new();
        int index = rand.Next(painSounds.Length);

        // Load and play it
        audioStream.Stream = GD.Load<AudioStream>(painSounds[index]);
        audioStream.Play();
    }
}