using Godot;
using MessagePack;
/// <summary>
/// data class that holds all of the options that can be set for the gameMode. Whole thing gets shoved over the network.
/// </summary>
[MessagePackObject]
public class GameModeOptions
{
    [Key(7)] public GameModeType gameMode = GameModeType.TTT;

    [Key(0)] public string selectedMapScenePath = "res://scenes/world/debugPlatform.tscn";

    [Key(1)] public bool debugMode = false;

    [Key(2)] public float percentTraitors = 0.333333f;

    [Key(4)] public float percentManagers = 0.166666f;

    [Key(5)] public float roleAssignmentDelay = 10;

    [Key(6)] public float newRoundDelay = 5;

    [Key(8)] public bool manualTeamOverride = true;
    [Key(9)] public int manualTraitorCount = 0;
    [Key(10)] public int manualManagerCount = 1;
    [Key(11)] public int itemsPerPackage = 1;
    [Key(12)] public bool usePackageOverride = false; 
    [Key(13)] public int timePerKillEdit = 60; 
    [Key(14)] public float mainhordeDelay = 300.0f;
    [Key(15)] public float timeAddedPerPackage = 120f; 
    [Key(16)] public double roundTime = 480;
    [Key(17)] public float hordeSizeMultiplier = 20.0f;
    [Key(18)] public float endgameHordeSizeMultiplier = 3.0f;
    [Key(19)] public bool warehouseRobots = false;
    [Key(20)] public bool hordeRobots = true;
}

