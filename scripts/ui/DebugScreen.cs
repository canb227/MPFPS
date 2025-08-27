using Godot;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Reflection;

public partial class DebugScreen : Control
{

    //Nodes

    // basic ui elements
    public Button StartGameButton;
    public Button QuitGameButton;

    //direct load map box
    public CheckBox directLoadMap_loadCheck;
    public OptionButton directLoadMap_mapList;
    public Panel directLoadMap_hidePanel;
    public TextureRect directLoadMap_mapImage;

    //session option box


    //chat box
    public Button chat_send;
    public RichTextLabel chat_text;
    public TextEdit chat_chatbar;

    //player list
    public VBoxContainer playerList_list;

    //Vars
    public bool directLoadMap = false;

    public static List<string> directLoadMap_mapPaths = new()
    {

        { "res://scenes/world/debugPlatform.tscn" },
        { "res://scenes/world/debugFlat.tscn" },

    };

    public  static List<string> directLoadMap_mapIconPaths = new()
    {

        { "res://assets/ui/img/debugMapScreenie.png" },
        { "res://assets/ui/img/debugMapFlatScreenie.png" },

    };

    public static List<string> directLoadMap_mapNames = new()
    {

        { "DebugPlatform" },
        { "DebugFlat" },

    };
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        //basic
        StartGameButton = GetNode<Button>("start");
        QuitGameButton = GetNode<Button>("quit");

        //direct load
        directLoadMap_loadCheck = GetNode<CheckBox>("directLoadMap/loadCheck");
        directLoadMap_mapList = GetNode<OptionButton>("directLoadMap/mapList");
		directLoadMap_hidePanel = GetNode<Panel>("directLoadMap/hide");
        directLoadMap_mapImage = GetNode<TextureRect>("directLoadMap/img");

        //session box

        //chat box
        chat_chatbar = GetNode<TextEdit>("chat/chatbar");
        chat_text = GetNode<RichTextLabel>("chat/chatbox/chattext");
        chat_send = GetNode<Button>("chat/send");

        //player list
        playerList_list = GetNode<VBoxContainer>("PlayerListBox/ScrollContainer/Players/PlayersVbox");

        //ui events
        directLoadMap_loadCheck.Toggled += DirectLoadMap_loadCheck_Toggled;
        directLoadMap_mapList.ItemSelected += DirectLoadMap_mapList_ItemSelected;
        chat_send.Pressed += Chat_send_Pressed;
        StartGameButton.Pressed += StartGameButton_Pressed;

        //other events
        GameSession.GameSessionOptionsChangedEvent += GameSession_OptionsChangedEvent;
        GameSession.GameSessionNewPlayerEvent += GameSession_NewPlayerEvent;


        foreach (string map in directLoadMap_mapNames)
        {
            directLoadMap_mapList.AddItem(map);
        }


        Logging.Log("Debug Screen ready.", "DebugScreen");

    }

    private void GameSession_NewPlayerEvent(ulong newPlayerSteamID)
    {
        Logging.Log($"Adding player {newPlayerSteamID} to debug screen.", "DebugScreen");
        Control playerListItem = ResourceLoader.Load<PackedScene>("res://scenes/ui/playerListItem.tscn").Instantiate<Control>();
        playerListItem.GetNode<Label>("playername").Text = SteamFriends.GetFriendPersonaName(new CSteamID(newPlayerSteamID));
        playerListItem.GetNode<TextureRect>("icon").Texture = Utils.GetMediumSteamAvatar(newPlayerSteamID);
        playerListItem.GetNode<Label>("level").Text = Global.GameSession.playerData[newPlayerSteamID].progression.AccountLevel.ToString();
        playerListItem.GetNode<Label>("id").Text = newPlayerSteamID.ToString();
        playerList_list.AddChild(playerListItem);
    }

    private void StartGameButton_Pressed()
    {

    }

    private void GameSession_OptionsChangedEvent()
    {
        if (Global.GameSession.sessionOptions.DEBUG_DIRECTLOADMAP)
        {
            directLoadMap = true;
            directLoadMap_hidePanel.Visible = false;
            StartGameButton.Text = "Start Game On Selected Map";
            directLoadMap_mapImage.Texture = ImageTexture.CreateFromImage(Image.LoadFromFile(directLoadMap_mapIconPaths[Global.GameSession.sessionOptions.DEBUG_DIRECTLOADMAPINDEX]));
            directLoadMap_mapList.Selected = Global.GameSession.sessionOptions.DEBUG_DIRECTLOADMAPINDEX;
            chat_text.AddText($"SYSTEM: Enabled map direct load; map:{directLoadMap_mapPaths[Global.GameSession.sessionOptions.DEBUG_DIRECTLOADMAPINDEX]}\n");
        }
        else
        {
            directLoadMap = false;
            directLoadMap_hidePanel.Visible = true;
            StartGameButton.Text = "Start Game";
            chat_text.AddText($"SYSTEM: Disabled map direct load.\n");
        }
    }

    private void Chat_send_Pressed()
    {
        throw new NotImplementedException();
    }

    private void DirectLoadMap_mapList_ItemSelected(long index)
    {
        Global.GameSession.sessionOptions.DEBUG_DIRECTLOADMAPINDEX = (int)index;

        Global.GameSession.BroadcastSessionMessage(NetworkUtils.StructToBytes<SessionOptions>(Global.GameSession.sessionOptions), SessionMessageType.RESPONSE_SESSIONOPTIONS);
    }

    private void DirectLoadMap_loadCheck_Toggled(bool toggledOn)
    {
       
        if (toggledOn)
        {
            Global.GameSession.sessionOptions.DEBUG_DIRECTLOADMAP = true;
            Global.GameSession.sessionOptions.DEBUG_DIRECTLOADMAPINDEX = 0;
            Global.GameSession.BroadcastSessionMessage(NetworkUtils.StructToBytes<SessionOptions>(Global.GameSession.sessionOptions), SessionMessageType.RESPONSE_SESSIONOPTIONS);

        }
        else
        {
            Global.GameSession.sessionOptions.DEBUG_DIRECTLOADMAP = false;
            Global.GameSession.sessionOptions.DEBUG_DIRECTLOADMAPINDEX = 0;
            Global.GameSession.BroadcastSessionMessage(NetworkUtils.StructToBytes<SessionOptions>(Global.GameSession.sessionOptions), SessionMessageType.RESPONSE_SESSIONOPTIONS);

        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{
	}
}
