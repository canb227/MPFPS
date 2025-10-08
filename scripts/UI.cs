using Godot;
using System;
using System.Collections.Generic;


/// <summary>
/// UI manager singleton. I don't really love this so I didn't document it as to not form an attachment.
/// </summary>
public partial class UI : Node
{

    public Control currentFullScreenUI;

    public Control currentLoadingScreen;
    public ProgressBar loadingProgressBar;

    public PlayerInputData localInput { get; set; }
    public ActionFlags lastTickActions { get; set; }

    private InGameUI playerUI { get; set; }

    public void PerTick(double delta)
    {
        if (localInput.actions.HasFlag(ActionFlags.ScoreBoard))
        {
            ShowScoreBoard();
        }
        else if (lastTickActions.HasFlag(ActionFlags.ScoreBoard))
        {
            HideScoreBoard();
        }
        lastTickActions = localInput.actions;
    }

    public void AddLocalInput()
    {
        if (Global.gameState.PlayerInputs.ContainsKey(Global.steamid))
        {
            localInput = Global.gameState.PlayerInputs[Global.steamid];
        }
        else
        {
            Logging.Error($"Tried registering local user input for UI handling but their steamid {Global.steamid} isn't in PlayerInputs", "UI");
        }
    }

    public Dictionary<string, string> FullScreenUIScenePaths = new Dictionary<string, string>()
    {
        { "NONE", "" },
        { "DEBUG_Launcher","res://scenes/ui/menu/DebugScreen.tscn" },
        { "UI_LoadingScreen","res://scenes/ui/menu/LoadingScreen.tscn" },
        { "UI_MainMenu", "res://scenes/ui/menu/MainMenu.tscn" },
        { "BasePlayerHUD", "res://scenes/ui/hud/BasePlayerHUD.tscn" },
        { "InGameUI", "res://scenes/ui/hud/InGameUI.tscn"}
    };

    public void ToLobbyUI()
    {
        SwitchFullScreenUI("DEBUG_Launcher");
    }
    internal void ToMainMenuUI()
    {
        SwitchFullScreenUI("UI_MainMenu");
    }

    public void ToGameUI()
    {
        playerUI = (InGameUI)SwitchFullScreenUI("InGameUI");
        playerUI.PlayerUIManager.Visible = false;
    }

    public void ToPlayerCharacterUI()
    {
        playerUI = (InGameUI)SwitchFullScreenUI("InGameUI");
        playerUI.PlayerUIManager.Visible = true;
    }


    public Control SwitchFullScreenUI(string sceneName)
    {
        Logging.Log($"Setting fullscreen UI to: {sceneName}.", "UI");
        ClearFullScreenUI();
        if (sceneName.Equals("NONE"))
        {
            return null;
        }

        Control loadedUI = LoadUI(sceneName);
        currentFullScreenUI = loadedUI;
        currentFullScreenUI.Show();
        return loadedUI;
    }

    private Control LoadUI(string sceneName)
    {
        Control loadedUI = ResourceLoader.Load<PackedScene>(FullScreenUIScenePaths[sceneName]).Instantiate<Control>();
        loadedUI.Name = sceneName;
        loadedUI.Hide();
        AddChild(loadedUI);
        return loadedUI;
    }

    private void ClearFullScreenUI()
    {
        Logging.Log($"Clearing fullscreen UI", "UI");
        if (currentFullScreenUI != null) currentFullScreenUI.Hide();
        currentFullScreenUI = null;
    }

    internal void StartLoadingScreen(string loadingScreenSceneName = "UI_LoadingScreen")
    {
        Logging.Log($"Bringing up loading screen.", "UI_Loading");
        ClearFullScreenUI();
        Control loadingScreen = LoadUI(loadingScreenSceneName);
        currentLoadingScreen = loadingScreen;
        loadingProgressBar = loadingScreen.GetNode<ProgressBar>("bar");
        loadingScreen.Show();
    }

    internal void UpdateLoadingScreenProgressBar(double progress)
    {
        if (loadingProgressBar != null)
        {
            loadingProgressBar.Value = progress;
            Logging.Log($"Updating loading screen bar to: {progress}", "UI_Loading");
        }
        else
        {
            Logging.Error("There is no valid loading screen loaded, cannot update progress bar value.", "UI_Loading");
        }
    }

    public void SetLoadingScreenDescription(string description)
    {
        Logging.Log($"Setting load screen progress description to: {description}", "UI_Loading");
        currentLoadingScreen.GetNode<Label>("description").Text = description;
    }

    internal void StopLoadingScreen()
    {
        Logging.Log($"Removing and resetting loading screen.", "UI_Loading");
        loadingProgressBar.Value = 0;
        currentLoadingScreen.GetNode<Label>("description").Text = "PLEASE SET ME";
        currentLoadingScreen.Hide();
    }
    
        
    public void ShowScoreBoard()
    {
        if (!playerUI.ScoreBoardUI.Visible)
        {
            playerUI.ScoreBoardUI.Visible = true;
        }
    }

    public void HideScoreBoard()
    {
        if (playerUI.ScoreBoardUI.Visible)
        {
            playerUI.ScoreBoardUI.Visible = false;
        }
    }



    //internal void PregameWaitingForPlayers()
    //{
    //    //probably like, load our UI, but gray it out or smth and put "waiting for players on screen?"
    //}

    //internal void InGameStart()
    //{
    //    Logging.Log("Enabling ingame UI.","UI");
    //}
}
