using System.Collections.Generic;
using Godot;

public partial class Fog : Node
{
    [Export] public FogVolume[] fogVolumes;
    public Fog()
    {
        
    }

    public override void _Ready()
    {
        base._Ready();
        GameModeManager.EvacuationStarted += EvacuationStarted;
        foreach(var fog in fogVolumes)
        {
            fog.Visible = true;
        }
    }

    public void EvacuationStarted()
    {
        SetFogVisibility(false);
    }

    public void SetFogVisibility(bool visible)
    {
        foreach(var fog in fogVolumes)
        {
            fog.Visible = visible;
        }
    }

}