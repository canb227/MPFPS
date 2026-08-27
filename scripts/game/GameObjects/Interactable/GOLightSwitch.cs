using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[GlobalClass]
public partial class GOLightSwitch: GOButton
{
    [Export]
    public AnimationPlayer animationPlayer { get; set; }
    public bool isOn = true;


    public override void _Ready()
    {
        base._Ready();
        if (animationPlayer == null)
        {
            Logging.Error($"Button {Name} ({id}) could not load its Animation Player! Check object properties.", "GOLightSwitch");
        }
    }

    private bool lightOnStorage = true;
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        if(Global.gameState.gameModeManager.lightsOn != lightOnStorage)
        {
            if(Global.gameState.gameModeManager.lightsOn)
            {
                animationPlayer.Play("switch_on");
            }
            else
            {
                animationPlayer.Play("switch_off");
            }
        }

        lightOnStorage = Global.gameState.gameModeManager.lightsOn;
    }

    public override void PressedFailed(ulong byID, ulong onTick)
    {
        base.PressedFailed(byID, onTick);
    }

    public override void PressedSuccessfully(ulong byID, ulong onTick)
    {
        //this is RPC'd to everybody when we press successful so we dont need to RPC the on/off (though maybe thats begging for desync'd light states, will test)
        base.PressedSuccessfully(byID, onTick);
        if(Global.gameState.gameModeManager.generator.GetGeneratorPowered())
        {
            if(Global.gameState.gameModeManager.lightsOn)
            {
                Global.gameState.gameModeManager.TurnOffAllSpotLights();
            }
            else
            {
                Global.gameState.gameModeManager.TurnOnAllSpotLights();
            }
        }
        
    }

    public override void PressedWhileDisabled(ulong byID, ulong onTick)
    {
        base.PressedWhileDisabled(byID, onTick);
    }

    public override void OnEnable(ulong onTick)
    {
        base.OnEnable(onTick);
    }

    public override void OnDisable(ulong onTick)
    {
        base.OnDisable(onTick);
    }

    public override string GenerateStateString()
    {
        return base.GenerateStateString() + $"|currentAnimation:{isOn}";
    }

}

