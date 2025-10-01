using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


[GlobalClass]
public partial class GOSlidingDoor : GOBaseStaticBody, IsInteractable
{
    public ulong interactCooldownSeconds { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public InteractableCooldownSetting InteractableCooldownSetting { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public ulong lastInteractTick { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public ulong lastInteractPlayer { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public bool CanInteract(ulong byID)
    {
        throw new NotImplementedException();
    }

    public override string GenerateStateString()
    {
        throw new NotImplementedException();
    }

    public override byte[] GenerateStateUpdate()
    {
        throw new NotImplementedException();
    }

    public void OnInteract(ulong byID)
    {
        throw new NotImplementedException();
    }

    public override void PerFrameAuth(double delta)
    {
        throw new NotImplementedException();
    }

    public override void PerFrameLocal(double delta)
    {
        throw new NotImplementedException();
    }

    public override void PerTickAuth(double delta)
    {
        throw new NotImplementedException();
    }

    public override void PerTickLocal(double delta)
    {
        throw new NotImplementedException();
    }

    public override void ProcessStateUpdate(byte[] update)
    {
        throw new NotImplementedException();
    }
}

