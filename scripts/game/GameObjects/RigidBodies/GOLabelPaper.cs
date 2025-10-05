using Godot;
using MessagePack;

[GlobalClass]
public partial class GOLabelPaper : SimpleShape
{
    [Export]
    public SubViewport viewport { get; set; }
    private Label viewportLabel { get; set; }

    private string text = "123 NeedAddress FromConstructor";

    public override void _Ready()
    {
        base._Ready();
        viewportLabel = viewport.GetNode<Label>("Label");
        viewportLabel.Text = text;
    }

    public override bool InitFromData(GameState.GameObjectConstructorData data)
    {
        if (base.InitFromData(data))
        {
            text = (string)data.paramList[0];
            return true;
        }
        return false;
    }
}