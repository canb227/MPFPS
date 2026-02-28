using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using MessagePack;

[GlobalClass]
public partial class GOGenerator : GOBaseStaticBody
{
	[Export] Area3D generatorArea;
	public float generatorHealthInSecondsPerRobot = 0.0f;
	public float generatorMaxHealth = 900.0f;
	public override void _Ready()
	{
		base._Ready();
		Global.gameState.gameModeManager.generator = this;
		generatorHealthInSecondsPerRobot = generatorMaxHealth;
		generatorArea.AreaEntered += OnBodyEntered;
		generatorArea.AreaExited += OnBodyExited;
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

	private bool announcedAttacked;
	private float timeSinceNoEnemy;
	public override void _PhysicsProcess(double delta)
	{
		if(Global.Lobby.bIsLobbyHost)
		{
			if (robotsInArea > 0)
			{
				int cappedRobotsInArea = Math.Max(20, robotsInArea);
				generatorHealthInSecondsPerRobot -= (float)delta * cappedRobotsInArea; //max 20 robots deal damage
				timeSinceNoEnemy = 0;
				if(generatorHealthInSecondsPerRobot <= 0)
				{
					//ignore generator if we have started end of round evacuation
					if(!Global.gameState.gameModeManager.evacuationStarted && Global.gameState.gameModeManager.roundStarted)
					{
						//traitors win
						RPCManager.RPC(Global.gameState.gameModeManager, "TraitorsWin", []);
					}
				}
				else if(generatorHealthInSecondsPerRobot <= 30 && !announcedAttacked)
				{
					//announcer alert
					announcedAttacked = true;
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
				if (generatorHealthInSecondsPerRobot > generatorMaxHealth)
					generatorHealthInSecondsPerRobot = generatorMaxHealth;
				else
					generatorHealthInSecondsPerRobot += (float)delta;
			}
		}
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
