using Godot;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public abstract partial class GODoor : GOBaseStaticBody, IsInteractable
{
    [Export]
    public virtual float interactCooldownSeconds { get; set; }
    public virtual ulong lastInteractTick { get; set; }
    public virtual ulong lastInteractPlayer { get; set; }

    public abstract bool CanInteract(ulong byID);

    public abstract void OnInteract(ulong byID);


    public override string GenerateStateString()
    {
        return MessagePackSerializer.ConvertToJson(GenerateStateUpdate());
    }
    public override void PerFrameAuth(double delta)
    {
        
    }

    public override void PerFrameLocal(double delta)
    {
        
    }

    public override void PerFrameShared(double delta)
    {
        
    }

    public override void PerTickAuth(double delta)
    {
        
    }

    public override void PerTickLocal(double delta)
    {
        
    }

    public override void PerTickShared(double delta)
    {
        
    }


}

