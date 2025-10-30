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
    [Export] Node3D lockCameraPosition;
    [Export] Node3D lockPlayerPosition;
    [Export] AnimationPlayer animationPlayer;

    [Export] DeliveryVehicle2D vehicle2D;
    [Export] Area2D finishArea;

    public bool locked = false;

    private ActionFlags lastTickActions { get; set; }
    private PlayerInputData input;
    public bool activeDelivery;
    private ulong activeCharacterID;
    private ulong activeSteamID;

    private Transform3D playerCameraBackUp { get; set; }
    private Transform3D playerPositionBackUp { get; set; }


    public int orderID = -1;

    public override void _Ready()
    {
        base._Ready();
        finishArea.BodyEntered += OnBodyEntered;
        GameModeManager.OnDeliveryQueueAppended += NewDelivery;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationPredelete)
        {
            GameModeManager.OnDeliveryQueueAppended -= NewDelivery;
        }
    }


    public override void Auth_HandleInteractionRequest(ulong byCharacterID, ulong onTick)
    {
        //get player controller by ID
        if (!locked && orderID != -1)
        {
            RPCManager.RPC(this, "LockPlayer", [byCharacterID, lockCameraPosition.GlobalTransform, lockPlayerPosition.Transform]);
        }
    }

    public void NewDelivery()
    {
        if (!activeDelivery && !locked && Global.gameState.gameModeManager.deliveryQueue.Any())
        {
            orderID = Global.gameState.gameModeManager.deliveryQueue.Dequeue();
            activeDelivery = true;
            PrepareMiniGameForNewDelivery();
        }
    }
    
    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void PlayAnimation(string animationName)
    {
        animationPlayer.Play(animationName);
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void LockPlayer(ulong characterID, Transform3D cameraPosition, Transform3D playerPosition)
    {
        Logging.Log("Locking Player, and passing input to the machine", "GODeliveryGameMonitor");
        GOBasePlayerCharacter playerCharacter = (GOBasePlayerCharacter)Global.gameState.GameObjects[characterID];
        if (playerCharacter is BasicPlayerCharacter basicPlayerCharacter)
        {
            basicPlayerCharacter.KnockedOut += UnlockPlayer;
            basicPlayerCharacter.Killed += UnlockPlayer;
        }
        playerCharacter.LockControl();
        playerCameraBackUp = playerCharacter.camera.GlobalTransform;
        playerCharacter.camera.GlobalTransform = cameraPosition;
        locked = true;
        activeCharacterID = characterID;
        activeSteamID = playerCharacter.authority;
        input = Global.gameState.PlayerInputs[playerCharacter.authority];
        
        rpc_MiniGameStart();
    }
    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void UnlockPlayer(ulong characterID)
    {
        if (locked)
        {
            GOBasePlayerCharacter playerCharacter = (GOBasePlayerCharacter)Global.gameState.GameObjects[characterID];
            if (playerCharacter is BasicPlayerCharacter basicPlayerCharacter)
            {
                basicPlayerCharacter.KnockedOut -= UnlockPlayer;
                basicPlayerCharacter.Killed -= UnlockPlayer;
            }
            playerCharacter.UnlockControl();
            playerCharacter.camera.GlobalTransform = playerCameraBackUp;
            locked = false;
            input = new();
            PlayAnimation("gameReady");
        }
    }

    public void PrepareMiniGameForNewDelivery()
    {
        PlayAnimation("gameReady");
    }

    public void rpc_MiniGameStart()
    {
        PlayAnimation("gameStart");
        Transform2D vehicleTransform = Transform2D.Identity;
        vehicleTransform.Origin = new(500, 500);
        vehicle2D.Transform = vehicleTransform;
    }

    [RPCMethod(mode = RPCMode.SendToAllPeers)]
    public void MiniGameWon()
    {
        UnlockPlayer(activeCharacterID);
        PlayAnimation("gameWon");
        Global.gameState.gameModeManager.OrderFinished(orderID);
        orderID = -1;
        activeDelivery = false;
        MiniGameResetDelayer(delaySeconds: 4f);
    }

    public async void MiniGameResetDelayer(float delaySeconds = 4f)
    {
        await ToSignal(GetTree().CreateTimer(delaySeconds), SceneTreeTimer.SignalName.Timeout);
        PlayAnimation("gameWaiting");
        NewDelivery();
    }

    private void OnBodyEntered(Node body)
    {
        if (Global.Lobby.bIsLobbyHost)
        {
            if (body is DeliveryVehicle2D)
            {
                RPCManager.RPC(this, "MiniGameWon", []);
            }
        }
        else
        {
            if (body is DeliveryVehicle2D)
            {
                Logging.Log("We are a client and won the delivery game, hopefully the host agrees", "GODeliveryGameMonitor");
            }
        }
    }

    public override void PerFrameAuth(double delta)
    {

    }

    public override void PerFrameLocal(double delta)
    {

    }

    public override void PerTickShared(double delta)
    {

    }


    public override void PerFrameShared(double delta)
    {
        if (locked)
        {
            GD.Print("Machine Locked and taking input:" + Global.gameState.PlayerInputs[activeSteamID].playerID + " " + Global.gameState.PlayerInputs[activeSteamID].actions);
            vehicle2D.PerFrameShared(Global.gameState.PlayerInputs[activeSteamID], delta);
        }
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

