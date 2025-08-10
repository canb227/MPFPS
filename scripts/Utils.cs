//Singleton that autoloads right after Godot engine init. Can be statically referenced using "Utils." anywhere
//This class is for storing universally useful static utility functions
using System;

public static class Utils
{
    internal static ulong GetTime()
    {
        return (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); 
    }
}
