using Godot;
using System;

public enum PainSoundType
{
    None,
    Generic,
    Fire,
    Bullet,
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

    public void PlayDamageSound(AudioStreamPlayer3D audioStream, PainSoundType soundType)
    {
        if (soundType == PainSoundType.None)
        {
            
        }
        else if (soundType == PainSoundType.Generic)
        {
            PlayGenericPainSound(audioStream);
        }
        else if (soundType == PainSoundType.Bullet)
        {
            PlayBulletPainSound(audioStream);
        }
        else if (soundType == PainSoundType.Fire)
        {
            PlayFirePainSound(audioStream);
        }
        else
        {
            Logging.Error("Invalid SoundType for Pain: " + soundType.ToString(), "CharacterSoundManager");
        }
    }

    public void PlayMovementSound(AudioStreamPlayer3D audioStream, MovementSoundType soundType, bool isJump)
    {
        if (soundType == MovementSoundType.None)
        {
            
        }
        else if (soundType == MovementSoundType.Generic)
        {
            PlayGenericFootstepSound(audioStream, isJump);
        }
        else
        {
            Logging.Error("Invalid SoundType for Movement: " + soundType.ToString(), "CharacterSoundManager");
        }
    }
    
    public void PlayGenericFootstepSound(AudioStreamPlayer3D audioStream, bool isJump)
    {
        //increase step sound if this is a jump
        if (isJump)
        {
            audioStream.VolumeDb = 6.0f;
        }
        else
        {
            audioStream.VolumeDb = 0.0f;
            if (audioStream.GetPlaybackPosition() < 0.4 && audioStream.Playing)
            {
                return;
            }
        }
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
        audioStream.PitchScale = pitch;
        audioStream.Stream = GD.Load<AudioStream>(stepSounds[index]);
        audioStream.Play();
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