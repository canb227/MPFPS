using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using NetworkMessages;
using Steamworks;
using System;
using System.Runtime.InteropServices;

/// <summary>
/// Static library for useful networking functions
/// </summary>
public static class NetworkUtils
{

    public static TypeRegistry TypeRegistry = TypeRegistry.FromFiles(GameMessage.Descriptor.File);

    /// <summary>
    /// Extracts and converts the payload inside a Steam message into one of the Protobuf Parsable types. See [MessageType].Parse.ParseFrom().
    /// In practice, this is unwrapping the original payload object from the Steam Message wrapper.
    /// </summary>
    /// <param name="msg">a fully valid Steam Message</param>
    /// <returns>the payload of the Steam Message converted into a type compatible with Parser.ParseFrom()</returns>
    public static byte[] ConvertMessageToParsableType(SteamNetworkingMessage_t msg)
    {
        byte[] msgBytes = new byte[msg.m_cbSize];
        Marshal.Copy(msg.m_pData, msgBytes, 0, msg.m_cbSize);
        return msgBytes;
    }

    /// <summary>
    /// Returns true if the provided steamID corresponds to our own Steam Account, false otherwise.
    /// </summary>
    /// <param name="steamID"></param>
    /// <returns></returns>
    public static bool IsMe(ulong steamID)
    {
        return steamID == Global.steamid;
    }

    /// <summary>
    /// Returns true if the provided SteamWorks.Net Identity corresponds to our own Steam Account, false otherwise.
    /// </summary>
    /// <param name="identity"></param>
    /// <returns></returns>
    public static bool IsMe(SteamNetworkingIdentity identity)
    {
        return IsMe(identity.GetSteamID64());
    }

    /// <summary>
    /// Returns true if the provided SteamWorks.Net Identity corresponds to one of our Steam friends, false otherwise.
    /// </summary>
    /// <param name="identity"></param>
    /// <returns></returns>
    public static bool IsFriend(SteamNetworkingIdentity identity)
    {
        return SteamFriends.GetFriendRelationship(identity.GetSteamID()) == EFriendRelationship.k_EFriendRelationshipFriend;
    }

    /// <summary>
    /// Returns true if the provided SteamID corresponds to one of our Steam friends, false otherwise.
    /// </summary>
    /// <param name="identity"></param>
    /// <returns></returns>
    public static bool IsFriend(ulong steamID)
    {
        SteamNetworkingIdentity identity = new();
        identity.SetSteamID64(steamID);
        identity.SetSteamID(new CSteamID(steamID));
        return IsFriend(identity);
    }

    /// <summary>
    /// Helper function that constructs a new SteamNetworkingIdentity object from the given SteamID.
    /// </summary>
    /// <param name="steamID">A valid steamID that corresponds to an active Steam Account</param>
    /// <returns>A SteamNetworkingIdentity object that corresponds to the Steam Account associated with the given SteamID</returns>
    public static SteamNetworkingIdentity SteamIDToIdentity(ulong steamID)
    {
        SteamNetworkingIdentity id = new();
        id.SetSteamID64(steamID);
        return id;
    }

    public static GameMessage ConstructFullMessage(IMessage payload, MessageType type, bool broadcast, bool toServer, ulong targetUser)
    {
        GameMessage msg = new GameMessage();
        msg.Sender = Global.steamid;
        msg.SendTime = Utils.GetTime();
        msg.SendTick = Global.world.GetTick();
        msg.Broadcast = broadcast;
        msg.ToServer = toServer;
        msg.TargetUser = targetUser;
        msg.Type = type;
        msg.Payload = Any.Pack(payload);

        msg.Payload.Unpack(NetworkUtils.TypeRegistry);

        return msg;
    }

    internal static SteamNetworkingIdentity SteamIDStringToIdentity(string m_rgchConnect)
    {
        return SteamIDToIdentity(ulong.Parse(m_rgchConnect));
    }
}

