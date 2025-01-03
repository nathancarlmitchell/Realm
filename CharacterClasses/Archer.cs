using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace Realm.CharacterClasses
{
    public class Archer : Player
    {
        public Archer()
        {
            PlayerClass = Class.Archer;
            Name = "Archer";
            Description =
                "The archer has a long-range attack and can acquire very powerful weapons.";

            image = Art.Archer;
            Sound.PlayerHit = Game1.Instance.Content.Load<SoundEffect>("Sounds/Player/archer_hit");

            WeaponType = PlayerWeaponType.Bow;

            baseHealth = 150;
            baseMana = 100;
            baseAttack = 17;
            baseDefense = 0;
            baseSpeed = 22;
            baseDexterity = 15;
            BaseVitality = 5;
            baseWisdom = 15;

            MaxHealth = 750;
            MaxMana = 250;
            MaxAttack = 75;
            MaxDefense = 25;
            MaxSpeed = 55;
            MaxDexterity = 50;
            MaxVitality = 40;
            MaxWisdom = 50;

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

            Weapon = Weapon.LoadWeapon("Shortbow");
            Weapon.Type = Weapon.WeaponType.Bow;
        }

        public override void LevelUp()
        {
            Attack = baseAttack + (Level * 2);
            Defense = baseDefense + (int)(Level * 0.5);
            Vitality = BaseVitality + (Level * 1);
            Wisdom = baseWisdom + (Level * 1);
            Speed = baseSpeed + (Level * 1);
            Dexterity = baseDexterity + (Level * 1);

            HealthMax = baseHealth + (Level * 25);
            ManaMax = baseMana + (Level * 5);

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

                var aim = Input.GetMouseAimDirection();
                float aimAngle = aim.ToAngle();

                Vector2 vel = Extensions.FromPolar(aimAngle, Weapon.ProjectileMagnitude);

                EntityManager.Add(
                    new Projectile(Player.Instance.Position, vel)
                    {
                        Damage = damage,
                        Duration = Weapon.ProjectileDuration + 25,
                        image = Weapon.ProjectileImage,
                    }
                );
            }
            else
            {
                Sound.Play(Sound.NoMana, 0.4f);
            }
        }
    }
}
