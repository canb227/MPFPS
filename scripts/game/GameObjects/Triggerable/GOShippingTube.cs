using Godot;


[GlobalClass]
public partial class GOShippingTube : GOTrap
{
    [Export] Area3D ItemsForShipping;

    public override void _Ready()
    {
        base._Ready();
        animationPlayer.Play("shippingFailed");
    }
}