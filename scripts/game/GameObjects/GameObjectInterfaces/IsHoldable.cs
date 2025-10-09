public interface IsHoldable
{
    public ulong currentlyHeldBy { get; set; }


    public bool customHeldPhysics { get; set; }

    public bool snapHoldNoPhysics { get; set; }
    public float heldWeight { get; set; }
    public float heldDrag {  get; set; }
    public float heldFriction { get; set; }

    void OnHold(ulong byID);
    void OnRelease(ulong byID);
}