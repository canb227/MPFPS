using Godot;
using Godot.Collections;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

[GlobalClass]
public partial class GameButton : GOBaseStaticBody, IsInteractable
{

    [Export] 
    public AnimationPlayer animationPlayer;

    [Export]
    public string onPressAnimationName;

    [Export]
    public ulong cooldown { get; set; }

    [Export]
    public Godot.Collections.Array<Trigger> triggers { get; set; }

    public ulong lastInteractTick { get; set; }
    public ulong lastInteractPlayer { get; set; }

    private ulong cooldownTimer = 0;

    public override GameObjectType type { get; set; } = GameObjectType.GameButton;



    public override void _Ready()
    {

    }

    public void OnInteract(ulong byID)
    {
        if (cooldownTimer==0)
        {
            GameButtonRPCPacket packet = new();
            packet.byID = byID;
            byte[] data = MessagePackSerializer.Serialize(packet);
            RPCManager.SendRPC(this.GetPath(), "rpc_OnInteract", data);
        }
    }

    public void rpc_OnInteract(byte[] data)
    {
        GameButtonRPCPacket packet = MessagePackSerializer.Deserialize<GameButtonRPCPacket>(data);
        _OnInteract(packet.byID);
    }

    private void _OnInteract(ulong byID)
    {
        dirty = true;
        cooldownTimer = cooldown;
        if (animationPlayer != null)
        {
            animationPlayer.Play(onPressAnimationName);
        }
        foreach (var trigger in triggers)
        {
            (GetNode(trigger.triggerableNode) as IsTriggerable).Trigger(trigger.triggerName, byID);
        }
    }

    public override byte[] GenerateStateUpdate()
    {
        GameButtonStatePacket update = new();
        update.cooldownTimer = cooldownTimer;
        return MessagePackSerializer.Serialize(update);
    }

    public override void ProcessStateUpdate(byte[] _update)
    {
        GameButtonStatePacket update = MessagePackSerializer.Deserialize<GameButtonStatePacket>(_update);

        if(cooldownTimer==0 && update.cooldownTimer!=0)
        {
            OnInteract(id);
        }
        cooldownTimer = update.cooldownTimer;
    }

    public override void PerTickAuth(double delta)
    {
        if (cooldownTimer>0)
        {
            cooldownTimer--;
        }
    }

    public override void PerFrameAuth(double delta)
    {

    }

    public override void PerTickLocal(double delta)
    {

    }

    public override void PerFrameLocal(double delta)
    {

    }

    public override string GenerateStateString()
    {
        return MessagePackSerializer.ConvertToJson(GenerateStateUpdate());
    }

}

[MessagePackObject]
public struct GameButtonStatePacket
{
    [Key(0)]
    public ulong cooldownTimer;


}

[MessagePackObject]
public struct GameButtonRPCPacket
{
    [Key(0)]
    public ulong byID;
}