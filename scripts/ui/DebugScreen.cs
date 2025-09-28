using Godot;
using Limbo.Console.Sharp;
using SteamMultiplayerPeerCSharp;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Reflection;


/// <summary>
/// Super messy code for the dev launcher UI screen
/// </summary>
public partial class DebugScreen : Control
{

    //Nodes

    // basic ui elements
    public Button StartGameButton;
    public Button QuitGameButton;
    public ColorRect hostHide;

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
        "res://scenes/world/devLevel.tscn",
        "res://scenes/world/debugPlatform.tscn",
        "res://scenes/world/debugFlat.tscn",
        "res://scenes/world/warehouse.tscn",
    };

    public static List<string> directLoadMap_mapIconPaths = new()
    {
        "res://assets/ui/img/devMapScreenie.png",
        "res://assets/ui/img/debugMapScreenie.png",
        "res://assets/ui/img/debugMapFlatScreenie.png",
        "res://assets/ui/img/debugMapFlatScreenie.png",

    };

    public static List<string> directLoadMap_mapNames = new()
    {
        "dev",
        "platform",
        "flat",
        "warehouse",
    };

    public static List<string> playerCharacters = new()
    {
        "ghost",
        "tony",
    };


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        //basic
        StartGameButton = GetNode<Button>("start");
        QuitGameButton = GetNode<Button>("quit");
        hostHide = GetNode<ColorRect>("hostHide");

        //direct load
        directLoadMap_mapList = GetNode<OptionButton>("directLoadMap/mapList");
        directLoadMap_mapImage = GetNode<TextureRect>("directLoadMap/img");

        //chat box
        chat_chatbar = GetNode<TextEdit>("chat/chatbar");
        chat_text = GetNode<RichTextLabel>("chat/chatbox/chattext");
        chat_send = GetNode<Button>("chat/send");

        //player list
        playerList_list = GetNode<VBoxContainer>("PlayerListBox/ScrollContainer/Players/PlayersVbox");

        //ui events
        directLoadMap_mapList.ItemSelected += DirectLoadMap_mapList_ItemSelected;
        chat_send.Pressed += Chat_send_Pressed;
        StartGameButton.Pressed += StartGameButton_Pressed;
        QuitGameButton.Pressed += QuitGameButton_Pressed;
        RPCManager.ChatReceivedEvent += RPCManager_ChatReceivedEvent;

        Lobby.NewLobbyPeerAddedEvent += Lobby_NewLobbyPeerAddedEvent;
        Lobby.LobbyPeerRemovedEvent += Lobby_LobbyPeerRemovedEvent;

        foreach (string map in directLoadMap_mapNames)
        {
            directLoadMap_mapList.AddItem(map);
        }

        hostHide.Visible = !Global.Lobby.bIsLobbyHost;
        StartGameButton.Disabled = !Global.Lobby.bIsLobbyHost;
        directLoadMap_mapList.Disabled = !Global.Lobby.bIsLobbyHost;
        DirectLoadMap_mapList_ItemSelected(0);
        if (Global.Lobby.bInLobby)
        {
            foreach (ulong peer in Global.Lobby.AllPeers())
            {
                Lobby_NewLobbyPeerAddedEvent(peer);
            }

        }
        DirectLoadMap_mapList_ItemSelected(0);


        Logging.Log("Debug Screen ready.", "DebugScreen");

    }

    private void DirectLoadMap_mapList_ItemSelected(long v)
    {
        directLoadMap_mapImage.Texture = ImageTexture.CreateFromImage(Image.LoadFromFile(directLoadMap_mapIconPaths[directLoadMap_mapList.Selected]));
    }

    private void Lobby_LobbyPeerRemovedEvent(ulong removedPlayerSteamID)
    {
        playerList_list.GetNode(removedPlayerSteamID.ToString()).QueueFree();
    }

    private void RPCManager_ChatReceivedEvent(string msg, ulong sender)
    {
        chat_text.AddText($"{SteamFriends.GetFriendPersonaName(new CSteamID(sender))}: {msg}");
    }

    private void QuitGameButton_Pressed()
    {
        Global.Lobby.LeaveLobby(true);
        Global.ui.ToMainMenuUI();
    }

    private void Lobby_NewLobbyPeerAddedEvent(ulong newPlayerSteamID)
    {
        Logging.Log($"Adding player {newPlayerSteamID} to debug screen.", "DebugScreen");
        Control playerListItem = ResourceLoader.Load<PackedScene>("res://scenes/ui/menu/playerListItem.tscn").Instantiate<Control>();
        playerListItem.GetNode<Label>("playername").Text = SteamFriends.GetFriendPersonaName(new CSteamID(newPlayerSteamID));
        playerListItem.GetNode<TextureRect>("icon").Texture = Utils.GetMediumSteamAvatar(newPlayerSteamID);
        //playerListItem.GetNode<Label>("level").Text = Global.GameSession.playerData[newPlayerSteamID].progression.AccountLevel.ToString();
        playerListItem.GetNode<Label>("id").Text = newPlayerSteamID.ToString();
        playerListItem.Name = newPlayerSteamID.ToString();

        if (newPlayerSteamID==Global.steamid)
        {
            foreach (string character in playerCharacters)
            {
                playerListItem.GetNode<OptionButton>("charSelect").AddItem(character);
            }
            playerListItem.GetNode<OptionButton>("charSelect").ItemSelected += (index) => OnCharSelect(playerCharacters[(int)index]);
            playerListItem.GetNode<ColorPickerButton>("colorSelect").ColorChanged += OnColorSelect;
        }
        else
        {
            playerListItem.GetNode<OptionButton>("charSelect").Disabled = true;
            playerListItem.GetNode<ColorPickerButton>("colorSelect").Disabled = true;
        }

            playerList_list.AddChild(playerListItem);


    }

    private void OnColorSelect(Color color)
    {
        Global.gameState.PlayerData[Global.steamid].color = color;
        Global.gameState.PushLocalPlayerData();
    }

    private void OnCharSelect(string character)
    {
        Global.gameState.PlayerData[Global.steamid].selectedCharacter = character;
        Global.gameState.PushLocalPlayerData();
    }


    private void StartGameButton_Pressed()
    {
        RPCManager.RPC_StartGame(directLoadMap_mapPaths[directLoadMap_mapList.Selected]);
    }


    private void Chat_send_Pressed()
    {
        RPCManager.RPC_Chat(chat_chatbar.Text);
        chat_chatbar.Text = "";
    }


}
