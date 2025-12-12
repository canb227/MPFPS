using System.Collections.Generic;
using Godot;
using Godot.Collections;
using MessagePack;

[GlobalClass]
public partial class GOGenerator : GOBaseStaticBody
{
	[Export] Area3D generatorArea;
	public float generatorHealthInSeconds = 0.0f;
	public float generatorMaxHealth = 45.0f;
	public override void _Ready()
	{
		base._Ready();
		Global.gameState.gameModeManager.generator = this;
		generatorArea.BodyEntered += OnBodyEntered;
		generatorArea.BodyExited += OnBodyExited;
	}
	private int robotsInArea = 0;
	private void OnBodyEntered(Node3D body)
	{
		if (body.IsInGroup("enemies")) // or body.IsInGroup("robots")
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
				generatorHealthInSeconds -= (float)delta;
				timeSinceNoEnemy = 0;
				if(generatorHealthInSeconds <= 0)
				{
					//ignore generator if we have started end of round evacuation
					if(!Global.gameState.gameModeManager.evacuationStarted)
					{
						//traitors win
						RPCManager.RPC(Global.gameState.gameModeManager, "TraitorsWin", []);
					}
				}
				else if(generatorHealthInSeconds <= 30 && !announcedAttacked)
				{
					//announcer alert
					announcedAttacked = true;
					Global.gameState.gameModeManager.TriggerGeneratorUnderAttack();
				}
			}
			else
			{
				timeSinceNoEnemy += (float)delta;   
				//don't consider it "safe" until no enemies are present for 1 second
				if(announcedAttacked && timeSinceNoEnemy > 1)
				{
					announcedAttacked = false;
					Global.gameState.gameModeManager.TriggerGeneratorSafe();
				}
				if (generatorHealthInSeconds > generatorMaxHealth)
					generatorHealthInSeconds = generatorMaxHealth;
				else
					generatorHealthInSeconds += (float)delta;
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
