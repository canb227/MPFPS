using Godot;
using System;
using System.Collections.Generic;
using Tomlyn;

[Flags]
public enum ActionFlags : UInt64
{
    None = 0,

    MoveForward = 1L<<0,
    MoveBackward = 1L<<1,
    MoveLeft = 1L<<2,
    MoveRight = 1L<<3,

    Jump = 1L<<4,
    Crouch = 1L<<5,
    CrouchToggle = 1L<<6,
    Sprint = 1L<<7,
    SprintToggle = 1L<<8,
    Prone = 1L << 9,
    ProneToggle = 1L << 10,

    Use = 1L<<11,
    Reload = 1L << 12,

    Fire = 1L<<13,
    Aim = 1L<<14,
    AimToggle = 1L<<15,

    LeanLeft = 1L<<16,
    LeanLeftToggle = 1L<<17,
    LeanRight = 1L<<18,
    LeanRightToggle = 1L<<19,

    LookUp = 1L<<20,
    LookDown = 1L<<21,
    LookLeft = 1L<<22,
    LookRight = 1L<<23,

    Ability1 = 1L<<24,
    Ability2 = 1L<<25,
    Ability3 = 1L<<26,
    Ability4 = 1L<<27,

    InventorySlot1 = 1L<<28,
    InventorySlot2 = 1L<<29,
    InventorySlot3 = 1L<<30,
    InventorySlot4 = 1L<<31,
    InventorySlot5 = 1L<<32,
    NextSlot = 1L<<33,
    PrevSlot = 1L<<34,
    LastSlot = 1L<<35,


    Escape = 1L<<62,//this is the max value


}
/// <summary>
/// Handles saving/loading input maps from disk, and also handles dynamic key remapping.
/// </summary>
public static class InputMapManager
{
    /// <summary>
    /// An in-memory cached version of the player's inputmap file that is saved to disk. Any changes made to this version will (try) to be automatically saved to disk.
    /// </summary>
    public static PlayerInputMap loadedPlayerInputMap;

    public static Dictionary<string, ActionFlags> actionNameToActionFlagMap = new();

    /// <summary>
    /// Loads the player's input map file from disk and registers all of the keybinds with the engine. If no changes are made to keybinds in game, this is the only thing the InputMapManager will do.
    /// </summary>
    public static void InitInputMap()
    {
        Logging.Log($"Starting InputMapManager...", "InputMapping");
        LoadPlayerInputMap();

        //First we load the names of all our actions and register them with the engine. This is a programmatic version of adding them in Godot Editor->Project->Project Settings->Input Map.
        string[] actionNames = Enum.GetNames(typeof(ActionFlags));
        foreach (string actionName in actionNames)
        {
            actionNameToActionFlagMap[actionName] = Enum.Parse<ActionFlags>(actionName);
            InputMap.AddAction(actionName);
        }
        Logging.Log($"Successfully loaded {actionNames.Length} total possible actions", "InputMapping");

        //Next we load all of our keyboard keybinds and assign them to the indicated action. This is a programmatic version of adding a specific keybind to an action in Godot Editor->Project->Project Settings->Input Map.
        foreach (Key key in loadedPlayerInputMap.KeyboardKeyCodeToActionMap.Keys)
        {
            InputEventKey keyEvent = new();
            keyEvent.PhysicalKeycode = key;
            InputMap.ActionAddEvent(Enum.GetName<ActionFlags>(loadedPlayerInputMap.KeyboardKeyCodeToActionMap[key]), keyEvent);
        }
        Logging.Log($"Successfully loaded {loadedPlayerInputMap.KeyboardKeyCodeToActionMap.Count} Keyboard binds.", "InputMapping");

        //Next we load all of our mouse button binds and assign them to the indicated action. This is a programmatic version of adding a specific keybind to an action in Godot Editor->Project->Project Settings->Input Map.
        foreach (MouseButton mb in loadedPlayerInputMap.MouseButtonToActionMap.Keys)
        {
            InputEventMouseButton mbEvent = new();
            mbEvent.ButtonIndex = mb;
            InputMap.ActionAddEvent(Enum.GetName<ActionFlags>(loadedPlayerInputMap.MouseButtonToActionMap[mb]), mbEvent);
        }
        Logging.Log($"Successfully loaded {loadedPlayerInputMap.MouseButtonToActionMap.Count} Mouse Button binds", "InputMapping");

        //Next we load all of our gamepad button binds and assign them to the indicated action. This is a programmatic version of adding a specific keybind to an action in Godot Editor->Project->Project Settings->Input Map.
        foreach (JoyButton jb in loadedPlayerInputMap.JoypadButtonToActionMap.Keys)
        {
            InputEventJoypadButton jbEvent = new();
            jbEvent.ButtonIndex = jb;
            InputMap.ActionAddEvent(Enum.GetName<ActionFlags>(loadedPlayerInputMap.JoypadButtonToActionMap[jb]), jbEvent);
        }
        Logging.Log($"Successfully loaded {loadedPlayerInputMap.JoypadButtonToActionMap.Count} Gamepad Button binds", "InputMapping");

        Logging.Log($"InputMap loading complete. Loaded a total of {loadedPlayerInputMap.JoypadButtonToActionMap.Count + loadedPlayerInputMap.MouseButtonToActionMap.Count + loadedPlayerInputMap.KeyboardKeyCodeToActionMap.Count} binds.", "InputMapping");
    }

    /// <summary>
    /// Unbinds a key, removing its association with any action, then save the updated inputmap to disk.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public static string UnbindKeyboardKey(Key key)
    {
        if (loadedPlayerInputMap.KeyboardKeyCodeToActionMap.ContainsKey(key))
        {
            string boundActionName = Enum.GetName(loadedPlayerInputMap.KeyboardKeyCodeToActionMap[key]);
            InputEventKey keyEvent = new();
            keyEvent.PhysicalKeycode = key;
            InputMap.ActionEraseEvent(boundActionName, keyEvent);
            loadedPlayerInputMap.KeyboardKeyCodeToActionMap.Remove(key);
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

    public static string UnbindMouseButton(MouseButton mb)
    {
        if (loadedPlayerInputMap.MouseButtonToActionMap.ContainsKey(mb))
        {
            string boundActionName = Enum.GetName(loadedPlayerInputMap.MouseButtonToActionMap[mb]);
            InputEventMouseButton mouseEvent = new();
            mouseEvent.ButtonIndex = mb;
            InputMap.ActionEraseEvent(boundActionName, mouseEvent);
            loadedPlayerInputMap.MouseButtonToActionMap.Remove(mb);
            Logging.Log($"InputMapManager Unbinding Success! MouseButton:{mb.ToString()}", "InputMapping");
            SavePlayerInputMap();
            return boundActionName;
        }
        else
        {
            Logging.Warn($"InputMapManager Unbinding Failed! Mouse Button is not bound. MouseButton:{mb.ToString()}", "InputMapping");
            return null;
        }
    }

    /// <summary>
    /// Binds a key to an action.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="action"></param>
    /// <param name="overwrite"></param>
    public static void BindKeyboardKey(Key key, ActionFlags action, bool overwrite = false)
    {
        InputEventKey inputEventKey = new();
        inputEventKey.PhysicalKeycode = key;
        if (loadedPlayerInputMap.KeyboardKeyCodeToActionMap.ContainsKey(key))
        {
            if (overwrite)
            {
                string oldAction = UnbindKeyboardKey(key);
                loadedPlayerInputMap.KeyboardKeyCodeToActionMap.Add(key, action);
                InputMap.ActionAddEvent(Enum.GetName(loadedPlayerInputMap.KeyboardKeyCodeToActionMap[key]), inputEventKey);
                Logging.Log($"InputMapManager Binding (with overwrite) Success! Key:{key.ToString()} Action:{action} OldAction:{oldAction}", "InputMapping");
            }
            else
            {
                Logging.Warn($"InputMapManager Binding Failed! Key is already bound (use overwrite param to bypass) Key:{key.ToString()} Action:{action}", "InputMapping");
            }

        }
        else
        {
            loadedPlayerInputMap.KeyboardKeyCodeToActionMap.Add(key, action);
            InputMap.ActionAddEvent(Enum.GetName(loadedPlayerInputMap.KeyboardKeyCodeToActionMap[key]), inputEventKey);
            Logging.Log($"InputMapManager Binding Success! Key:{key.ToString()} Action:{action}", "InputMapping");
        }
        SavePlayerInputMap();
    }

    /// <summary>
    /// Binds a key to an action.
    /// </summary>
    /// <param name="mb"></param>
    /// <param name="action"></param>
    /// <param name="overwrite"></param>
    public static void BindMouseButton(MouseButton mb, ActionFlags action, bool overwrite = false)
    {
        InputEventMouseButton inputEventMB = new();
        inputEventMB.ButtonIndex = mb;

        if (loadedPlayerInputMap.MouseButtonToActionMap.ContainsKey(mb))
        {
            if (overwrite)
            {
                string oldAction = UnbindMouseButton(mb);
                loadedPlayerInputMap.MouseButtonToActionMap.Add(mb, action);
                InputMap.ActionAddEvent(Enum.GetName(loadedPlayerInputMap.MouseButtonToActionMap[mb]), inputEventMB);
                Logging.Log($"InputMapManager Binding (with overwrite) Success! MouseButton:{mb.ToString()} Action:{action} OldAction:{oldAction}", "InputMapping");
            }
            else
            {
                Logging.Warn($"InputMapManager Binding Failed! MouseButton is already bound (use overwrite param to bypass) MouseButton:{mb.ToString()} Action:{action}", "InputMapping");
            }

        }
        else
        {
            loadedPlayerInputMap.MouseButtonToActionMap.Add(mb, action);
            InputMap.ActionAddEvent(Enum.GetName(loadedPlayerInputMap.MouseButtonToActionMap[mb]), inputEventMB);
            Logging.Log($"InputMapManager Binding Success! MouseButton:{mb.ToString()} Action:{action}", "InputMapping");
        }
        SavePlayerInputMap();
    }

    /// <summary>
    /// Attempts to load the player's inputmap file from disk. 
    /// If the file doesnt exist, it makes a new one with all fields set to defaults. 
    /// The filepath points to a Steam user specific folder inside "user://", which is resolved by Godot to an OS specific path.
    /// See <see cref="NetworkUtils.BytesToStruct{T}(byte[])"/> for details on the transformation.
    /// </summary>
    public static void LoadPlayerInputMap()
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
    public static void SavePlayerInputMap()
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
