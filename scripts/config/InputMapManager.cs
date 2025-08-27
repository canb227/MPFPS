using Godot;
using Limbo.Console.Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tomlyn;

public class InputMapManager
{
    public PlayerInputMap loadedPlayerInputMap;

    public void InitInputMap()
    {
        Logging.Log($"Starting InputMapManager...", "InputMapping");
        LoadPlayerInputMap();

        foreach (string inputActionName in loadedPlayerInputMap.InputActionList)
        {
            InputMap.AddAction(inputActionName);
        }
        Logging.Log($"Successfully loaded {loadedPlayerInputMap.InputActionList.Count} total possible actions", "InputMapping");

        foreach (Key key in loadedPlayerInputMap.KeyboardKeyCodeToActionMap.Keys)
        {
            InputEventKey keyEvent = new();
            keyEvent.PhysicalKeycode = key;
            InputMap.ActionAddEvent(loadedPlayerInputMap.KeyboardKeyCodeToActionMap[(int)key], keyEvent);
        }
        Logging.Log($"Successfully loaded {loadedPlayerInputMap.KeyboardKeyCodeToActionMap.Count} Keyboard binds.", "InputMapping");

        foreach (MouseButton mb in loadedPlayerInputMap.MouseButtonToActionMap.Keys)
        {
            InputEventMouseButton mbEvent = new();
            mbEvent.ButtonIndex = mb;
            InputMap.ActionAddEvent(loadedPlayerInputMap.MouseButtonToActionMap[(int)mb], mbEvent);
        }
        Logging.Log($"Successfully loaded {loadedPlayerInputMap.MouseButtonToActionMap.Count} Mouse Button binds", "InputMapping");

        foreach (JoyButton jb in loadedPlayerInputMap.JoypadButtonToActionMap.Keys)
        {
            InputEventJoypadButton jbEvent = new();
            jbEvent.ButtonIndex = jb;
            InputMap.ActionAddEvent(loadedPlayerInputMap.JoypadButtonToActionMap[(int)jb], jbEvent);
        }
        Logging.Log($"Successfully loaded {loadedPlayerInputMap.JoypadButtonToActionMap.Count} Gamepad Button binds", "InputMapping");

        Logging.Log($"InputMap loading complete. Loaded a total of {loadedPlayerInputMap.JoypadButtonToActionMap.Count+loadedPlayerInputMap.MouseButtonToActionMap.Count+loadedPlayerInputMap.KeyboardKeyCodeToActionMap.Count} binds.", "InputMapping");
    }

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

    public void BindKeyString(string key, string action, bool overwrite = false)
    {
        Key keyCode = Key.None;
        try
        {
            keyCode = (Key)Enum.Parse(typeof(Key), key);
        }
        catch (Exception e)
        {
            Logging.Error($"Key parse failed: {e.ToString()}","InputMapping");
        }
        if (keyCode!=Key.None)
        {
            BindKey(keyCode, action, overwrite);
        }
        else
        {
            Logging.Log($"Bind Failed. Specified keystring '{key}' did not map to any known keycodes", "InputMapping");
        }
    }

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

    public void LoadPlayerInputMap()
    {
        
        string filePath = $"user://config/{Global.steamid}/input.toml";
        Logging.Log($"Attempting to load player input map at {filePath}","InputMapping");
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
