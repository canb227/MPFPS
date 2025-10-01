using Godot;
using Godot.Collections;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


[GlobalClass]
public partial class GOOneWayHingeDoor : GOBaseStaticBody, IsInteractable
{

    [Export]
    public ulong interactCooldownSeconds { get; set; }

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
    public bool ready { get; set; }
    public ulong lastInteractTick { get; set; }
    public ulong lastInteractPlayer { get; set; }
    private float cooldownTimer { get; set; }

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

    public bool CanInteract(ulong byID)
    {
        if (interruptable && ready)
        {
            return true;
        }
        else if (!interruptable && ready && !opening && !closing)
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
        throw new NotImplementedException();
    }

    public override byte[] GenerateStateUpdate()
    {
        throw new NotImplementedException();
    }

    public void OnInteract(ulong byID)
    {
        GODoorRPC packet = new();
        packet.byID = byID;
        byte[] data = MessagePackSerializer.Serialize(packet);
        RPCManager.SendRPC(this, "rpc_OnInteract", data);
    }

    public void rpc_OnInteract(byte[] data)
    {
        GOButtonRPC packet = MessagePackSerializer.Deserialize<GOButtonRPC>(data);
        Logging.Log($"Interaction request received from network: Object {id} interacted with by {packet.byID}", "GOInteractable");
        _OnInteract(packet.byID);
    }

    private void _OnInteract(ulong byID)
    {
        if (CanInteract(byID))
        {
            if (IsOpen() || opening)
            {
                closing = true;
                opening = false;
                ready = false;
                cooldownTimer = interactCooldownSeconds;
            }
            else if (IsClosed() || closing)
            {
                closing = false;
                opening = true;
                ready = false;
                cooldownTimer = interactCooldownSeconds;
            }
        }
    }


    public override void PerFrameAuth(double delta)
    {
        PerTickLocal(delta);

    }

    public override void PerFrameLocal(double delta)
    {
        throw new NotImplementedException();
    }

    public override void PerTickAuth(double delta)
    {
        throw new NotImplementedException();
    }

    public override void PerTickLocal(double delta)
    {
        if (!ready)
        {

        }
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
        }
    }

    public override void ProcessStateUpdate(byte[] update)
    {
        throw new NotImplementedException();
    }
}

[MessagePackObject]
public struct GODoorRPC
{
    [Key(0)]
    public ulong byID;
}