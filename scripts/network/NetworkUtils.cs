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

    //  Steam Networking takes a bitflag string to configure message settings. Look them up in the steamworks API
    //  These options effectively switch between UDP and TCP-like behavior, and have signifigant implications on the functionality and performance of networking. Handle with care.
    public const int k_nSteamNetworkingSend_NoNagle = 1;
    public const int k_nSteamNetworkingSend_NoDelay = 4;
    public const int k_nSteamNetworkingSend_Unreliable = 0;
    public const int k_nSteamNetworkingSend_Reliable = 8;
    public const int k_nSteamNetworkingSend_UnreliableNoNagle = k_nSteamNetworkingSend_Unreliable | k_nSteamNetworkingSend_NoNagle;
    public const int k_nSteamNetworkingSend_UnreliableNoDelay = k_nSteamNetworkingSend_Unreliable | k_nSteamNetworkingSend_NoDelay | k_nSteamNetworkingSend_NoNagle;
    public const int k_nSteamNetworkingSend_ReliableNoNagle = k_nSteamNetworkingSend_Reliable | k_nSteamNetworkingSend_NoNagle;

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


    internal static SteamNetworkingIdentity SteamIDStringToIdentity(string m_rgchConnect)
    {
        return SteamIDToIdentity(ulong.Parse(m_rgchConnect));
    }
}

