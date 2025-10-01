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
    public ButtonCooldownSetting ButtonCooldownSetting { get; set; }
}

public enum ButtonCooldownSetting
{
    DisableOnlyIfSelfOnCooldown,
    DisableIfSelfOrAnyTriggersOnCooldown,
    DisableIfSelfOrAllTriggersOnCooldown,
}