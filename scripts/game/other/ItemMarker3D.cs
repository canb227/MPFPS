using System.Collections.Generic;
using Godot;
using Godot.Collections;
using MessagePack;

[GlobalClass]
public partial class ItemMarker3D : Marker3D
{
    [Export] public bool canSpawnWeapons;
    [Export] public bool canSpawnPackageItems;
    [Export] public bool canSpawnComponents;
    [Export] public bool canSpawnAccessories;
    [Export(PropertyHint.Range, "0, 100")] public int generalWeight = 1; //effects likelyhood this marker is chosen for mission critical items
    [Export(PropertyHint.Range, "0.0, 1.0")] public float spawnChance = 1.0f; //chance a random item is spawned here
}