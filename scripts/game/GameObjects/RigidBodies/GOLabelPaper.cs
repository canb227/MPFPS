using Godot;
using MessagePack;

[GlobalClass]
public partial class GOLabelPaper : SimpleShape
{
    [Export]
    public SubViewport viewport { get; set; }
    private Label viewportLabel { get; set; }

    public override void _Ready()
    {
        base._Ready();
        viewportLabel = viewport.GetNode<Label>("Label");
        viewportLabel.Text = "123 NeedAddress FromConstructor";
    }
    
}