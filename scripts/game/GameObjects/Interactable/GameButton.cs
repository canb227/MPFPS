using Godot;
using Godot.Collections;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;

[GlobalClass]
public partial class GameButton : GOBaseStaticBody, IInteractable
{

    [Export] 
    public AnimationPlayer animationPlayer;

    [Export]
    public Godot.Collections.Array<Node> triggers { get; set; }

    [Export]
    public ulong cooldown { get; set; }

    public ulong lastInteractTick { get; set; }
    public ulong lastInteractPlayer { get; set; }

    private ulong cooldownTimer = 0; 

    public override void _Ready()
    {
        foreach (var trigger in triggers)
        {
            if (trigger is not ITriggerable)
            {
                Logging.Error($"This Interactable Node: {Name} has non Triggerable nodes in its triggers list!", "GameButton", true, true);
            }
        }
    }

    public void OnInteract()
    {
        if (cooldownTimer==0)
        {
            dirty = true;
            cooldownTimer = cooldown;
            if (animationPlayer != null)
            {
                animationPlayer.Play("button_press");
            }

            foreach (var trigger in triggers)
            {
                if (trigger is ITriggerable t)
                {
                    t.OnTrigger();
                }

            }
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
            OnInteract();
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