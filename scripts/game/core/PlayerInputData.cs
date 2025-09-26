using Godot;
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
    public Actions actions;
}

[Flags]
public enum Actions
{
    MoveForward = 1,
    MoveBackward = 2,
    MoveLeft = 4,
    MoveRight = 8,
    
    Jump = 16,
    Crouch = 32,
    Sprint = 64,

    Use = 128,
}