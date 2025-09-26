using Godot;
using MessagePack;
/// <summary>
/// data class that holds all of the options and info that we need about a player. Whole thing gets shoved over the network.
/// </summary>
[MessagePackObject]
public class PlayerData
{
    [Key(0)]
    public ulong playerID;

}

