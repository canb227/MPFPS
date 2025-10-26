using Godot;
using System;

[GlobalClass]
public partial class RadarMarker : MeshInstance3D
{
    [Export] private Label distanceLabel;
    [Export] private PanelContainer panelContainer;
    [Export] private float lifetime = 10f;
    [Export] private float fadeDuration = 5f;
    [Export] private float scaleFactor = 0.04f; // tweak this 
    [Export] private float minScale = 0.3f;
    [Export] private float maxScale = 5f;

    private Node3D target;
    private Node3D origin;
    private float timer;

    public void Init(Node3D origin, Node3D target, Color color, float lifetime)
    {
        this.lifetime = lifetime;
        this.origin = origin;
        this.target = target;
        timer = lifetime;
        var stylebox = new StyleBoxTexture();
        stylebox.Texture = ResourceLoader.Load<CompressedTexture2D>("res://assets/ui/img/circle.png");
        stylebox.ModulateColor = color;
        panelContainer.AddThemeStyleboxOverride("panel", stylebox);
    }

    public override void _Process(double delta)
    {
        if (target == null || origin == null)
        {
            QueueFree();
            return;
        }
        
        // Distance
        float dist = origin.GlobalPosition.DistanceTo(target.GlobalPosition);

        // Update label
        distanceLabel.Text = $"{dist:0}m";

        // Position marker
        GlobalPosition = target.GlobalPosition;

        // Scale with distance
        float s = Mathf.Clamp(dist * scaleFactor, minScale, maxScale);
        Scale = new Vector3(s, s, s);

        // Lifetime fade
        timer -= (float)delta;
        if (timer <= 0)
        {
            float alpha = Mathf.Clamp(timer / -fadeDuration, 0, 1);
            distanceLabel.Modulate = new Color(1, 1, 1, 1 - alpha);
            panelContainer.Modulate = new Color(panelContainer.Modulate.R, panelContainer.Modulate.G, panelContainer.Modulate.B, 1 - alpha);
            if (alpha >= 1) QueueFree();
        }
        var targetNoY = new Vector3(origin.GlobalPosition.X, GlobalPosition.Y, origin.GlobalPosition.Z);
        LookAt(targetNoY, Vector3.Up);
        RotateY(Mathf.Pi);
    }
}
