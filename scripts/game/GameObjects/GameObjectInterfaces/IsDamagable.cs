using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public interface IsDamagable
{
    public float maxHealth { get; set; }
    public float currentHealth { get; set; }
    public void TakeDamage(float damage, ulong byID, PainSoundType soundType);
    public void TakeStunDamage(float damage, ulong byID, PainSoundType soundType);

}

