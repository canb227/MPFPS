

public interface IsInteractable
{
    public float interactCooldownSeconds { get; set; }
    public bool interactCooldownReady { get; set; }
    public void Local_OnInteract(ulong byID);
    public ulong lastInteractTick { get; set; }
    public ulong lastInteractPlayer { get; set; }

}
