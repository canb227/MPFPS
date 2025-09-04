using Godot;

public partial class PlayerCharacter : Character
{
    public Camera3D cam;

    public override void _Ready()
    {
        this.body = GetNode<CharacterBody3D>("body");
        this.cam = GetNode<Camera3D>("cam");
    }

}

