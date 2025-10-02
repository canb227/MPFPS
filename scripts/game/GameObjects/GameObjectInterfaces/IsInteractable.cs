using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public interface IsInteractable
{
    [Export]
    public float interactCooldownSeconds { get; set; }

    public bool CanInteract(ulong byID);
    public void OnInteract(ulong byID);
    public ulong lastInteractTick { get; set; }
    public ulong lastInteractPlayer { get; set; }

}
