using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using static Realm.Weapon;

namespace Realm.CharacterClasses
{
    public class Wizard : Player
    {
        public Wizard()
        {
            PlayerClass = Class.Wizard;
            Name = "Wizard";
            Description =
                "The Wizard deals damage from a long distance and blasts enemies with powerful spells.";

            image = Art.Wizard;
            Sound.PlayerHit = Game1.Instance.Content.Load<SoundEffect>("Sounds/Player/wizard_hit");

            WeaponType = PlayerWeaponType.Wand;

            baseHealth = 100;
            baseMana = 100;
            baseAttack = 17;
            baseDefense = 0;
            baseSpeed = 17;
            baseDexterity = 17;
            BaseVitality = 5;
            baseWisdom = 23;

            MaxHealth = 700;
            MaxMana = 385;
            MaxAttack = 75;
            MaxDefense = 25;
            MaxSpeed = 50;
            MaxDexterity = 75;
            MaxVitality = 40;
            MaxWisdom = 60;

            Health = baseHealth;
            HealthMax = baseHealth;

            Mana = baseMana;
            ManaMax = baseMana;

            Attack = baseAttack;
            Defense = baseDefense;
            Speed = baseSpeed;
            Dexterity = baseDexterity;
            Vitality = BaseVitality;
            Wisdom = baseWisdom;

            Weapon = LoadWeapon("Fire Wand");
            Weapon.Type = Weapon.WeaponType.Wand;
        }

        public override void LevelUp()
        {
            Attack = baseAttack + (Level * 2);
            Defense = baseDefense + (int)(Level * 0.5);
            Vitality = BaseVitality + (Level * 1);
            Wisdom = baseWisdom + (Level * 1);
            Speed = baseSpeed + (Level * 1);
            Dexterity = baseDexterity + (Level * 2);

            HealthMax = baseHealth + (Level * 25);
            ManaMax = baseMana + (Level * 10);

            base.LevelUp();
        }

        public override void UseAbility()
        {
            base.UseAbility();

            int abilityCost = 25;

            int damage = rand.Next(10, 15);

            if (Mana >= abilityCost)
            {
                Mana -= abilityCost;

                // Spell bomb.
                for (int i = 0; i < 35; i++)
                {
                    Vector2 vel = Extensions.FromPolar(i * 10, Instance.Weapon.ProjectileMagnitude);
                    EntityManager.Add(
                        new Projectile(Input.GetMousePosition(), vel) { Damage = damage }
                    );
                }
            }
            else
            {
                Sound.Play(Sound.NoMana, 0.4f);
            }
        }
    }
}
