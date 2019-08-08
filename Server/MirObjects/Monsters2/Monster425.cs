using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  毒妖道士 3种攻击手段
    ///  1.无敌
    ///  2.范围减速，范围攻击，范围治疗
    ///  3.远程噬血(范围攻击)
    /// </summary>
    public class Monster425 : MonsterObject
    {
        private long unmatchedTime;//无敌的时间，3秒无敌

        protected internal Monster425(MonsterInfo info)
            : base(info)
        {
        }



        protected override bool InAttackRange()
        {
            return CurrentMap == Target.CurrentMap && Functions.InRange(CurrentLocation, Target.CurrentLocation, 8);
        }

        protected override void Attack()
        {
            if (!Target.IsAttackTarget(this))
            {
                Target = null;
                return;
            }
            Direction = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);
            int damage = GetAttackPower(MinDC, MaxDC);
            int distance = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation);
            int delay = distance * 50 + 500; //50 MS per Step
            DelayedAction action = null;
            int rd = RandomUtils.Next(100);
            byte AttackType = 3;

            if (rd < 70)
            {
                //3.远程噬血(范围攻击)
                AttackType = 3;
            }
            else if (rd < 85 )
            {
                //1.无敌
                if (Envir.Time > unmatchedTime + 2000)
                {
                    AttackType = 1;
                }
                else
                {
                    AttackType = 3;
                }
            }
            else
            {
                //2.范围减速，范围攻击，范围治疗
                AttackType = 2;
            }

            //1.无敌
            if (AttackType == 1)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                unmatchedTime = Envir.Time + RandomUtils.Next(3,6) * 1000;
            }
            //2.范围减速，范围攻击，范围治疗
            if (AttackType == 2)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                List<MapObject> listtargets = CurrentMap.getMapObjects(CurrentLocation.X, CurrentLocation.Y, 3);
                for (int o = 0; o < listtargets.Count; o++)
                {
                    MapObject ob = listtargets[o];
                    if (ob.Race != ObjectType.Player && ob.Race != ObjectType.Monster) continue;
                    if (ob.IsAttackTarget(this))
                    {
                        action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage, DefenceType.MAC);
                        ActionList.Add(action);
                        if (RandomUtils.Next(Settings.PoisonResistWeight) >= ob.PoisonResist && RandomUtils.Next(100) < 40)
                        {
                            ob.ApplyPoison(new Poison { Owner = this, Duration = 3, PType = PoisonType.Slow, Value = damage / 10, TickSpeed = 2000 }, this);
                        }
                    }
                    else
                    {
                        //解除中毒
                        PoisonList.Clear();
                        ob.PoisonList.Clear();
                        //血量恢复
                        ob.HealAmount = (ushort)Math.Min(ushort.MaxValue, ob.HealAmount + damage*2);
                    }
                }
            }
            //3.远程噬血(范围攻击)
            if (AttackType == 3)
            {
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Type = 0 });
                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.MAC);
                ActionList.Add(action);
                //血量恢复
                ChangeHP(damage/5);
            }


            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }


        public override int Attacked(MonsterObject attacker, int damage, DefenceType type = DefenceType.ACAgility)
        {
            if (Envir.Time > unmatchedTime)
            {
                return base.Attacked(attacker, damage, type);
            }
            return 0;
        }

        public override int Attacked(PlayerObject attacker, int damage, DefenceType type = DefenceType.ACAgility, bool damageWeapon = true)
        {
            if (Envir.Time > unmatchedTime)
            {
                return base.Attacked(attacker, damage, type, damageWeapon);
            }

            return 0;
        }



        protected override void ProcessTarget()
        {
            if (Target == null) return;

            if (InAttackRange() && CanAttack)
            {
                Attack();
                if (Target == null || Target.Dead)
                    FindTarget();

                return;
            }

            if (Envir.Time < ShockTime)
            {
                Target = null;
                return;
            }

            if (!CanMove || Target == null) return;
            int distance = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation);
            if (distance > 6)
            {
                MoveTo(Target.CurrentLocation);
                return;
            }


            //40几率跑路
            if (RandomUtils.Next(100) < 60)
            {
                return;
            }
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
}
