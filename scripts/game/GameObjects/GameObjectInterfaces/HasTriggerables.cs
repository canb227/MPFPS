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

    public bool CanTrigger(string triggerName, ulong byID);
    public float GetTriggerCooldown(string triggerName, ulong byID);
    public void Trigger(string triggerName, ulong byID);

    public Trigger GetTrigger(string triggerName);

}


