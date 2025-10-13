using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[GlobalClass]
public partial class Hurtbox : Area3D
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

    public override void _Ready()
    {
        BodyEntered += Hurtbox_BodyEntered;
    }

    private void Hurtbox_BodyEntered(Node3D body)
    {

    }

    public override void _PhysicsProcess(double delta)
    {
        foreach (Node3D node in GetOverlappingBodies())
        {
            if (node is GameObject go)
            {
                if (go.authority == Global.steamid)
                {
                    if (go is IsDamagable d)
                    {
                        RPCManager.RPC(node, "TakeDamage", [damagePerTick,(ulong)0]);
                    }
                }
                else
                {
                    if (go is IsDamagable d)
                    {

                    }
                }
            }
        }
    }

}

