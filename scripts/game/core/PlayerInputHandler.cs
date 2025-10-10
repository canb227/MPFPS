using Godot;
using Godot.Collections;
using ImGuiNET;
using System;

public partial class PlayerInputHandler : Node
{



    public override void _Ready()
    {
        Logging.Log($"Starting local input gathering", "LocalInput");
        Global.gameState.PlayerInputs[Global.steamid] = new PlayerInputData();
        Global.gameState.PlayerInputs[Global.steamid].playerID = Global.steamid;
    }
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionType())
        {
            Global.gameState.PlayerInputs[Global.steamid].MovementInputVector = Input.GetVector("MoveForward", "MoveBackward", "MoveLeft", "MoveRight");
            foreach (string action in Enum.GetNames(typeof(ActionFlags)))
            {
                if (@event.IsAction(action))
                {

                    if(@event.IsPressed())
                    {
                        Global.gameState.PlayerInputs[Global.steamid].actions = Global.gameState.PlayerInputs[Global.steamid].actions | InputMapManager.actionNameToActionFlagMap[action];
                    }
                    //else if (@event.IsReleased())
                    //{
                    //    Global.gameState.PlayerInputs[Global.steamid].actions = Global.gameState.PlayerInputs[Global.steamid].actions & ~InputMapManager.actionNameToActionFlagMap[action];
                    //}
                }
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion m)
        {
            Global.gameState.PlayerInputs[Global.steamid].LookInputVector = m.Relative;
        }
        else if (@event is InputEventJoypadMotion j)
        {
            //Controller not supported
        }
    }

    public override void _Process(double delta)
    {
        if (Global.DrawDebugScreens)
        {
            //ImGui.Begin("input Debug");
            //ImGui.Text("InputMvVector: " + Global.gameState.PlayerInputs[Global.steamid].MovementInputVector.ToString());
            //ImGui.Text("InputLookVector: " + Global.gameState.PlayerInputs[Global.steamid].LookInputVector.ToString());
            //foreach (var actionEntry in Global.gameState.PlayerInputs[Global.steamid].actions)
            //{
            //    ImGui.Text($"{actionEntry.Key}:{actionEntry.Value}");
            //}
            //ImGui.End();
        }
    }
}

