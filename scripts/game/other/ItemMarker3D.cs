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
    [Export] public float spawnWeaponWeight = 1.0f;
    [Export] public float spawnPackageItemWeight = 1.0f;
    [Export] public float spawnComponentsWeight = 1.0f;
    [Export] public float generalWeight = 1.0f;
}