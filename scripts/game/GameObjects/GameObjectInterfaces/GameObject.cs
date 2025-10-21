using Godot;
using System;
using System.Collections.Generic;
using static GameState;

public interface GameObject
{
    [Export]
    ulong id { get; set; }

    [Export]
    float priority { get; set; }

    [Export]
    float priorityAccumulator { get; set; }

    [Export]
    ulong authority { get; set; }

    [Export]
    GameObjectType type { get; set; }
    bool dirty { get; set; }
    bool sleeping { get; set; }
    bool destroyed { get; set; }
    bool predict {  get; set; }
    void PerTickAuth(double delta);
    void PerFrameAuth(double delta);
    void PerTickLocal(double delta);
    void PerFrameLocal(double delta);
    void PerTickShared(double delta);
    void PerFrameShared(double delta);
    void ProcessStateUpdate(byte[] update);
    byte[] GenerateStateUpdate();
    string GenerateStateString();
    bool InitFromData(GameObjectConstructorData data); 

}

