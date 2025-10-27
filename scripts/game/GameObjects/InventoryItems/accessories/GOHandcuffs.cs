using Godot;
using Godot.Collections;
using ImGuiGodot.Internal;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


[GlobalClass]
public partial class GOHandcuffs : GOBaseAccessory
{
    [Export] AudioStreamPlayer3D audioStreamPlayer { get; set; }
    public override void HandleInput(ActionFlags input)
    {
        if (!lastTickActions.HasFlag(ActionFlags.Fire) && input.HasFlag(ActionFlags.Fire))
        {
            if (interactRayCast.IsColliding())
            {
                var hit = interactRayCast.GetCollider();

                //we have to like climb up the scene tree to look for the actual object, because players have static bodies to represent just their head and body hitbox
                //we do this so they are on their own layers for precision hitboxes on layer 3 and phys capsules on layer 5. not opposed to redesigning that eventually
                Node current = (Node)hit;
                while (current != null && current is not BasicPlayerCharacter)
                    current = current.GetParent();

                if (current is BasicPlayerCharacter target)
                {
                    Logging.Log($"Hit a BasicPlayerCharacter object: " + target.currentStunBar, "GOHandcuffs");
                    if(target.state == CharacterState.Living)
                    {
                        if (GetHeldBy() is BasicPlayerCharacter basicPlayerCharacter)
                        {
                            basicPlayerCharacter.DropEquipped();
                        }
                        target.Handcuff(this);
                        audioStreamPlayer.Play();
                    }
                }
            }
        }
        base.HandleInput(input);
    }
}