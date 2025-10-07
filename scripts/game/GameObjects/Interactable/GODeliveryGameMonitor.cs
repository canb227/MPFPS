using Godot;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

[GlobalClass]
public partial class GODeliveryGameMonitor : GOBaseStaticInteractable
{
    [Export]
    Node3D lockCameraPosition;
    [Export]
    Node3D lockPlayerPosition;

    [Export]
    DeliveryVehicle2D vehicle2D;

    public bool locked = false;

    private ActionFlags lastTickActions { get; set; }
    private PlayerInputData input;
    private ulong activePlayerID;

    public override void Auth_HandleInteractionRequest(ulong byID, ulong onTick)
    {
        //get player controller by ID
        if (!locked)
        {
            activePlayerID = byID;
            LockPlayer(byID, lockCameraPosition.Transform, lockPlayerPosition.Transform);
        }
    }

    private void LockPlayer(ulong playerID, Transform3D cameraPosition, Transform3D playerPosition)
    {
        //assign input and lock player from acting and set camera/player position
        locked = true;
        input = new();
    }

    private void UnlockPlayer(ulong playerID)
    {
        //unassign input
        //reset camera and player position and unlock input
        locked = false;
        input = new();
    }

    public void MiniGameWon()
    {
        UnlockPlayer(activePlayerID);
        //minigame win stuff
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
        if (locked)
        {
            vehicle2D.PerTick(input, delta);
        }
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

