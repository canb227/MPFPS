using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[GlobalClass]
public partial class Triggers : Resource
{
    [Export]
    public NodePath triggerableNode
    {
        get { return _triggerableNode; }
        set { 
            
            _triggerableNode = value; 
        }
    }
    private NodePath _triggerableNode;


    [Export]
    public string triggerName;

    //public void Trigger(ulong byID)
    //{
    //    if (triggerableNode is HasTriggerables t)
    //    {
    //        if (CanTrigger(byID))
    //        {
    //            t.Trigger(triggerName, byID);
    //        }
    //        else
    //        {
    //            Logging.Log($"Trigger {triggerName} is still on cooldown.", "Triggers");
    //        }
    //    }
    //    else
    //    {
    //        Logging.Error($"Nodepath points to invalid (non-IsTriggerable) node!", "Triggers");
    //    }
    //}
    //public float GetTriggerCooldown()
    //{
    //    if (Global.instance.GetNode(triggerableNode) is HasTriggerables t)
    //    {
    //        return t.GetTriggerCooldown(triggerName);
    //    }
    //    else
    //    {
    //        Logging.Error($"Nodepath points to invalid (non-IsTriggerable) node!", "Triggers");
    //        return 0;
    //    }
    //}

    //public bool CanTrigger(ulong byID)
    //{
    //    if (Global.instance.GetNode(triggerableNode) is HasTriggerables t)
    //    {
    //        return t.CanTrigger(triggerName);
    //    }
    //    else
    //    {
    //        Logging.Error($"Nodepath points to invalid (non-IsTriggerable) node!", "Triggers");
    //        return false;
    //    }
    //}

}

