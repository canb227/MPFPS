using Godot;
using System;
using System.Linq;

public partial class MainMenu : Control
{
    [Export] Button startButton;
    [Export] Button optionsButton;
    [Export] Button quitButton;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        startButton.Pressed += OnStartPressed;
        optionsButton.Pressed += OnOptionsPressed;
        quitButton.Pressed += OnQuitPressed;
    }

    private void OnQuitPressed()
    {
        Main.QuitGame();
    }

    private void OnOptionsPressed()
    {
        Global.ui.SwitchFullScreenUI("UI_OptionsScreen");
    }

    private void OnStartPressed()
    {
        if (!Global.Lobby.bInLobby)
        {
            Global.Lobby.HostNewLobby();
        }
        Global.ui.ToLobbyUI();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{
	}
}
