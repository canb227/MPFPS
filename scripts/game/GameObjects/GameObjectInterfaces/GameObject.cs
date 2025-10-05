using System;
using System.Collections.Generic;
using static GameState;

public interface GameObject
{
    ulong id { get; set; }
    float priority { get; set; }
    float priorityAccumulator { get; set; }
    ulong authority { get; set; }
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

