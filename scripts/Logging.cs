using Godot;
using Limbo.Console.Sharp;
using System;
using System.Collections.Generic;

/// <summary>
/// Static logger class to keep things organized.
/// </summary>
public static class Logging
{
    //Todo: Logging to file should be a launch option probably
    /// <summary>
    /// compile time constant! Set to true if the logger should start a new logfile at game launch and write to it
    /// </summary>
    public const bool bSaveLogsToFile = false;

    //Add logging categories to this to silence them - can also use the relevant console commands while the game is running to add or remove values
    /// <summary>
    /// List of Log Prefixes (categories) to silence
    /// </summary>
    public static List<string> SilencedPrefixes = ["FirstTimeSetup", "LoggingMeta", "NetworkRelay", "NetworkSession", "GameSessionWire", "NetworkWire"];

    /// <summary>
    /// Is true if Logger is functioning.
    /// </summary>
    public static bool IsStarted = false;



    private static List<string> SilencedPrefixesCurrent = SilencedPrefixes;
    private static FileAccess logFile = null;
    private static bool writeToFile = false;

    /// <summary>
    /// Starts up the logging engine. Logging functions don't work until after this called.
    /// </summary>
    public static void Start()
    {
        IsStarted = true;
    }

    /// <summary>
    /// Starts logging to file
    /// </summary>
    public static void StartLoggingToFile()
    {
        string logName = $"[{DateTime.Now.Year}-{DateTime.Now.Month}-{DateTime.Now.Day}]--[{DateTime.Now.Hour};{DateTime.Now.Minute};{DateTime.Now.Second}].txt";
        string fileName = $"user://logs/{Global.steamid.ToString()}/{logName}";
        Logging.Log($"Starting log file at: {fileName}", "LoggingMeta");
        Logging.logFile = FileAccess.Open(fileName, FileAccess.ModeFlags.WriteRead);
        if (Logging.logFile == null)
        {
            GD.PushError($"Error creating log file: {FileAccess.GetOpenError().ToString()}");
        }
        Logging.Log($"Log file created succesfully.", "LoggingMeta");
        writeToFile = true;
    }

    /// <summary>
    /// Prints the message to both the ingame console and to the default Godot console using default formatting. Also logs to file if <see cref="bSaveLogsToFile"/>
    /// </summary>
    /// <param name="message">message to log</param>
    /// <param name="prefix">A custom prefix. Leave blank to remove prefix.</param>
    /// <param name="timestamp">If true a system timestamp is added to the message.</param>
    public static void Log(string message, string prefix, bool timestamp = true)
    {
        if (SilencedPrefixesCurrent.Contains(prefix) || !IsStarted)
        {
            return;
        }

        string ts = "";
        if (timestamp) ts = $"[{Time.GetTimeStringFromSystem()}]";

        string customPrefix = "";
        if (prefix != "" && prefix != null) customPrefix = $"[{prefix}]";

        LimboConsole.Info(customPrefix + ts + message);
        GD.Print(customPrefix + ts + message);
        if (writeToFile)
        {
            if (logFile.StoreLine(customPrefix + ts + message))
            {
                logFile.Flush(); //Flush the buffer to disk per line in case we crash. This probably incurs a serious performance hit.
            }
            else
            {
                GD.PrintErr("ALERT! LOG TO FILE ERROR! Log file may be missing options!");
            }
        }
    }

    /// <summary>
    /// Prints the message to both the ingame console and to the default Godot console using warning formatting.
    /// </summary>
    /// <param name="message">message to log</param>
    /// <param name="prefix">A custom prefix. Leave blank to remove prefix.</param>
    /// <param name="timestamp">If true a system timestamp is added to the message.</param>
    public static void Warn(string message, string prefix, bool timestamp = true)
    {
        if (SilencedPrefixesCurrent.Contains(prefix) || !IsStarted)
        {
            return;
        }
        string ts = "";
        if (timestamp) ts = $"[{Time.GetTimeStringFromSystem()}]";

        string customPrefix = "";
        if (prefix != "" && prefix != null) customPrefix = $"[{prefix}]";

        LimboConsole.Warn(customPrefix + ts + message);
        GD.Print(customPrefix + ts + message);
        if (writeToFile)
        {
            if (logFile.StoreLine(customPrefix + ts + message))
            {
                logFile.Flush(); //Flush the buffer to disk per line in case we crash. This probably incurs a serious performance hit.
            }
            else
            {
                GD.PrintErr("ALERT! LOG TO FILE ERROR! Log file may be missing options!");
            }
        }
    }

    /// <summary>
    /// Prints the message to both the ingame console and to the default Godot console using error formatting.
    /// </summary>
    /// <param name="message">message to log</param>
    /// <param name="prefix">A custom prefix. Leave blank to remove prefix.</param>
    /// <param name="timestamp">If true a system timestamp is added to the message.</param>
    public static void Error(string message, string prefix, bool timestamp = true)
    {
        if (!IsStarted)
        {
            return;
        }
        string ts = "";
        if (timestamp) ts = $"[{Time.GetTimeStringFromSystem()}]";

        string customPrefix = "";
        if (prefix != "" && prefix != null) customPrefix = $"[{prefix}]";

        LimboConsole.Error(customPrefix + ts + message);
        GD.PushError(customPrefix + ts + message);
        if (writeToFile)
        {
            if (logFile.StoreLine(customPrefix + ts + message))
            {
                logFile.Flush(); //Flush the buffer to disk per line in case we crash. This probably incurs a serious performance hit.
            }
            else
            {
                GD.PrintErr("ALERT! LOG TO FILE ERROR! Log file may be missing options!");
            }
        }
    }

    public static void UnSilenceAllPrefixes()
    {
        SilencedPrefixesCurrent = new();
    }

    public static void ResetSilencedPrefixesToDefault()
    {
        SilencedPrefixesCurrent = new(SilencedPrefixes);
    }

    public static void SilencePrefix(string category)
    {
        SilencedPrefixesCurrent.Add(category);
    }

    public static void UnSilencePrefix(string category)
    {
        SilencedPrefixesCurrent.Remove(category);
    }
}

