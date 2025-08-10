using Godot;
using Limbo.Console.Sharp;

/// <summary>
/// Static logger class to keep things organized.
/// </summary>
public static class Logging
{
    //Todo: add real logging (to file) at some poitn

    /// <summary>
    /// Prints the message to both the ingame console and to the default Godot console using default formatting.
    /// </summary>
    /// <param name="message">message to log</param>
    /// <param name="prefix">A custom prefix. Leave blank to remove prefix.</param>
    /// <param name="timestamp">If true a system timestamp is added to the message.</param>
    public static void Log(string message, string prefix = "", bool timestamp = true)
    {
        string ts = "";
        if (timestamp) ts = $"[{Time.GetTimeStringFromSystem()}]";

        string customPrefix = "";
        if (prefix != "" && prefix != null) customPrefix = $"[{prefix}]";

        LimboConsole.Info(customPrefix + ts + message);
        GD.Print(customPrefix + ts + message);
    }

    /// <summary>
    /// Prints the message to both the ingame console and to the default Godot console using warning formatting.
    /// </summary>
    /// <param name="message">message to log</param>
    /// <param name="prefix">A custom prefix. Leave blank to remove prefix.</param>
    /// <param name="timestamp">If true a system timestamp is added to the message.</param>
    public static void Warn(string message, string prefix, bool timestamp = true)
    {
        string ts = "";
        if (timestamp) ts = $"[{Time.GetTimeStringFromSystem()}]";

        string customPrefix = "";
        if (prefix != "" && prefix != null) customPrefix = $"[{prefix}]";

        LimboConsole.Warn(customPrefix + ts + message);
        GD.Print(customPrefix + ts + message);
    }

    /// <summary>
    /// Prints the message to both the ingame console and to the default Godot console using error formatting.
    /// </summary>
    /// <param name="message">message to log</param>
    /// <param name="prefix">A custom prefix. Leave blank to remove prefix.</param>
    /// <param name="timestamp">If true a system timestamp is added to the message.</param>
    public static void Error(string message, string prefix, bool timestamp = true)
    {
        string ts = "";
        if (timestamp) ts = $"[{Time.GetTimeStringFromSystem()}]";

        string customPrefix = "";
        if (prefix != "" && prefix != null) customPrefix = $"[{prefix}]";

        LimboConsole.Error(customPrefix + ts + message);
        GD.PrintErr(customPrefix + ts + message);
    }
}

