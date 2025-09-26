using Godot;

public partial class PlayerInputHandler : Node
{
    public override void _Ready()
    {
        Global.gameState.PlayerInputs[Global.steamid] = new PlayerInputData();
        Global.gameState.PlayerInputs[Global.steamid].playerID = Global.steamid;
        foreach (string action in Global.InputMap.InputActionList.Keys)
        {
            Global.gameState.PlayerInputs[Global.steamid].actions.Add(action, false);
        }
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
                    Global.gameState.PlayerInputs[Global.steamid].actions[action] = @event.IsPressed();
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
}

