using Godot;
using System;

public enum PainSoundType
{
    None,
    Generic,
    Fire,
    Bullet,
    Falling,
}
public enum MovementSoundType
{
    None,
    Generic,
    Duct,
    Ladder,
    Metal,
    Grate,
    Tile,
    Wood,
}
public partial class CharacterSoundManager : Node
{
    private float stepTimer;
    private int lastStepIndex;

    /// <summary>
    /// Plays a hurt sound based on the requested damage type.
    /// </summary>
    /// <param name="VolumeDb">
    /// Volume in decibels, defaults to whatever it is set to in the editor, 0 is normal volume, negatives are quieter
    /// </param>
    public void PlayDamageSound(AudioStreamPlayer3D audioStream, PainSoundType soundType, int VolumeDb = 0)
    {
        if (soundType == PainSoundType.None)
        {

        }
        else if (soundType == PainSoundType.Generic)
        {
            PlayGenericPainSound(audioStream, VolumeDb);
        }
        else if (soundType == PainSoundType.Bullet)
        {
            PlayBulletPainSound(audioStream, VolumeDb);
        }
        else if (soundType == PainSoundType.Fire)
        {
            PlayFirePainSound(audioStream, VolumeDb);
        }
        else if (soundType == PainSoundType.Falling)
        {
            PlayFallingPainSound(audioStream, VolumeDb);
        }
        else
        {
            Logging.Error("Invalid SoundType for Pain: " + soundType.ToString(), "CharacterSoundManager");
        }
    }

    public void PlayMovementSound(AudioStreamPlayer3D audioStream, MovementSoundType soundType, bool isJump, bool isRunning)
    {
        if (soundType == MovementSoundType.None)
        {
            
        }
        else if (soundType == MovementSoundType.Generic)
        {
            PlayGenericFootstepSound(audioStream, isJump, isRunning);
        }
        else
        {
            Logging.Error("Invalid SoundType for Movement: " + soundType.ToString(), "CharacterSoundManager");
        }
    }
    float audioSoundTime = 0f;
    public override void _Process(double delta)
    {
        base._Process(delta);
        audioSoundTime += (float)delta;
    }

    public void PlayGenericFootstepSound(AudioStreamPlayer3D audioStream, bool isJump, bool isRunning)
    {
        float audioTime = 0;
        if(isRunning)
        {
            audioTime = 0.0f;
        }
        else
        {
            audioTime = 0.4f;
        }
        //increase step sound if this is a jump
        if (isJump)
        {
            audioStream.VolumeDb = 6.0f;
        }
        else
        {
            audioStream.VolumeDb = 0.0f;
            if (audioSoundTime < audioTime && audioStream.Playing)
            {
                return;
            }
        }
        audioSoundTime = 0f;
        //if its a jump play the new loud step immediately otherwise skip as we have a previous step sound playing

        string[] stepSounds =
        {
            "res://assets/audio/character/footsteps/concrete1.wav",
            "res://assets/audio/character/footsteps/concrete2.wav",
            "res://assets/audio/character/footsteps/concrete3.wav",
            "res://assets/audio/character/footsteps/concrete4.wav",
        };
        Random rand = new();
        int index;
        do
        {
            index = rand.Next(stepSounds.Length);
        } while (index == lastStepIndex && stepSounds.Length > 1);

        lastStepIndex = index;

        //pitch variation (0.95–1.05)
        float pitch = 0.95f + (float)rand.NextDouble() * 0.1f;
        audioStream.Call("play_stream", GD.Load<AudioStream>(stepSounds[index]), 0f, 0f, pitch);
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
        //audioStream.Stream = GD.Load<AudioStream>(deathSounds[index]);
        audioStream.Call("play_stream", GD.Load<AudioStream>(deathSounds[index]), 0f, 0f, 1f);

    }

    private void PlayGenericPainSound(AudioStreamPlayer3D audioStream, int VolumeDb)
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
        // audioStream.Stream = GD.Load<AudioStream>(painSounds[index]);
        // audioStream.VolumeDb = VolumeDb;
        audioStream.Call("play_stream", GD.Load<AudioStream>(painSounds[index]), 0f, VolumeDb, 1f);
    }

    private void PlayFirePainSound(AudioStreamPlayer3D audioStream, int VolumeDb)
    {
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
        // audioStream.Stream = GD.Load<AudioStream>(painSounds[index]);
        // audioStream.VolumeDb = VolumeDb;
        audioStream.Call("play_stream", GD.Load<AudioStream>(painSounds[index]), 0f, VolumeDb, 1f);
    }

    private void PlayBulletPainSound(AudioStreamPlayer3D audioStream, int VolumeDb)
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
        // audioStream.Stream = GD.Load<AudioStream>(painSounds[index]);
        // audioStream.VolumeDb = VolumeDb;
        audioStream.Call("play_stream", GD.Load<AudioStream>(painSounds[index]), 0f, VolumeDb, 1f);
    }

    private void PlayFallingPainSound(AudioStreamPlayer3D audioStream, int VolumeDb)
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
        // audioStream.Stream = GD.Load<AudioStream>(painSounds[index]);
        // audioStream.VolumeDb = VolumeDb;
        audioStream.Call("play_stream", GD.Load<AudioStream>(painSounds[index]), 0f, VolumeDb, 1f);
    }

    
}