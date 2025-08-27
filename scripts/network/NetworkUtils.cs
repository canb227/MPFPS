using Steamworks;
using System.Linq;
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

    //The following six functions process pretty much every single bit of information going across the network.
    //The latter four process much of the non-networked stuff too.
    //TODO: Make sure these don't suck
    //TODO: Switch to unsafe code blocks for performance gain - ONLY IF NEEDED: EXPOSES DIRECT MEMORY ACCESS VULNERABILITY

    /// <summary>
    /// Unpacks a Steam Message payload into the one byte type enum and the actual data. This function completes even if the message is malformed, so be careful.
    /// </summary>
    /// <param name="payload"></param>
    /// <param name="data"></param>
    /// <param name="type"></param>
    public static void UnwrapSteamPayload(byte[] payload, out byte[] data, out NetType type)
    {
        data = new byte[payload.Length];
        type = (NetType)payload[0];
        data = payload.Skip(1).ToArray();
    }

    /// <summary>
    /// Packs a byte array and a type into the correct format for a Steam Message payload.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public static byte[] WrapSteamPayload(byte[] data, NetType type)
    {
        byte[] payload = new byte[data.Length + 1];
        payload[0] = (byte)type;
        data.CopyTo(payload, 1);
        return payload;
    }

    /// <summary>
    /// Given a pointer to unmanaged memory and a length (in bytes), COPIES that many bytes from the pointer into a new byte array.
    /// </summary>
    /// <param name="ptr"></param>
    /// <param name="length"></param>
    /// <returns>A byte array holding a copy of the first (length) number of bytes in the unmanaged memory location indicated by the provided pointer.</returns>
    public static byte[] PtrToBytes(nint ptr, int length)
    {
        byte[] data = new byte[length];
        Marshal.Copy(ptr, data, 0, length);
        return data;
    }

    /// <summary>
    /// Given a byte array, generates a new pointer to unmanaged memory, allocates memory equal to the length of the array, and COPIES the bytes from the array into the location pointed by the pointer 
    /// </summary>
    /// <param name="data"></param>
    /// <returns>A pointer to newly allocated unmanaged memory holding a copy of the bytes in the given array.</returns>
    public static nint BytesToPtr(byte[] data)
    {
        nint ptr = Marshal.AllocHGlobal(data.Length);
        Marshal.Copy(data, 0, ptr, data.Length);
        return ptr;
    }

    /// <summary>
    /// Transforms a given struct of type T into a newly allocated byte array. NOTE: The struct must have a compile time constant size in memory (no reference types!)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="structure"></param>
    /// <returns></returns>
    public static byte[] StructToBytes<T>(T structure)
    {
        byte[] data = new byte[Marshal.SizeOf<PlayerOptions>()];
        nint ptr = Marshal.AllocHGlobal(Marshal.SizeOf<PlayerOptions>());
        Marshal.StructureToPtr<T>(structure, ptr, true);
        Marshal.Copy(ptr, data, 0, Marshal.SizeOf<PlayerOptions>());
        return data;
    }

    /// <summary>
    /// Transforms a given byte array into a newly allocated struct of type T. Should only ever be called on byte arrays created by <see cref="StructToBytes{T}(T)"/>. NOTE: The struct must have a compile time constant size in memory (no reference types!)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <returns></returns>
    public static T BytesToStruct<T>(byte[] data)
    {
        nint ptr = Marshal.AllocHGlobal(data.Length);
        Marshal.Copy(data, 0, ptr, data.Length);
        return Marshal.PtrToStructure<T>(ptr);
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

    /// <summary>
    /// Helper function to turn a string containing only numbers into a SteamNetworkingIdentity with the SteamID parsed from the string.
    /// </summary>
    /// <param name="m_rgchConnect"></param>
    /// <returns></returns>
    public static SteamNetworkingIdentity SteamIDStringToIdentity(string m_rgchConnect)
    {
        return SteamIDToIdentity(ulong.Parse(m_rgchConnect));
    }


}

