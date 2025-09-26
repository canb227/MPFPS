using Godot;
using MessagePack;
/// <summary>
/// Struct that defines a single network message that updates the state of a single IGameObject.
/// </summary>
[MessagePackObject]
public struct StateUpdatePacket
{
    /// <summary>
    /// The ID of the object to update
    /// </summary>
    [Key(0)]
    public ulong objectID;

    /// <summary>
    /// Byte array that contains the state data for the object.
    /// </summary>
    [Key(1)]
    public byte[] data;

    /// <summary>
    /// IGameObject Type enum value - must match type of object with the given ID
    /// </summary>
    [Key(2)]
    public GameObjectType type;

    /// <summary>
    /// Auto populated with the current tick when sent.
    /// </summary>
    [Key(3)]
    public ulong tick;

    /// <summary>
    /// auto populated with the local user's steamID when sent
    /// </summary>
    [Key(4)]
    public ulong sender;

    public StateUpdatePacket(ulong id, byte[] data, GameObjectType type)
    {
        this.objectID = id;
        this.data = data;
        this.type = type;
        this.tick = Global.gameState.tick;
        this.sender = Global.steamid;
    }
}

