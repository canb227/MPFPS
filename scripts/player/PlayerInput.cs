using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[GlobalClass]
public partial class PlayerInput : MultiplayerSynchronizer
{

    [Export]
    public Vector2 movementInputVector;

    [Export]
    public Vector2 mouseInputVector;

    [Export]
    public Vector2 mouseInputAccumulator;

    public override void _Ready()
    {
        if (!IsMultiplayerAuthority())
        {
            SetProcess(false);
        }
    }

    public override void _Process(double delta)
    {
        movementInputVector = Input.GetVector("MOVE_LEFT", "MOVE_RIGHT", "MOVE_FORWARD", "MOVE_BACKWARD");
    }

    public override void _Input(InputEvent @event)
    {
        if(Input.MouseMode == Input.MouseModeEnum.Captured && @event is InputEventMouseMotion iemm)
        {
            mouseInputVector = iemm.ScreenRelative;
            mouseInputAccumulator += iemm.ScreenRelative;
        }
    }

}

