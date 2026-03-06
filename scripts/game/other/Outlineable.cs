using Godot;

public partial class Outlineable : Node3D
{
    [Export] public MeshInstance3D _outline;

    public override void _Ready()
    {
        _outline.Visible = false;
    }

    public void SetHighlighted(bool enabled)
    {
        _outline.Visible = enabled;
    }
}
