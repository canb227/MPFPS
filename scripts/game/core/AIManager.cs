using Godot;
using System;

public partial class AIManager : Node
{
    internal void GameStartAsHost()
    {
        Logging.Log($"Starting server-side AI manager", "AIManager");
    }
}