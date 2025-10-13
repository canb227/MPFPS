using Godot;
using ImGuiGodot.Internal;
using MessagePack;
using System;
using System.Collections.Generic;
/// <summary>
/// Class  that holds a single player's input data. This gets sent once per frame to all peers.
/// </summary>
[MessagePackObject]
public partial class PlayerInputData
{
    [Key(0)]
    public ulong playerID;

    [Key(1)]
    public Vector2 MovementInputVector;

    [Key(2)]
    public Vector2 LookInputVector;

    [Key(3)]
    public ActionFlags actions;

    public bool JustPressed(ActionFlags lastTickActions, ActionFlags action)
    {
        if (!lastTickActions.HasFlag(action) && actions.HasFlag(action))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool JustReleased(ActionFlags lastTickActions, ActionFlags action)
    {
        if (lastTickActions.HasFlag(action) && !actions.HasFlag(action))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}

