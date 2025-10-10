using Godot;
using MessagePack;
/// <summary>
/// data class that holds all of the options that can be set for the gameMode. Whole thing gets shoved over the network.
/// </summary>
[MessagePackObject]
public class GameModeOptions
{
    [Key(7)]
    public GameModeType gameMode = GameModeType.TTT;

    [Key(0)]
    public string selectedMapScenePath = "res://scenes/world/debugPlatform.tscn";

    [Key(1)]
    public bool debugMode = false;

    [Key(2)]
    public float percentTraitors = 0.2f;

    [Key(3)]
    public int maxTraitors = 2;

    [Key(4)]
    public int minTraitors = 1;

    [Key(5)]
    public float roleAssignmentDelay = 5;

    [Key(6)]
    public float newRoundDelay = 2;


}

