using Godot;
using Limbo.Console.Sharp;
using SteamMultiplayerPeerCSharp;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Sockets;
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
	public LineEdit chat_chatbar;

	//player list
	public VBoxContainer playerList_list;

	//Vars
	public bool directLoadMap = false;

	public static List<string> directLoadMap_mapPaths = new()
	{
		"res://scenes/world/ai_testscene.tscn",
		"res://scenes/world/tutorial.tscn",
		"res://scenes/world/warehouse.tscn",
		"res://scenes/world/devLevel.tscn",
	};

	public static List<string> directLoadMap_mapIconPaths = new()
	{
		"res://assets/ui/tutorial_1.png",
		"res://assets/ui/tutorial_1.png",
		"res://assets/ui/mainmenu_1.png",
		"res://assets/ui/img/devMapScreenie.png",
	};

	public static List<string> directLoadMap_mapNames = new()
	{
		"aitest",
		"tutorial",
		"warehouse",
		"dev",
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
		chat_chatbar = GetNode<LineEdit>("chat/chatbar");
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
		GameState.PlayerDataReceivedEvent += GameState_PlayerDataReceivedEvent;
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
				Logging.Log($"Lobby already has players in it, manually adding one of them :{peer}", "DebugScreen");
				Lobby_NewLobbyPeerAddedEvent(peer);
			}

		}
		DirectLoadMap_mapList_ItemSelected(0);

		//options
		optNode = GetNode<Control>("sessionOptions");

		ItemsPerPackage = optNode.GetNode<TextEdit>("ItemsPerPackageEdit");
		ItemsPerPackage.TextChanged += GameOptionChanged;

		NumberOfPackages = optNode.GetNode<TextEdit>("NumberOfPackagesEdit");
		NumberOfPackages.TextChanged += GameOptionChanged;

		PackagesPerPlayer = optNode.GetNode<TextEdit>("PackagesPerPlayerEdit");
		PackagesPerPlayer.TextChanged += GameOptionChanged;

		UsePackageOverride = optNode.GetNode<CheckBox>("UsePackageOverrideCheck");
		UsePackageOverride.Pressed += GameOptionChanged;

		RoundTime = optNode.GetNode<TextEdit>("RoundTimeEdit");
		RoundTime.TextChanged += GameOptionChanged;

		ExtraTime = optNode.GetNode<TextEdit>("ExtraTimeEdit");
		ExtraTime.TextChanged += GameOptionChanged;

		PercentTraitors = optNode.GetNode<TextEdit>("PercentTraitorsEdit");
		PercentTraitors.TextChanged += GameOptionChanged;

		MaxTraitors = optNode.GetNode<TextEdit>("MaxTraitorsEdit");
		MaxTraitors.TextChanged += GameOptionChanged;

		PercentManagers = optNode.GetNode<TextEdit>("PercentManagersEdit");
		PercentManagers.TextChanged += GameOptionChanged;

		ManualTeamOverride = optNode.GetNode<CheckBox>("ManualTeamOverrideCheck");
		ManualTeamOverride.Pressed += GameOptionChanged;

		ManualTraitorCount = optNode.GetNode<TextEdit>("ManualTraitorCountEdit");
		ManualTraitorCount.TextChanged += GameOptionChanged;

		ManualManagerCount = optNode.GetNode<TextEdit>("ManualManagerCountEdit");
		ManualManagerCount.TextChanged += GameOptionChanged;

		chat_chatbar.GrabFocus();
		Logging.Log("Debug Screen ready.", "DebugScreen");

	}

	private void GameState_PlayerDataReceivedEvent(PlayerData data, ulong sender)
	{
		Control playerListItem = playerList_list.GetNode<Control>(sender.ToString());
		playerListItem.GetNode<OptionButton>("roleSelect").Select((int)data.role);
	}

	private Control optNode;
	private TextEdit ItemsPerPackage;
	private TextEdit NumberOfPackages;
	private TextEdit PackagesPerPlayer;
	private CheckBox UsePackageOverride;
	private TextEdit ExtraTime;
	private TextEdit RoundTime;

	private TextEdit PercentTraitors;
	private TextEdit MaxTraitors;
	private TextEdit PercentManagers;

	private CheckBox ManualTeamOverride;
	private TextEdit ManualTraitorCount;
	private TextEdit ManualManagerCount;


	private void GameOptionChanged()
	{
		var optNode = GetNode<Control>("sessionOptions");
		var opts = Global.gameState.gameModeManager.options;

		opts.itemsPerPackage = int.Parse(ItemsPerPackage.Text);
		opts.numPackages = int.Parse(NumberOfPackages.Text);
		opts.packagePerPlayer = float.Parse(PackagesPerPlayer.Text);
		opts.usePackageOverride = UsePackageOverride.ButtonPressed;
		opts.roundTime = int.Parse(RoundTime.Text);
		opts.timeAddedPerPackage = int.Parse(ExtraTime.Text);

		opts.percentTraitors = float.Parse(PercentTraitors.Text);
		//opts.maxTraitors = int.Parse(MaxTraitors.Text);
		opts.percentManagers = float.Parse(PercentManagers.Text);

		opts.manualTeamOverride = ManualTeamOverride.ButtonPressed;
		opts.manualTraitorCount = int.Parse(ManualTraitorCount.Text);
		opts.manualManagerCount = int.Parse(ManualManagerCount.Text);

		Global.gameState.gameModeManager.PushGameStateOptions();
	}

	private void DirectLoadMap_mapList_ItemSelected(long v)
	{
		directLoadMap_mapImage.Texture = ResourceLoader.Load<Texture2D>(directLoadMap_mapIconPaths[directLoadMap_mapList.Selected]);
	}

	private void Lobby_LobbyPeerRemovedEvent(ulong removedPlayerSteamID)
	{
		playerList_list.GetNode(removedPlayerSteamID.ToString()).QueueFree();
	}

	private void RPCManager_ChatReceivedEvent(string msg, ulong sender)
	{

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
			playerListItem.GetNode<OptionButton>("roleSelect").ItemSelected += (index) => OnRoleSelect((Role)index);
			playerListItem.GetNode<ColorPickerButton>("colorSelect").ColorChanged += OnColorSelect;
			playerListItem.GetNode<OptionButton>("roleSelect").Select(0);
			Global.gameState.PlayerData[Global.steamid].role = (Role)0;
			playerList_list.AddChild(playerListItem);
			Global.gameState.PushLocalPlayerData();
		}
		else
		{
			playerListItem.GetNode<OptionButton>("roleSelect").Select(0);
			playerListItem.GetNode<OptionButton>("roleSelect").Disabled = true;
			playerListItem.GetNode<ColorPickerButton>("colorSelect").Disabled = true;
			playerList_list.AddChild(playerListItem);
		}

	}

	private void OnColorSelect(Color color)
	{
		Global.gameState.PlayerData[Global.steamid].color = color;
		Global.gameState.PushLocalPlayerData();
	}

	private void OnRoleSelect(Role role)
	{
		Global.gameState.PlayerData[Global.steamid].role = role;
		Global.gameState.PushLocalPlayerData();
	}


	private void StartGameButton_Pressed()
	{
		RPCManager.RPC(Global.gameState,"StartGame", [directLoadMap_mapPaths[directLoadMap_mapList.Selected],GameModeType.TTT]);
	}


	private void Chat_send_Pressed()
	{
		if (chat_chatbar.Text.Length > 0)
		{
			RPCManager.RPC(this, "ChatMessage", [chat_chatbar.Text, Global.steamid]);
		}
		chat_chatbar.Text = "";
	}

	[RPCMethod(RPCMode.SendToAllPeers)]
	public void ChatMessage(string message, ulong from)
	{
		chat_text.AddText($"{SteamFriends.GetFriendPersonaName(new CSteamID(from))}: {message}\n");
	}

	public override void _Input(InputEvent @event)
	{
		if (!Global.bConsoleOpen && @event is InputEventKey k && k.Keycode == Key.Enter && k.Pressed)
		{
			Chat_send_Pressed();
			chat_chatbar.GrabFocus();
			GetViewport().SetInputAsHandled();
		}
	}
}
