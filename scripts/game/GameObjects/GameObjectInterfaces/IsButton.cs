using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public interface IsButton : IsInteractable
{

    [Export]
    public Godot.Collections.Array<Triggers> triggers { get; set; }

    [Export]
    public ButtonDisableCondition ButtonDisableCondition { get; set; }
}

public enum ButtonDisableCondition
{
    DisableIfAnyTriggersOnCooldown,
    DisableIfAllTriggersOnCooldown,
}