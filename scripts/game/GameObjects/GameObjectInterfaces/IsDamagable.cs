using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public interface IsDamagable
{
    public float maxHealth { get; set; }
    public float currentHealth { get; set; }
    public void OnDamage(float damage,ulong byID);
    public void OnZeroHealth(float damage, ulong byID);

}

