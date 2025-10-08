using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Stores input mapping information for a given user. This gets saved/loaded to disk for persistence between sessions.
/// Changing the dictionaries DOES NOT actually rebind a key until the game is restarted. Use the functions in InputMapManager for dynamic rebinding instead.

/// </summary>
public class PlayerInputMap
{

    public Dictionary<Key, ActionFlags> KeyboardKeyCodeToActionMap { get; set; } = new()
        {
            { Key.W, ActionFlags.MoveForward },
            { Key.A, ActionFlags.MoveLeft },
            { Key.S, ActionFlags.MoveBackward },
            { Key.D, ActionFlags.MoveRight },

            { Key.F, ActionFlags.Use },
            { Key.R, ActionFlags.Reload },

            { Key.Space, ActionFlags.Jump },
            { Key.Shift, ActionFlags.Sprint},
            { Key.C, ActionFlags.CrouchToggle },
            { Key.Z, ActionFlags.ProneToggle },

            { Key.Key1, ActionFlags.InventorySlot1 },
            { Key.Key2, ActionFlags.InventorySlot2  },
            { Key.Key3, ActionFlags.InventorySlot3  },
            { Key.Key4, ActionFlags.InventorySlot4  },
            { Key.Key5, ActionFlags.InventorySlot5  },

            { Key.Q, ActionFlags.LeanLeft },
            { Key.E, ActionFlags.LeanRight },

            { Key.Tab, ActionFlags.ScoreBoard },
            { Key.Escape, ActionFlags.Escape },
        };

    public Dictionary<MouseButton, ActionFlags> MouseButtonToActionMap { get; set; } = new()
        {
            { MouseButton.Left, ActionFlags.Fire },
            { MouseButton.Right, ActionFlags.Aim },
            { MouseButton.WheelUp, ActionFlags.NextSlot},
            { MouseButton.WheelDown, ActionFlags.PrevSlot},
        };


    //doesnt work yet
    public Dictionary<JoyButton, ActionFlags> JoypadButtonToActionMap { get; set; } = new()
        {
            { JoyButton.A, ActionFlags.Jump },
        };

}