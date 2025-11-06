using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[GlobalClass]
public partial class GODoor : GOTrap
{
    bool doorOpen = true;
    public void ToggleDoor()
    {
        if (doorOpen)
        {
            animationPlayer.Play("close_door");
            doorOpen = false;
        }
        else
        {
            animationPlayer.Play("open_door");
            doorOpen = true;
        }        
    }
}
