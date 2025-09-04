using Godot;

/// <summary>
/// Handles loading and saving from/to disk for player save files, progression, and config settings.
/// </summary>
public class Config //TODO: Rename this ConfigManager or smth idk?
{
    /// <summary>
    /// An in-memory cached version of the player's progression file that is saved to disk. Any changes made to this version will (try) to be automatically saved to disk.
    /// </summary>
    public PlayerProgression loadedPlayerProgression;

    /// <summary>
    /// An in-memory cached version of the player's config file that is saved to disk. Any changes made to this version will (try) to be automatically saved to disk.
    /// </summary>
    public PlayerConfig loadedPlayerConfig;

    /// <summary>
    /// Loads the player config and player progression from file and stores them in the loadedPlayerProgression and loadedPlayerConfig fields in this object.
    /// </summary>
    public void InitConfig()
    {
        LoadPlayerProgression();
        LoadPlayerConfig();
    }

    /// <summary>
    /// Attempts to load the player's progression file from disk. 
    /// If the file doesnt exist, it makes a new one with all fields set to defaults. 
    /// The filepath points to a Steam user specific folder inside "user://", which is resolved by Godot to an OS specific path.
    /// See <see cref="NetworkUtils.BytesToStruct{T}(byte[])"/> for details on the transformation.
    /// </summary>
    public void LoadPlayerProgression()
    {
        string filePath = $"user://saves/{Global.steamid}/progression.bytes";
        Logging.Log($"Attempting to load player progression at {filePath}", "Config");
        if (FileAccess.FileExists(filePath))
        {
            FileAccess file = FileAccess.Open(filePath, FileAccess.ModeFlags.ReadWrite);
            if (file != null)
            {
                loadedPlayerProgression = NetworkUtils.BytesToStruct<PlayerProgression>(file.GetBuffer((long)file.GetLength()));
                Logging.Log($"Successfully loaded player progression!", "Config");
            }
            else
            {
                Logging.Error($"Failed to load player progression at {filePath}. Reason: {FileAccess.GetOpenError().ToString()}", "Config");
            }
        }
        else
        {
            Logging.Warn($"Player Progression doesn't exist. Ignore if this the first run. Creating file.", "Config");
            PlayerProgression prg = new();
            loadedPlayerProgression = prg;
            SavePlayerProgression();
        }
    }

    /// <summary>
    /// Attempts to save the current loadedPlayerProgression object to disk. If the file already exists (it always does except for first start), make a backup first before overwriting.
    /// The filepath points to a Steam user specific folder inside "user://", which is resolved by Godot to an OS specific path.
    /// See <see cref="NetworkUtils.StructToBytes{T}(T)"/> for details on the transformation.
    /// </summary>
    public void SavePlayerProgression()
    {
        string filePath = $"user://saves/{Global.steamid}/progression.bytes";
        Logging.Log($"Attempting to save player progression at {filePath}", "Config");
        if (FileAccess.FileExists(filePath))
        {
            Logging.Log($"File already exists, making a backup then deleting it.", "Config");
            DirAccess.CopyAbsolute(filePath, $"user://saves/{Global.steamid}/progressionBACKUP.toml");
            DirAccess.RemoveAbsolute(filePath);
        }
        FileAccess file = FileAccess.Open(filePath, FileAccess.ModeFlags.WriteRead);
        if (file != null)
        {
            file.StoreBuffer(NetworkUtils.StructToBytes(loadedPlayerProgression));
            file.Close();
            Logging.Log($"Successfully saved player progression!", "Config");
        }
        else
        {
            Logging.Warn($"Failed to save player progression at {filePath}. Reason: {FileAccess.GetOpenError().ToString()}", "Config");
        }

    }

    /// <summary>
    /// Attempts to load the player's config file from disk. 
    /// If the file doesnt exist, it makes a new one with all fields set to defaults. 
    /// The filepath points to a Steam user specific folder inside "user://", which is resolved by Godot to an OS specific path.
    /// See <see cref="NetworkUtils.BytesToStruct{T}(byte[])"/> for details on the transformation.
    /// </summary>
    public void LoadPlayerConfig()
    {
        string filePath = $"user://config/{Global.steamid}/config.bytes";
        Logging.Log($"Attempting to load player config at {filePath}", "Config");
        if (FileAccess.FileExists(filePath))
        {
            FileAccess file = FileAccess.Open(filePath, FileAccess.ModeFlags.ReadWrite);
            if (file != null)
            {
                loadedPlayerConfig = NetworkUtils.BytesToStruct<PlayerConfig>(file.GetBuffer((long)file.GetLength()));
                Logging.Log($"Successfully loaded player config!", "Config");
            }
            else
            {
                Logging.Warn($"Failed to load player config at {filePath}. Reason: {FileAccess.GetOpenError().ToString()}", "Config");
            }
        }
        else
        {
            Logging.Log($"Player Config doesn't exist. Is this the first run? Creating file.", "Config");
            PlayerConfig cfg = new();
            loadedPlayerConfig = cfg;
            SavePlayerConfig();
        }
        Logging.Log($"SENS: {loadedPlayerConfig.mouseSensY}","temp");
    }

    /// <summary>
    /// Attempts to save the current loadedPlayerConfig object to disk. If the file already exists (it always does except for first start), make a backup first before overwriting.
    /// The filepath points to a Steam user specific folder inside "user://", which is resolved by Godot to an OS specific path.
    /// See <see cref="NetworkUtils.StructToBytes{T}(T)"/> for details on the transformation.
    /// </summary>
    public void SavePlayerConfig()
    {
        string filePath = $"user://config/{Global.steamid}/config.bytes";
        Logging.Log($"Attempting to save player config at {filePath}", "Config");
        if (FileAccess.FileExists(filePath))
        {
            Logging.Log($"File already exists, making a backup then deleting it.", "Config");
            DirAccess.CopyAbsolute(filePath, $"user://config/{Global.steamid}/configBACKUP.bytes");
            DirAccess.RemoveAbsolute(filePath);
        }
        FileAccess file = FileAccess.Open(filePath, FileAccess.ModeFlags.WriteRead);
        if (file != null)
        {
            file.StoreBuffer(NetworkUtils.StructToBytes(loadedPlayerConfig));
            file.Close();
            Logging.Log($"Successfully saved player config!", "Config");
        }
        else
        {
            Logging.Warn($"Failed to save player config at {filePath}. Reason: {FileAccess.GetOpenError().ToString()}", "Config");
        }
    }
}

