using Godot;
using Godot.Collections;
using ImGuiNET;

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
            Global.gameState.PlayerInputs[Global.steamid].MovementInputVector = Input.GetVector("MOVE_FORWARD", "MOVE_BACKWARD", "MOVE_LEFT", "MOVE_RIGHT");
            foreach (string action in Global.InputMap.InputActionList.Keys)
            {
                if (@event.IsAction(action))
                {
                    if(@event.IsPressed())
                    {
                        Global.gameState.PlayerInputs[Global.steamid].actions = Global.gameState.PlayerInputs[Global.steamid].actions | flagMap[action];
                    }
                    else if (@event.IsReleased())
                    {
                        Global.gameState.PlayerInputs[Global.steamid].actions = Global.gameState.PlayerInputs[Global.steamid].actions & ~flagMap[action];
                    }
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

