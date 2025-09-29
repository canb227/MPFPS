using Godot;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[GlobalClass]
public partial class BaseTriggerable : GOBaseStaticBody, IsTriggerable
{
    public ulong lastTriggeredOnTick { get; set; }
    public ulong duration { get; set; }

    public bool isActive { get; set; }

    [Export]
    public AnimationPlayer animationPlayer { get; set; }

    [Export]
    public Godot.Collections.Array<string> triggerNames { get; set; }

    [Export]
    public ulong cooldown { get; set; }

    public override string GenerateStateString()
    {
        return "";
    }

    public override byte[] GenerateStateUpdate()
    {
        return new byte[1];
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

    public void Trigger(string triggerName, ulong byID)
    {
        if (triggerNames.Contains(triggerName))
        {
            byte[] data = MessagePackSerializer.Serialize(new TriggerRPCPacket(triggerName,byID));
            RPCManager.SendRPC(this.GetPath(), "rpc_Trigger", data);
        }
    }

    public void rpc_Trigger(byte[] data)
    {
        TriggerRPCPacket packet = MessagePackSerializer.Deserialize<TriggerRPCPacket>(data);
        _Trigger(packet.triggerName, packet.byID);
    }

    private void _Trigger(string triggerName, ulong byID)
    {
        animationPlayer.Play(triggerName);
    }
}

[MessagePackObject]
public struct TriggerRPCPacket
{
    [Key(0)]
    public ulong byID;

    [Key(1)]
    public string triggerName;

    public TriggerRPCPacket(string triggerName, ulong byID)
    { 
        this.triggerName = triggerName;
        this.byID = byID;
    }
}