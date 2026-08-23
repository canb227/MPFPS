using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;

public partial class BulletDecal : Decal
{
    [Export] float lifetime;
    [Export] Texture2D decal1;
    [Export] Texture2D decal2;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Random rand = new();
        if (rand.Next(1) == 0)
        {
            TextureAlbedo = decal1;
        }
        else
        {
            TextureAlbedo = decal2;
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        lifetime -= (float)delta;
        if(lifetime <= 0.0f)
        {
            QueueFree();
        }
        else
        {
            float alpha = Math.Min(lifetime, 1.0f);
            Modulate = new Godot.Color(1,1,1,alpha);
        }
    }
}
