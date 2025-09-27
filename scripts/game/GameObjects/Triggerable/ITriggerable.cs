using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public interface ITriggerable
{
    public ulong lastTriggerTick { get; set; }
    public ulong duration { get; set; }
    public ulong cooldown { get; set; }
    public bool isActive { get; set; }

    public AnimationPlayer animationPlayer {  get; set; }
    public void OnTrigger();
}


