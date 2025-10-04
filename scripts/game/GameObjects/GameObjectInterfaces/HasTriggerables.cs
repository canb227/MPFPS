using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public interface HasTriggerables
{

    [Export]
    public Godot.Collections.Array<Trigger> triggerables { get; set; }

    public bool UserCanTrigger(string triggerName, ulong byID);
    public float GetTriggerCooldown(string triggerName, ulong byID);
    public bool IsTriggerReady(string triggerName);
    public void Trigger(string triggerName, ulong byID, ulong onTick);

    public Trigger GetTrigger(string triggerName);

}


