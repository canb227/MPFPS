using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public static class PCUtils
{

    /// <summary>
    /// Returns the local velocity vector of a player character
    /// </summary>
    /// <param name="pc"></param>
    /// <returns></returns>
    public static Vector3 GetLocalVelocity(GOBasePlayerCharacter pc)
    {
        return LocalizeVector(pc, pc.Velocity);
    }

    /// <summary>
    /// Takes a global space vector and rotates it to be aligned with the current rotation of the given player character.
    /// </summary>
    /// <param name="pc"></param>
    /// <param name="globalVector"></param>
    /// <returns></returns>
    public static Vector3 LocalizeVector(GOBasePlayerCharacter pc, Vector3 globalVector)
    {
        return pc.Transform.Basis.Inverse() * globalVector;
    }

    /// <summary>
    /// Takes a vector aligned with the current rotation of the given player character vector and rotates it to be aligned with the global basis
    /// </summary>
    /// <param name="pc"></param>
    /// <param name="localVector"></param>
    /// <returns></returns>
    public static Vector3 GlobalizeVector(GOBasePlayerCharacter pc, Vector3 localVector)
    {
        return pc.Transform.Basis * localVector;
    }

    public static Vector3 InFrontOf(GOBaseCharacterBody3D pc, float distance)
    {
        var playerForwardVector = -pc.GlobalTransform.Basis.Z.Normalized();
        var position = pc.GlobalPosition + (playerForwardVector * distance);
        return position;
    }
}

