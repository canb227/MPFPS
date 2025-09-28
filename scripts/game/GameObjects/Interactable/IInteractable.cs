using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public interface IInteractable
{
    public ulong lastInteractTick { get; set; }
    public ulong lastInteractPlayer { get; set; }
    public Godot.Collections.Array<Node> triggers { get; set; }
    public ulong cooldown { get; set; }
    public void OnInteract();
}

