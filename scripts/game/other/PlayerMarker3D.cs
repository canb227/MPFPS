using System.Collections.Generic;
using Godot;
using Godot.Collections;
using MessagePack;

[GlobalClass]
public partial class PlayerMarker3D : Marker3D
{
    [Export] public bool canSpawnSecurity;
    [Export] public bool canSpawnOfficeWorker;
    [Export] public bool canSpawnWarehouseWorker;
}