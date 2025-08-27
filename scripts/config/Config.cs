using Godot;

public class Config
{
    public PlayerProgression loadedPlayerProgression;
    public PlayerConfig loadedPlayerConfig;

    public void InitConfig()
    {
        LoadPlayerProgression();
        LoadPlayerConfig();
    }

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


    public void LoadPlayerConfig()
    { 
        string filePath = $"user://config/{Global.steamid}/config.bytes";
        Logging.Log($"Attempting to load player config at {filePath}", "Config");
        if (FileAccess.FileExists(filePath))
        {
            FileAccess file = FileAccess.Open(filePath, FileAccess.ModeFlags.ReadWrite);
            if (file!=null)
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
    }

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

