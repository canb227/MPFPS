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
    public float percentTraitors = 0.333333f;

    [Key(3)]
    public int maxTraitors = 8;

    [Key(4)]
    public float percentManagers = 0.166666f;

    [Key(5)]
    public float roleAssignmentDelay = 5;

    [Key(6)]
    public float newRoundDelay = 3;

    [Key(8)]
    public bool manualOverride = false;
    [Key(9)]
    public int manualTraitorCount = 0;
    [Key(10)]
    public int manualManagerCount = 0;
    [Key(11)]
    public int itemsPerPackage = 3;
    [Key(12)]
    public double roundTime = 900;


}

