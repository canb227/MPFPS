using Godot;
using System;
using System.Linq;

public partial class MainMenu : Control
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        GetNode<Button>("BUTTON_start").Pressed += OnStartPressed;
        GetNode<Button>("BUTTON_options").Pressed += OnOptionsPressed;
        GetNode<Button>("BUTTON_quit").Pressed += OnQuitPressed;
    }

    private void OnQuitPressed()
    {
        Main.QuitGame();
    }

    private void OnOptionsPressed()
    {
        throw new NotImplementedException();
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
