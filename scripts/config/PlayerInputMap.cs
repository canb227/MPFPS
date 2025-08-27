using Godot;
using System.Collections.Generic;

/// <summary>
/// Stores input mapping information for a given user. This gets saved/loaded to disk for persistence between sessions.
/// Changing the dictionaries DOES NOT actually rebind a key until the game is restarted. Use the functions in InputMapManager for dynamic rebinding instead.
/// This one is a class instead of a struct because I wanted to use dictionaries and those are hard to turn into bytes, so this using TOML instead.
/// I'm not super happy with that but it works and I can change it later.
/// </summary>
public class PlayerInputMap
{

    public Dictionary<int, string> KeyboardKeyCodeToActionMap { get; set; } = new()
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