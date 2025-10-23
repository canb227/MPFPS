using Godot;
using System;

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
        if (winningTeam == Team.Traitor)
        {
            if (ResultBackground.HasThemeStyleboxOverride("panel"))
            {
                ResultBackground.RemoveThemeStyleboxOverride("panel");
            }
            var style = new StyleBoxFlat();
            style.BgColor = new Color(0.509f, 0.001f, 0.002f);
            style.CornerRadiusTopLeft = 8;
            style.CornerRadiusTopRight = 8;
            style.CornerRadiusBottomLeft = 8;
            style.CornerRadiusBottomRight = 8;
            ResultBackground.AddThemeStyleboxOverride("panel", style);

            ResultLabel.Text = "THE TRAITORS WIN";
        }
        else
        {
            if (ResultBackground.HasThemeStyleboxOverride("panel"))
            {
                ResultBackground.RemoveThemeStyleboxOverride("panel");
            }
            var style = new StyleBoxFlat();
            style.BgColor = new Color(0.002f, 0.501f, 0.002f);
            style.CornerRadiusTopLeft = 8;
            style.CornerRadiusTopRight = 8;
            style.CornerRadiusBottomLeft = 8;
            style.CornerRadiusBottomRight = 8;
            ResultBackground.AddThemeStyleboxOverride("panel", style);

            ResultLabel.Text = "THE INNOCENTS WIN";
        }
        if (Global.gameState.gameModeManager.numManagers > 0)
        {
            PlayerCountLabel.Text = $"{Global.gameState.gameModeManager.numPlayers} Players took part, {Global.gameState.gameModeManager.numManagers} of them were managers, and {Global.gameState.gameModeManager.numTraitors} of them were traitors!";
        }
        else
        {
            PlayerCountLabel.Text = $"{Global.gameState.gameModeManager.numPlayers} Players took part, and {Global.gameState.gameModeManager.numTraitors} of them were traitors!";
        }
        RoundLastLabel.Text = $"The round lasted {TimeSpan.FromSeconds(Global.gameState.gameModeManager.options.roundTime - Global.gameState.gameModeManager.remainingRoundTime):mm\\:ss}";
    }
    public void NewRound()
    {
        Visible = false;
    }
}