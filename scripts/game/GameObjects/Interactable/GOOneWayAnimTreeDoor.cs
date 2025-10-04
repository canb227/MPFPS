using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public partial class GOOneWayAnimTreeDoor : GOBaseStaticInteractable
{
    [Export]
    public AnimationTree animationTree {  get; set; }

    [Export]
    public string openAnimationStateName { get; set; } = "opened";

    [Export]
    public string closeAnimationStateName { get; set; } = "closed";

    private bool openingOrOpen = false;
    private AnimationNodeStateMachinePlayback stateMachine { get; set; }

    public override void _Ready()
    {
        if (animationTree == null)
        {
            Logging.Error($"Door {Name} ({id}) could not load its Animation Tree! Check object properties.", "GODoor");
        }

        stateMachine = animationTree.Get("parameters/playback").As<AnimationNodeStateMachinePlayback>();
        if (stateMachine == null)
        {
            Logging.Error($"Door {Name} ({id}) could not load its Animation State machine! Check animation tree configuration.", "GODoor");
        }
    }

    [RPCMethod(RPCMode.SendToAllPeers)]
    public void Toggle(bool toOpen,ulong byID, ulong onTick)
    {
        if (!toOpen)
        {
            if (animationTree.HasNode(closeAnimationStateName))
            {
                stateMachine.Travel(closeAnimationStateName);
            }
            else
            {
                Logging.Error($"The AnimationTree State machine of {Name} ({id}) is missing a node that matches the requested state: {closeAnimationStateName}!", "GODoor");
            }

        }
        else
        {
            if (animationTree.HasNode(openAnimationStateName))
            {
                stateMachine.Travel(openAnimationStateName);
            }
            else
            {
                Logging.Error($"The AnimationTree State machine of {Name} ({id}) is missing a node that matches the requested state: {closeAnimationStateName}!", "GODoor");
            }
        }
    }

    [RPCMethod(RPCMode.OnlySendToAuth)]
    public override void Auth_HandleInteractionRequest(ulong byID, ulong onTick)
    {
        if (openingOrOpen)
        {
            openingOrOpen = false;
            RPCManager.RPC(this, MethodName.Toggle, [false, byID, onTick]);
        }
        else
        {
            openingOrOpen = true;
            RPCManager.RPC(this, MethodName.Toggle, [true, byID, onTick]);
        }
    }

    public override string GenerateStateString()
    {
        return $"OpenOrOpening?:{openingOrOpen}|currentAnimation:{stateMachine.GetCurrentNode()}";
    }
}

