using Godot;
using System;
using System.Collections.Generic;

public partial class AIManager : Node
{

    public List<GOBaseNPC> controlledNPCs = new();

    private GameModeOptions options;
    internal void GameStartAsHost()
    {
        Logging.Log($"Starting server-side AI manager", "AIManager");
        options = Global.gameState.gameModeManager.options;


    }

    public void PerTick(double delta)
    {
        foreach (GOBaseNPC npc in controlledNPCs)
        {

        }
    }

    public void SetGlobalAITarget(Node3D target)
    {
        foreach (GOBaseNPC npc in controlledNPCs)
        {
            npc.MovementTarget = target;
        }
    }



}