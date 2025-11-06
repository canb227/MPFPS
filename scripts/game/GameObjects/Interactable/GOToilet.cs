using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[GlobalClass]
public partial class GOToilet : GOBaseStaticInteractable
{
    [Export] public AudioStreamPlayer3D audioStreamPlayer3D;
    public override void Auth_HandleInteractionRequest(ulong byID, ulong onTick)
    {
        if(!audioStreamPlayer3D.Playing)
        {
            audioStreamPlayer3D.Play();
        }
    }
}