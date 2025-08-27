//Singleton that autoloads right after Godot engine init. Can be statically referenced using "Utils." anywhere
//This class is for storing universally useful static utility functions
using Godot;
using Steamworks;
using System;

public static class Utils
{
    internal static ulong GetTime()
    {
        return (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    internal static Texture2D GetMediumSteamAvatar(ulong id)
    {
        int avatarHandle = SteamFriends.GetMediumFriendAvatar(new CSteamID(id));
        SteamUtils.GetImageSize(avatarHandle, out uint width, out uint height);
        int size = (int)(width * height * 4); // RGBA format
        byte[] avatarData = new byte[size];
        SteamUtils.GetImageRGBA(avatarHandle, avatarData, size);
        Image testImage = Image.CreateFromData((int)width, (int)height, false, Image.Format.Rgba8, avatarData);
        ImageTexture texture = ImageTexture.CreateFromImage(testImage);
        return texture;
    }

    internal static bool IsFirstLaunch()
    {
        if (DirAccess.DirExistsAbsolute("user://saves"))
        {
            if (DirAccess.DirExistsAbsolute("user://saves/" + Global.steamid.ToString()))
            {
                Logging.Log("Local: NOT First Launch, User: NOT First Launch", "FirstTimeSetup");
                return false;
            }
            else
            {
                Logging.Log("Local: NOT First Launch, User: FIRST LAUNCH", "FirstTimeSetup");
                return false;
            }
        }
        else
        {
            Logging.Log("Local: FIRST LAUNCH, User: PROBABLY FIRST LAUNCH", "FirstTimeSetup");
            return true;
        }
    }
}
