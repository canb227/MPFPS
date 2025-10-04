using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[GlobalClass]
public partial class Trigger : Resource
{

    [Export]
    public string triggerName;

    [Export]
    public float cooldownSeconds;

    public float cooldownSecondsRemaining;
    public ulong lastTriggeredOnTick { get; set; }
    public ulong lastTriggeredByID { get; set; }
    public bool isActive;

    public Trigger() { }

    public Trigger(string triggerName,float cooldownSeconds)
    {
        this.triggerName = triggerName;
        this.cooldownSeconds = cooldownSeconds;
    }

    public override string ToString()
    {
        return $"Trigger Name: {triggerName} | cooldown: {cooldownSecondsRemaining}s / {cooldownSeconds}s ";
    }
}

