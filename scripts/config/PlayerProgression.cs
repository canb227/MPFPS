/// <summary>
/// Struct that is saved/loaded to disk and sent over network to communcate about player progression.
/// Feel free to add whatever new fields you want in the struct definition, and set default values
/// Should NOT contain any private information, as this gets shared over network.
/// MUST only use basic (non-reference) types, or structs that also consist of only basic types.
/// </summary>
public struct PlayerProgression
{
    public PlayerProgression() { }

    public int AccountLevel { get; set; } = 1;

}

