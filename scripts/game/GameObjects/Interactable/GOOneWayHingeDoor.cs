using Godot;
using Godot.Collections;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


[GlobalClass]
public partial class GOOneWayHingeDoor : GODoor
{

    [Export]
    public bool interruptable { get; set; }

    [Export]
    float degreesPerSecond { get; set; }

    [Export]
    public float doorOpen_MaxDegrees { get; set; }

    [Export]
    public float doorClosed_MinDegrees { get; set; }

    public bool opening { get; set; }
    public bool closing { get; set; }

    public bool IsOpen()
    {
        if (RotationDegrees.Y == doorOpen_MaxDegrees) return true;
        return false;
    }

    public bool IsClosed()
    {
        if (RotationDegrees.Y == doorClosed_MinDegrees) return true;
        return false;
    }

    public override bool CanInteract(ulong byID)
    {
        if (interruptable && internalCooldownReady)
        {
            return true;
        }
        else if (!interruptable && internalCooldownReady && !opening && !closing)
        {
            return true;
        }
        else
        {
            return false; 
        }
    }

    public override string GenerateStateString()
    {
        return $"opening:{opening}|closing:{closing}|ready:{internalCooldownReady}|cooldown:{internalCooldownTimer}";
    }

    public override byte[] GenerateStateUpdate()
    {
        return new byte[0];
    }

    public override void ActivateDoor(ulong byID)
    {
        if (CanInteract(byID))
        {
            if (IsOpen() || opening)
            {
                closing = true;
                opening = false;
            }
            else if (IsClosed() || closing)
            {
                closing = false;
                opening = true;
            }
        }
    }


    public override void PerFrameAuth(double delta)
    {

    }

    public override void PerFrameLocal(double delta)
    {

    }

    public override void PerTickAuth(double delta)
    {

    }

    public override void PerTickLocal(double delta)
    {

    }

    public override void ProcessStateUpdate(byte[] update)
    {

    }

    public override void PerFrameShared(double delta)
    {

    }

    public override void PerTickShared(double delta)
    {
        base.PerTickShared(delta);
        if (opening)
        {
            float rot = RotationDegrees.Y;
            rot += degreesPerSecond;
            if (rot >= doorOpen_MaxDegrees)
            {
                rot = doorOpen_MaxDegrees;
                opening = false;
                closing = false;
            }
            RotationDegrees = new Vector3(RotationDegrees.X, rot, RotationDegrees.Z);
        }
        else if (closing)
        {
            float rot = RotationDegrees.Y;
            rot -= degreesPerSecond;
            if (rot <= doorClosed_MinDegrees)
            {
                rot = doorClosed_MinDegrees;
                opening = false;
                closing = false;
            }
            RotationDegrees = new Vector3(RotationDegrees.X, rot, RotationDegrees.Z);
        }
    }
}

[MessagePackObject]
public struct GODoorRPC
{
    [Key(0)]
    public ulong byID;
}