using Godot;
using MessagePack;
using System;

[GlobalClass]
public partial class Crusher : GOBaseStaticBody,ITriggerable
{
    public ulong lastTriggerTick { get; set; }
    public ulong duration { get; set; }
    public ulong cooldown { get; set; }
    public bool isActive { get; set; }
    public AnimationPlayer animationPlayer { get; set; }

    private ulong cooldownTimer {get; set;}
    public override string GenerateStateString()
    {
        return MessagePackSerializer.ConvertToJson(GenerateStateUpdate());
    }

    public override byte[] GenerateStateUpdate()
    {
        CrusherStatePacket update = new();
        update.cooldownTimer = cooldownTimer;
        return MessagePackSerializer.Serialize(update);
    }

    public override void ProcessStateUpdate(byte[] _update)
    {
        CrusherStatePacket update = MessagePackSerializer.Deserialize<CrusherStatePacket>(_update);

        if (cooldownTimer == 0 && update.cooldownTimer != 0)
        {
            OnTrigger();
        }
        cooldownTimer = update.cooldownTimer;
    }

    public void OnTrigger()
    {
        if (cooldownTimer == 0)
        {
            dirty = true;
            cooldownTimer = cooldown;
            if (animationPlayer != null)
            {
                animationPlayer.Play();
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
        if (cooldownTimer > 0)
        {
            cooldownTimer--;
        }
    }

    public override void PerTickLocal(double delta)
    {

    }

}

[MessagePackObject]
public struct CrusherStatePacket
{
    [Key(0)]
    public ulong cooldownTimer;
}