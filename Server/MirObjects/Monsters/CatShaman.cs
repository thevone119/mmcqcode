﻿using System.Drawing;
using Server.MirDatabase;
using Server.MirEnvir;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    /// 灵猫法师
    /// 类似狐狸法师的AI
    /// 远程，几率麻痹
    /// </summary>
    public class CatShaman : MonsterObject
    {
        public long FearTime, TeleportTime;
        public byte AttackRange = 7;

        protected internal CatShaman(MonsterInfo info)
            : base(info)
        {
        }

        protected override bool InAttackRange()
        {
            return CurrentMap == Target.CurrentMap && Functions.InRange(CurrentLocation, Target.CurrentLocation, AttackRange);
        }

        protected override void Attack()
        {
            if (!Target.IsAttackTarget(this))
            {
                Target = null;
                return;
            }

            ShockTime = 0;


            Direction = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);

            //byte spelltype = RandomUtils.Next(2) == 0 ? (byte)1 : (byte)2;
            Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, TargetID = Target.ObjectID, Type = 0 });


            ActionTime = Envir.Time + 300;
            AttackTime = Envir.Time + AttackSpeed;

            int damage = GetAttackPower(MinDC, MaxDC);
            if (damage == 0) return;

            DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + 500, Target, damage, DefenceType.MAC);
            ActionList.Add(action);
            //几率麻痹
            if (RandomUtils.Next(Settings.PoisonResistWeight) >= Target.PoisonResist)
            {
                if (RandomUtils.Next(8) == 0)
                {
                    Target.ApplyPoison(new Poison { Owner = this, Duration = 2, PType = PoisonType.Paralysis, Value = damage / 2, TickSpeed = 2000 }, this);
                }
            }

            if (Target.Dead)
                FindTarget();

        }

        protected override void ProcessTarget()
        {
            if (Target == null || !CanAttack) return;

            if (InAttackRange() && (Envir.Time < FearTime))
            {
                if (Functions.InRange(CurrentLocation, Target.CurrentLocation, 1) && Envir.Time > TeleportTime && RandomUtils.Next(5) == 0)
                {
                    TeleportTime = Envir.Time + 5000;
                    TeleportRandom(40, 4);
                    return;
                }
                else
                {
                    Attack();
                    return;
                }
            }

            FearTime = Envir.Time + 5000;

            if (Envir.Time < ShockTime)
            {
                Target = null;
                return;
            }

            int dist = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation);

            if (dist >= AttackRange)
                MoveTo(Target.CurrentLocation);
            else
            {
                MirDirection dir = Functions.DirectionFromPoint(Target.CurrentLocation, CurrentLocation);

                if (Walk(dir)) return;

                switch (RandomUtils.Next(2)) //No favour
                {
                    case 0:
                        for (int i = 0; i < 7; i++)
                        {
                            dir = Functions.NextDir(dir);

                            if (Walk(dir))
                                return;
                        }
                        break;
                    default:
                        for (int i = 0; i < 7; i++)
                        {
                            dir = Functions.PreviousDir(dir);

                            if (Walk(dir))
                                return;
                        }
                        break;
                }

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

                if (Teleport(CurrentMap, location, true, 2)) return true;
            }

            return false;
        }
    }
}