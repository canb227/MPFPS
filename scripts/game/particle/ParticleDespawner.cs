using Godot;
using System;

public partial class ParticleDespawner : Node3D
{
	GpuParticles3D particleEmitter;
	GpuParticles3D particleEmitter2;
	public override void _Ready()
    {
		particleEmitter = GetNode<GpuParticles3D>("GPUParticles3D");
		particleEmitter2 = GetNode<GpuParticles3D>("GPUParticles3D2");
		particleEmitter.Emitting = true;
		particleEmitter2.Emitting = true;
		particleEmitter.Finished += OnParticleEmitterFinished;
    }

    private void OnParticleEmitterFinished()
    {
		QueueFree();
    }
}
