using Godot;

[GlobalClass]
public partial class RoundReportUI : PanelContainer
{
    [Export] public PanelContainer ResultBackground;
    [Export] public Label ResultLabel;
    [Export] public Label PlayerCountLabel;
    [Export] public Label RoundLastLabel;
    [Export] public VBoxContainer DeliveriesVBox;
    [Export] public PackedScene DeliveryRowScene;
    public void ShowRoundReport(Team winningTeam)
    {
        Visible = true;
    }
    public void NewRound()
    {
        Visible = false;
    }
}