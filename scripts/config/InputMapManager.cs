using Godot;
using System;
using System.Collections.Generic;
using Tomlyn;

[Flags]
public enum Actions
{
    None = 0,

    MoveForward = 1<<0,
    MoveBackward = 1<<1,
    MoveLeft = 1<<2,
    MoveRight = 1<<3,

    Jump = 1<<4,
    Crouch = 1<<5,
    Sprint = 1<<6,
    Use = 1<<7,
    Fire = 1<<8,
    Aim = 1<<9,

    LookUp = 1<<10,
    LookDown = 1<<11,
    LookLeft = 1<<12,
    LookRight = 1<<12,
    
    Ability1 = 1<<13,
    Ability2 = 1<<14,
    Ability3 = 1<<15,
    Ability4 = 1<<16,
}
/// <summary>
/// Handles saving/loading input maps from disk, and also handles dynamic key remapping.
/// </summary>
public class InputMapManager
{
    /// <summary>
    /// An in-memory cached version of the player's inputmap file that is saved to disk. Any changes made to this version will (try) to be automatically saved to disk.
    /// </summary>
    public PlayerInputMap loadedPlayerInputMap;

    /// <summary>
    /// List of possible input actions that we can bind keys to. Also holds display names for each input for use in remap screens or other places.
    /// You can safely change display names at any time, as they are not tied to any functionality.
    /// </summary>
    public readonly Dictionary<string, string> InputActionList = new() {

        { "MOVE_FORWARD", "Forward" },
        { "MOVE_BACKWARD", "Backward" },
        { "MOVE_LEFT", "Strafe Left" },
        { "MOVE_RIGHT", "Strafe Right" },
        { "LOOK_UP", "Look Up" },
        { "LOOK_DOWN", "Look Down" },
        { "LOOK_LEFT", "Look Left" },
        { "LOOK_RIGHT", "Look Right" },
        { "JUMP", "Jump/Mount" },
        { "FIRE", "Fire Primary Weapon" },
        { "AIM", "Aim Down Sights (ADS)" },
        { "AIM_TOGGLE", "Toggle Aim Down Sights (ADS)" },
        { "USE", "Interact" },
        { "SPRINT", "Sprint" },
        { "SPRINT_TOGGLE", "Toggle Sprint" },
        { "CROUCH", "Toggle Crouch" },
        { "CROUCH_TOGGLE", "Toggle Crouch" },
    };

    public Dictionary<string, Actions> flagMap = new Dictionary<string, Actions>()
    {
        { "MOVE_FORWARD",Actions.MoveForward },
        { "MOVE_BACKWARD",Actions.MoveBackward },
        { "MOVE_LEFT",Actions.MoveLeft },
        { "MOVE_RIGHT",Actions.MoveRight },
        { "CROUCH", Actions.Crouch },
        { "JUMP", Actions.Jump },
        { "SPRINT", Actions.Sprint },
        { "USE", Actions.Use },

    };

    /// <summary>
    /// Loads the player's input map file from disk and registers all of the keybinds with the engine. If no changes are made to keybinds in game, this is the only thing the InputMapManager will do.
    /// </summary>
    public void InitInputMap()
    {
        Logging.Log($"Starting InputMapManager...", "InputMapping");
        LoadPlayerInputMap();

        //First we load the names of all our actions and register them with the engine. This is a programmatic version of adding them in Godot Editor->Project->Project Settings->Input Map.
        foreach (string inputActionName in InputActionList.Keys)
        {
            InputMap.AddAction(inputActionName);
        }
        Logging.Log($"Successfully loaded {InputActionList.Count} total possible actions", "InputMapping");

        //Next we load all of our keyboard keybinds and assign them to the indicated action. This is a programmatic version of adding a specific keybind to an action in Godot Editor->Project->Project Settings->Input Map.
        foreach (Key key in loadedPlayerInputMap.KeyboardKeyCodeToActionMap.Keys)
        {
            InputEventKey keyEvent = new();
            keyEvent.PhysicalKeycode = key;
            InputMap.ActionAddEvent(loadedPlayerInputMap.KeyboardKeyCodeToActionMap[(int)key], keyEvent);
        }
        Logging.Log($"Successfully loaded {loadedPlayerInputMap.KeyboardKeyCodeToActionMap.Count} Keyboard binds.", "InputMapping");

        //Next we load all of our mouse button binds and assign them to the indicated action. This is a programmatic version of adding a specific keybind to an action in Godot Editor->Project->Project Settings->Input Map.
        foreach (MouseButton mb in loadedPlayerInputMap.MouseButtonToActionMap.Keys)
        {
            InputEventMouseButton mbEvent = new();
            mbEvent.ButtonIndex = mb;
            InputMap.ActionAddEvent(loadedPlayerInputMap.MouseButtonToActionMap[(int)mb], mbEvent);
        }
        Logging.Log($"Successfully loaded {loadedPlayerInputMap.MouseButtonToActionMap.Count} Mouse Button binds", "InputMapping");

        //Next we load all of our gamepad button binds and assign them to the indicated action. This is a programmatic version of adding a specific keybind to an action in Godot Editor->Project->Project Settings->Input Map.
        foreach (JoyButton jb in loadedPlayerInputMap.JoypadButtonToActionMap.Keys)
        {
            InputEventJoypadButton jbEvent = new();
            jbEvent.ButtonIndex = jb;
            InputMap.ActionAddEvent(loadedPlayerInputMap.JoypadButtonToActionMap[(int)jb], jbEvent);
        }
        Logging.Log($"Successfully loaded {loadedPlayerInputMap.JoypadButtonToActionMap.Count} Gamepad Button binds", "InputMapping");

        Logging.Log($"InputMap loading complete. Loaded a total of {loadedPlayerInputMap.JoypadButtonToActionMap.Count + loadedPlayerInputMap.MouseButtonToActionMap.Count + loadedPlayerInputMap.KeyboardKeyCodeToActionMap.Count} binds.", "InputMapping");
    }

    /// <summary>
    /// Unbinds a key, removing its association with any action, then save the updated inputmap to disk.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public string UnbindKey(Key key)
    {
        if (loadedPlayerInputMap.KeyboardKeyCodeToActionMap.ContainsKey((int)key))
        {
            string boundActionName = loadedPlayerInputMap.KeyboardKeyCodeToActionMap[(int)key];
            InputEventKey keyEvent = new();
            keyEvent.PhysicalKeycode = key;
            InputMap.ActionEraseEvent(boundActionName, keyEvent);
            loadedPlayerInputMap.KeyboardKeyCodeToActionMap.Remove((int)key);
            Logging.Log($"InputMapManager Unbinding Success! Key:{key.ToString()}", "InputMapping");
            SavePlayerInputMap();
            return boundActionName;
        }
        else
        {
            Logging.Warn($"InputMapManager Unbinding Failed! Key is not bound. Key:{key.ToString()}", "InputMapping");
            return null;
        }
    }

    /// <summary>
    /// Binds a key to an action using the key's name/label. This is kinda shit, I recommend using <see cref="BindKey(Key, string, bool)"/> instead/
    /// </summary>
    /// <param name="key"></param>
    /// <param name="action"></param>
    /// <param name="overwrite"></param>
    public void BindKeyString(string key, string action, bool overwrite = false)
    {
        Key keyCode = Key.None;
        try
        {
            keyCode = (Key)Enum.Parse(typeof(Key), key);
        }
        catch (Exception e)
        {
            Logging.Error($"Key parse failed: {e.ToString()}", "InputMapping");
        }
        if (keyCode != Key.None)
        {
            BindKey(keyCode, action, overwrite);
        }
        else
        {
            Logging.Log($"Bind Failed. Specified keystring '{key}' did not map to any known keycodes", "InputMapping");
        }
    }

    /// <summary>
    /// Binds a key to an action.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="action"></param>
    /// <param name="overwrite"></param>
    public void BindKey(Key key, string action, bool overwrite = false)
    {
        InputEventKey inputEventKey = new();
        inputEventKey.PhysicalKeycode = key;

        if (loadedPlayerInputMap.KeyboardKeyCodeToActionMap.ContainsKey((int)key))
        {
            if (overwrite)
            {
                string oldAction = UnbindKey(key);
                InputEventKey keyEvent = new();
                keyEvent.PhysicalKeycode = key;
                loadedPlayerInputMap.KeyboardKeyCodeToActionMap.Add((int)key, action);
                InputMap.ActionAddEvent(action, inputEventKey);
                Logging.Log($"InputMapManager Binding (with overwrite) Success! Key:{key.ToString()} Action:{action} OldAction:{oldAction}", "InputMapping");
            }
            else
            {
                Logging.Warn($"InputMapManager Binding Failed! Key is already bound (use overwrite param to bypass) Key:{key.ToString()} Action:{action}", "InputMapping");
            }

        }
        else

        {
            loadedPlayerInputMap.KeyboardKeyCodeToActionMap.Add((int)key, action);
            InputMap.ActionAddEvent(action, inputEventKey);
        }
        SavePlayerInputMap();
    }

    /// <summary>
    /// Attempts to load the player's inputmap file from disk. 
    /// If the file doesnt exist, it makes a new one with all fields set to defaults. 
    /// The filepath points to a Steam user specific folder inside "user://", which is resolved by Godot to an OS specific path.
    /// See <see cref="NetworkUtils.BytesToStruct{T}(byte[])"/> for details on the transformation.
    /// </summary>
    public void LoadPlayerInputMap()
    {

        string filePath = $"user://config/{Global.steamid}/input.toml";
        Logging.Log($"Attempting to load player input map at {filePath}", "InputMapping");
        if (FileAccess.FileExists(filePath))
        {
            FileAccess file = FileAccess.Open(filePath, FileAccess.ModeFlags.ReadWrite);
            if (file != null)
            {
                try
                {
                    loadedPlayerInputMap = Toml.ToModel<PlayerInputMap>(file.GetAsText());
                }
                catch (Exception ex)
                {
                    Logging.Error($"PreGameLoading input map file failed! Reason: {ex.ToString()}", "InputMapping");
                    Logging.Warn($"Error parsing saved InputMap file, attempting to recover by recreating a default input map.", "InputMapping");
                    PlayerInputMap map = new();
                    loadedPlayerInputMap = map;
                    SavePlayerInputMap();
                }
                Logging.Log($"Input map file loaded succesfully!", "InputMapping");
            }
            else
            {
                Logging.Error($"PreGameLoading input map file failed! Reason: {FileAccess.GetOpenError().ToString()}", "InputMapping");
            }
        }
        else
        {
            Logging.Warn($"Player InputMapManager Map doesn't exist. Is this the first run? Creating file.", "InputMapping");
            PlayerInputMap map = new();
            loadedPlayerInputMap = map;
            SavePlayerInputMap();
        }
    }

    /// <summary>
    /// Attempts to save the current loadedPlayerInputMap object to disk. If the file already exists (it always does except for first start), make a backup first before overwriting.
    /// The filepath points to a Steam user specific folder inside "user://", which is resolved by Godot to an OS specific path.
    /// See <see cref="NetworkUtils.StructToBytes{T}(T)"/> for details on the transformation.
    /// </summary>
    public void SavePlayerInputMap()
    {
        string filePath = $"user://config/{Global.steamid}/input.toml";
        Logging.Log($"Attempting to save player input map at {filePath}", "InputMapping");
        if (FileAccess.FileExists(filePath))
        {
            Logging.Log($"File already exists, making a backup the deleting it", "InputMapping");
            DirAccess.CopyAbsolute(filePath, $"user://config/{Global.steamid}/inputBACKUP.toml");
            DirAccess.RemoveAbsolute(filePath);
        }
        FileAccess file = FileAccess.Open(filePath, FileAccess.ModeFlags.WriteRead);
        if (file != null)
        {
            string tomlString = Toml.FromModel(loadedPlayerInputMap);
            file.StoreString(tomlString);
            file.Close();
            Logging.Log($"InputMapManager map saved succesfully!", "InputMapping");
        }
        else
        {
            Logging.Log($"Saving input map failed! Reason: {FileAccess.GetOpenError().ToString()}", "InputMapping");
        }
    }
}
