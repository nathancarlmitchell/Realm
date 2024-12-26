using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Realm
{
    public class AbilityItem : Item
    {
        public enum Type
        {
            Spell,
            Quiver,
        }

        public int Teir { get; set; }
        public int ManaCost { get; set; }
        public int DamageMin { get; set; }
        public int DamageMax { get; set; }
        public int ProjectileSpeed { get; set; }
        public int Lifetime { get; set; }
        public int Range { get; set; }

        public AbilityItem() { }
    }
}
