using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using MessagePack;

[GlobalClass]
public partial class GOGenerator : GOBaseStaticBody
{
	[Export] Area3D generatorArea;
	[Export] public MeshInstance3D _outline;
	[Export] public AudioStreamPlayer3D idleAudio;
	[Export] public AudioStreamPlayer3D otherAudio;
	private bool outlineDesiredState;
	public float generatorHealthInSecondsPerRobot = 0.0f;
	public float generatorMaxHealth = 300.0f;
	private bool generatorPowered;
	public bool GetGeneratorPowered()
	{
		return generatorPowered;
	}
	public override void _Ready()
	{
		base._Ready();
		Global.gameState.gameModeManager.generator = this;
		generatorHealthInSecondsPerRobot = generatorMaxHealth;
		generatorArea.AreaEntered += OnBodyEntered;
		generatorArea.AreaExited += OnBodyExited;
		_outline.Visible = false;
		generatorPowered = true;
	}
	private int robotsInArea = 0;
	private void OnBodyEntered(Node3D body)
	{
		if (body.IsInGroup("enemies"))
		{
			robotsInArea++;
		}
	}

	private void OnBodyExited(Node3D body)
	{
		if (body.IsInGroup("enemies"))
		{
			robotsInArea--;
			if (robotsInArea < 0) robotsInArea = 0;
		}
	}

	public void SetHighlighted(bool enabled)
    {
		outlineDesiredState = enabled;
        _outline.Visible = enabled;
    }

	private bool announcedAttacked;
	private bool announcedCritical;
	private float timeSinceNoEnemy;
	public override void _PhysicsProcess(double delta)
	{
		if(Global.Lobby.bIsLobbyHost)
		{
			if (robotsInArea > 0)
			{
				int cappedRobotsInArea = Math.Min(30, robotsInArea);
				generatorHealthInSecondsPerRobot -= (float)delta * cappedRobotsInArea; //max 20 robots deal damage
				timeSinceNoEnemy = 0;
				if(generatorHealthInSecondsPerRobot <= 0)
				{
					//ignore generator dying if we have started end of round evacuation or if the round hasnt started
					if(!Global.gameState.gameModeManager.evacuationStarted && Global.gameState.gameModeManager.roundStarted)
					{
						//traitors win
						//RPCManager.RPC(Global.gameState.gameModeManager, "TraitorsWin", []);
						if(generatorPowered) 
						{
							TurnOffGenerator();
							Global.gameState.gameModeManager.TriggerSwarmDefeatedEvent(); //we end the swarm because the power is out and its cooked anyways
						}
					}
				}
				else if(generatorHealthInSecondsPerRobot <= (generatorMaxHealth*0.8) && !announcedAttacked)
				{
					//announcer alert
					announcedAttacked = true;
					RPCManager.RPC(Global.gameState.gameModeManager, "TriggerGeneratorUnderAttack", []);
				}
				else if(generatorHealthInSecondsPerRobot <= (generatorMaxHealth*0.4) && !announcedCritical)
				{
					announcedCritical = true;
					RPCManager.RPC(Global.gameState.gameModeManager, "TriggerGeneratorUnderAttack", []);
				}
			}
			else
			{
				timeSinceNoEnemy += (float)delta;   
				//don't consider it "safe" until no enemies are present for 1 second
				if(announcedAttacked && timeSinceNoEnemy > 1)
				{
					announcedAttacked = false;
					RPCManager.RPC(Global.gameState.gameModeManager, "TriggerGeneratorSafe", []);
				}
				if(announcedCritical && timeSinceNoEnemy > 1)
				{
					announcedCritical = false;
				}
				if (generatorHealthInSecondsPerRobot > generatorMaxHealth)
					generatorHealthInSecondsPerRobot = generatorMaxHealth;
				else
					generatorHealthInSecondsPerRobot += (float)delta;
				if(generatorHealthInSecondsPerRobot >= generatorMaxHealth/2)
				{
					if(!generatorPowered) TurnOnGenerator();
				}
			}
		}

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

	public void TurnOnGenerator()
	{
		generatorPowered = true;
		RPCManager.RPC(Global.gameState.gameModeManager, "TurnOnAllSpotLights", []);
		idleAudio.Play();
		otherAudio.Stop();
	}
	public void TurnOffGenerator()
	{
		generatorPowered = false;
		RPCManager.RPC(Global.gameState.gameModeManager, "TurnOffAllSpotLights", []);
		otherAudio.Play();
		idleAudio.Stop();
	}

	public override string GenerateStateString()
	{
		return $"I am the generator";
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
