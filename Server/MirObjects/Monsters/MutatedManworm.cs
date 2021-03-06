using Server.MirDatabase;
using Server.MirEnvir;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    //��Ѫ����
    public class MutatedManworm : CrazyManworm
    {
        public int AttackRange = 5;

        protected internal MutatedManworm(MonsterInfo info)
            : base(info)
        {
        }

        public override int Attacked(PlayerObject attacker, int damage, DefenceType type = DefenceType.ACAgility, bool damageWeapon = true)
        {
            int attackerDamage = base.Attacked(attacker, damage, type, damageWeapon);

            int ownDamage = GetAttackPower(MinDC, MaxDC);

            if (attackerDamage > ownDamage && RandomUtils.Next(2) == 0)
            {
                TeleportToWeakerTarget();
            }

            return attackerDamage;
        }

        public override int Attacked(MonsterObject attacker, int damage, DefenceType type = DefenceType.ACAgility)
        {
            int attackerDamage = base.Attacked(attacker, damage, type);

            int ownDamage = GetAttackPower(MinDC, MaxDC);

            if (attackerDamage > ownDamage && RandomUtils.Next(2) == 0)
            {
                TeleportToWeakerTarget();
            }

            return attackerDamage;
        }

        private void TeleportToWeakerTarget()
        {
            List<MapObject> targets = FindAllTargets(AttackRange, CurrentLocation);

            if (targets.Count < 2) return;

            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i].MinDC > Target.MinDC) continue;

                CurrentLocation = targets[i].CurrentLocation;
                Target = targets[i];

                TeleportRandom(5, 2, CurrentMap);
                break;
            }
        }

        public override bool TeleportRandom(int attempts, int distance, Map temp = null)
        {
            for (int i = 0; i < attempts; i++)
            {
                Point location;

                if (distance <= 0)
                    location = new Point(RandomUtils.Next(CurrentMap.Width), RandomUtils.Next(CurrentMap.Height));
                else
                    location = new Point(CurrentLocation.X + RandomUtils.Next(-distance, distance + 1),
                                         CurrentLocation.Y + RandomUtils.Next(-distance, distance + 1));

                if (Teleport(CurrentMap, location, true, 4)) return true;
            }

            return false;
        }
    }
}
