using Godot;
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
            animationPlayer.Play(triggerName);
        }
    }
}

