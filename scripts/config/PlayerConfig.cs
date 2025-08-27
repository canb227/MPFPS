/// <summary>
/// Struct that is saved/loaded to disk and sent over network to communcate about player config settings.
/// Feel free to add whatever new fields you want in the struct definition, and set default values
/// Should NOT contain any private information, as this potentially gets shared over network.
/// MUST only use basic (non-reference) types, or structs that also consist of only basic types.
/// </summary>
public struct PlayerConfig
{
    public PlayerConfig() { }

    public int window_height { get; set; } = 1920;
    public int window_width { get; set; } = 1080;

}

