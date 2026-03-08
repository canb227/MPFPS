using Godot;
using System;

public partial class ParticleDespawner : Node3D
{
	GpuParticles3D particleEmitter;
	public override void _Ready()
    {
		particleEmitter = GetNode<GpuParticles3D>("GPUParticles3D");
		particleEmitter.Emitting = true;
		particleEmitter.Finished += OnParticleEmitterFinished;
    }

    private void OnParticleEmitterFinished()
    {
		QueueFree();
    }
}
