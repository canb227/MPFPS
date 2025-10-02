using Godot;
using MessagePack;
using System;
using System.Collections.Generic;

using System.Linq;
using System.Text;
using System.Threading.Tasks;


public partial class GOStaticDestructible : GOBaseStaticBody, IsDamagable
{
    [Export]
    public float maxHealth { get; set; }
    public float currentHealth { get; set; }

    public override string GenerateStateString()
    {
        return MessagePackSerializer.ConvertToJson(GenerateStateUpdate());
    }

    public override byte[] GenerateStateUpdate()
    {
        StaticDestructibleState state = new();
        state.currentHealth = currentHealth;
        return MessagePackSerializer.Serialize(state);
    }

    public override void PerFrameAuth(double delta)
    {

    }

    public override void PerFrameLocal(double delta)
    {

    }

    public override void PerFrameShared(double delta)
    {

    }

    public override void PerTickAuth(double delta)
    {

    }

    public override void PerTickLocal(double delta)
    {

    }

    public override void PerTickShared(double delta)
    {

    }

    public override void ProcessStateUpdate(byte[] update)
    {
        StaticDestructibleState state = MessagePackSerializer.Deserialize<StaticDestructibleState>(update);
        currentHealth = state.currentHealth;
    }

    public void TakeDamage(float damage, ulong byID)
    {
        currentHealth -= damage;
        if (currentHealth < 0)
        {
            destroyed = true;
            Visible = false;
            GlobalPosition = new Vector3(0, -100, 0);
        }
    }
}

[MessagePackObject]
public struct StaticDestructibleState
{
    [Key(0)]
    public float currentHealth;
}