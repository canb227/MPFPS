using Godot;
using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[GlobalClass]
public partial class Entity : Node3D
{
    public ulong authority { get; set; }
    public ulong eid { get; set; }

    [Export]
    public string name { get; set; }

    public void Update(IMessage message)
    {
        throw new NotImplementedException();
    }
}

