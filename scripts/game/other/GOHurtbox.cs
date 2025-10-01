using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[GlobalClass]
public partial class GOHurtbox : Area3D
{
    [Export]
    public float damagePerTick { get; set; }

    [Export]
    public bool instantKill { get; set; }

    [Export]
    public bool hurtsInnocents { get; set; }

    [Export]
    public bool hurtsTraitors { get; set; }

    [Export]
    public bool hurtsNPCs { get; set; }

    [Export]
    public bool active {  get; set; }

    internal void DoDamage()
    {
        if (!active)
        {
            return;
        }
        foreach (Node node in GetOverlappingBodies())
        {
            if (node is IsDamagable d)
            {
                if (instantKill)
                {
                    d.TakeDamage(d.maxHealth, 0);
                }
                else
                {
                    d.TakeDamage(damagePerTick, 0);
                }
            }
        }
    }
}

