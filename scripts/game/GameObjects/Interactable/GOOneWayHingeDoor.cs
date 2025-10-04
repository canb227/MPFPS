using Godot;
using Godot.Collections;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


[GlobalClass]
public partial class GOOneWayHingeDoor : GOBaseStaticInteractable
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

    [RPCMethod(RPCMode.OnlySendToAuth)]
    public override void Auth_HandleInteractionRequest(ulong byID, ulong onTick)
    {
        if (interruptable || (!opening && !closing))
        {
            if (IsOpen() || opening)
            {
                RPCManager.RPC(this, MethodName.Close, [byID, onTick]);
            }
            else if (IsClosed() || closing)
            {
                RPCManager.RPC(this, MethodName.Open, [byID, onTick]);
            }
        }
    }

    [RPCMethod(RPCMode.SendToAllPeers)]
    public void Open(ulong byID, ulong onTick)
    {
        closing = false;
        opening = true;
    }

    [RPCMethod(RPCMode.SendToAllPeers)]
    public void Close(ulong byID, ulong onTick)
    {
        closing = true;
        opening = false;
    }

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

    public override string GenerateStateString()
    {
        return $"opening:{opening}|closing:{closing}|isopen:{IsOpen()}|isCLosed:{IsClosed()}|ready:{interactCooldownReady}|cooldown:{interactCooldownTimer}|speed:{degreesPerSecond}";
    }

    public override byte[] GenerateStateUpdate()
    {
        return new byte[0];
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
