using Godot;
using Limbo.Console.Sharp;
using SteamMultiplayerPeerCSharp;
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

    public static List<string> directLoadMap_mapIconPaths = new()
    {

        { "res://assets/ui/img/debugMapScreenie.png" },
        { "res://assets/ui/img/debugMapFlatScreenie.png" },

    };

    public static List<string> directLoadMap_mapNames = new()
    {

        { "platform" },
        { "flat" },

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

        NetworkManager.HostedServerEvent += NetworkManager_HostedServerEvent;
        Multiplayer.ConnectedToServer += Multiplayer_ConnectedToServer;
        Multiplayer.PeerConnected += Multiplayer_PeerConnected;

        foreach (string map in directLoadMap_mapNames)
        {
            directLoadMap_mapList.AddItem(map);
        }



        Logging.Log("Debug Screen ready.", "DebugScreen");
        DirectLoadMap_mapList_ItemSelected(0);
    }

    private void DirectLoadMap_mapList_ItemSelected(long index)
    {
        directLoadMap_mapImage.Texture = ImageTexture.CreateFromImage(Image.LoadFromFile(directLoadMap_mapIconPaths[(int)index]));
    }

    private void DirectLoadMap_loadCheck_Toggled(bool toggledOn)
    {
        directLoadMap_hidePanel.Visible = !toggledOn;
    }

    private void Multiplayer_PeerConnected(long id)
    {
        AddNewPlayer((ulong)id);
    }

    private void Multiplayer_ConnectedToServer()
    {
        AddNewPlayer((ulong)Multiplayer.GetUniqueId());
    }

    private void NetworkManager_HostedServerEvent()
    {
        Logging.Log("Just Hosted a server! time to update the UI!", "DebugScreen");
        AddNewPlayer((ulong)Multiplayer.GetUniqueId());
    }

    private void AddNewPlayer(ulong newPlayerSteamID)
    {
        Logging.Log($"Adding player {newPlayerSteamID} to debug screen.", "DebugScreen");
        Control playerListItem = ResourceLoader.Load<PackedScene>("res://scenes/ui/playerListItem.tscn").Instantiate<Control>();
        
        if (!Global.OFFLINE_MODE)
        {
           // playerListItem.GetNode<Label>("playername").Text = SteamFriends.GetFriendPersonaName(new CSteamID(newPlayerSteamID));
            //playerListItem.GetNode<TextureRect>("icon").Texture = Utils.GetMediumSteamAvatar(newPlayerSteamID);
        }


        playerListItem.GetNode<Label>("id").Text = newPlayerSteamID.ToString();
        playerList_list.AddChild(playerListItem);
    }

    private void StartGameButton_Pressed()
    {
        if (directLoadMap_loadCheck.ButtonPressed)
        {
            Global.GameState.Rpc(GameState.MethodName.StartGame, directLoadMap_mapNames[directLoadMap_mapList.Selected]);
        }

    }


    private void Chat_send_Pressed()
    {
        Rpc(MethodName.Chat, [Multiplayer.GetUniqueId().ToString(), chat_chatbar.Text]);
        chat_chatbar.Text = "";
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Chat(string sender,string message)
    {
        chat_text.Text+=($"{sender}: {message}\n");

    }


    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
}
