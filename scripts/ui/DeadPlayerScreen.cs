using Godot;
using Steamworks;


[GlobalClass]
public partial class DeadPlayerScreen : PanelContainer
{
    [Export] public DeadPlayerScreen deadPlayerScreen;
    [Export] public Label playerNameLabel;
    [Export] public Button closeWindowButton;
    [Export] public TextureRect teamIcon;
    [Export] public Label teamLabel;
    [Export] public TextureRect deathIcon;
    [Export] public Label deathLabel;
    [Export] public TextureRect lastSeenPlayerIcon;
    [Export] public Label lastSeenLabel;
    [Export] public Button closeWindowButton2;

    public override void _Ready()
    {
        base._Ready();
        closeWindowButton.Pressed += CloseDeadPlayerScreen;
        closeWindowButton2.Pressed += CloseDeadPlayerScreen;
    }

    public void OpenDeadPlayerScreen(BasicPlayerCharacter basicPlayerCharacter)
    {
        Input.MouseMode = Input.MouseModeEnum.Confined;
                        
        playerNameLabel.Text = "Body of " + SteamFriends.GetFriendPersonaName(new CSteamID(basicPlayerCharacter.authority));
        if (basicPlayerCharacter.team == Team.Innocent)
        {
            //teamIcon.Texture = ;
        }
        else if (basicPlayerCharacter.team == Team.Manager)
        {
            //teamIcon.Texture = ;
        }
        else if (basicPlayerCharacter.team == Team.Traitor)
        {
            //teamIcon.Texture = ;
        }

        teamLabel.Text = SteamFriends.GetFriendPersonaName(new CSteamID(basicPlayerCharacter.authority)) + " was a part of the ";
        if (basicPlayerCharacter.team == Team.Innocent)
        {
            teamLabel.Text += "Innocents";
        }
        else if (basicPlayerCharacter.team == Team.Manager)
        {
            teamLabel.Text += "Managers";
        }
        else if (basicPlayerCharacter.team == Team.Traitor)
        {
            teamLabel.Text += "Traitors";
        }

        deadPlayerScreen.Visible = true;
    }

    public void CloseDeadPlayerScreen()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
        deadPlayerScreen.Visible = false;
    }    
}