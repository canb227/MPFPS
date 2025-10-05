using System.Net.Sockets;
using Godot;
using MessagePack;

[GlobalClass]
public partial class GOLabelPaper : SimpleShape
{
    [Export]
    public SubViewport viewport { get; set; }
    private Label viewportLabel { get; set; }
    
    public int addressID = 0;

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
            addressID = (int)data.paramList[1];
            return true;
        }
        return false;
    }
}