using Godot;
using System.Collections.Generic;

public class PlayerInputMap
{
    public List<string> InputActionList { get; set; } = [
        "MOVE_FORWARD",
        "MOVE_BACKWARD",
        "MOVE_LEFT",
        "MOVE_RIGHT",
        "JUMP",
        "FIRE",
        "AIM",
        "USE",
        "SPRINT",
        "CROUCH",
    ];
    public Dictionary<int, string> KeyboardKeyCodeToActionMap { get; set; } =  new()       
        {

            { (int)Key.W, "MOVE_FORWARD" },
            { (int)Key.A, "MOVE_LEFT" },
            { (int)Key.S, "MOVE_BACKWARD" },
            { (int)Key.D, "MOVE_RIGHT" },

            { (int)Key.F, "USE" },

            { (int)Key.Space, "JUMP" },
            { (int)Key.Shift, "SPRINT" },
            { (int)Key.C, "CROUCH" },

        };

    public Dictionary<int, string> MouseButtonToActionMap { get; set; } = new()
        {
            { (int)MouseButton.Left, "FIRE" },
            { (int)MouseButton.Right, "AIM" },
        };

    public Dictionary<int, string> JoypadButtonToActionMap { get; set; } = new()
        {
            { (int)JoyButton.A, "JUMP" },
        };

}