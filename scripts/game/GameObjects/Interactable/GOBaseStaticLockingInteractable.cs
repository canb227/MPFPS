using Godot;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;


public partial class GOBaseStaticLockingInteractable : GOBaseStaticInteractable
{
    [Export]
    Node3D lockCameraPosition;
    [Export]
    Node3D lockPlayerPosition;

    public override void Auth_HandleInteractionRequest(ulong byID, ulong onTick)
    {
        //get player controller by ID
        LockPlayer(byID, lockCameraPosition.Transform, lockPlayerPosition.Transform);
    }

    private void LockPlayer(ulong playerID, Transform3D cameraPosition, Transform3D playerPosition)
    {

    }

    private void UnlockPlayer(ulong playerID)
    {

    }

    public override void PerFrameAuth(double delta)
    {

    }

    public override void PerFrameLocal(double delta)
    {

    }

    public override void PerFrameShared(double delta)
    {

    }

    public override void PerTickAuth(double delta)
    {
        
    }

    public override void PerTickLocal(double delta)
    {

    }
    public override byte[] GenerateStateUpdate()
    {
        return new byte[0];
    }

    public override void ProcessStateUpdate(byte[] _update)
    {

    }
    public override string GenerateStateString()
    {
        return $"interactCooldown: {interactCooldownTimer.ToString("0.00")}s / {interactCooldownSeconds.ToString("0.00")}s | ready?{interactCooldownReady}";
    }
}

