using Godot;

public partial class UI : Control
{


    public override void _Ready()
    {

    }

    public void LoadDebugSessionScreen()
    {
        PackedScene pck = ResourceLoader.Load<PackedScene>("res://scenes/ui/DebugScreen.tscn");
        Control dbg = pck.Instantiate<Control>();
        dbg.Name = "dbg";
        AddChild(dbg);
    }

    internal void HideDebugScreen()
    {
        GetNode<Control>("dbg").Hide();
    }
}
