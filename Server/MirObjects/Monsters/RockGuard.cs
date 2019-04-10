using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  石巨人 
    ///  2种攻击手段
    ///  1.普通攻击
    ///  2.身前2格范围伤害，几率眩晕
    /// </summary>
    public class RockGuard : MonsterObject
    {
        public long FearTime;

        public byte AttackRange = 2;//攻击范围2格

        protected internal RockGuard(MonsterInfo info)
            : base(info)
        {

        }

       

        protected override bool InAttackRange()
        {
            if (Target.CurrentMap != CurrentMap) return false;
            if (Target.CurrentLocation == CurrentLocation) return false;

            int x = Math.Abs(Target.CurrentLocation.X - CurrentLocation.X);
            int y = Math.Abs(Target.CurrentLocation.Y - CurrentLocation.Y);

            if (x > AttackRange || y > AttackRange) return false;

            return (x == 0) || (y == 0) || (x == y);
        }

        protected override void ProcessAI()
        {
            if (!Dead && Envir.Time > FearTime)
            {
                FearTime = Envir.Time + 2000;
                if (HP > MaxHP / 2)
                {
                    if (RandomUtils.Next(3) == 0)
                    {
                        AttackRange = 2;
                    }
                    else
                    {
                        AttackRange = 1;
                    }
                }
                else {
                    if (RandomUtils.Next(10) < 8)
                    {
                        AttackRange = 2;
                    }
                    else
                    {
                        AttackRange = 1;
                    }
                }
            }
            base.ProcessAI();
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
            int delay = distance * 50 + 750; //50 MS per Step
            DelayedAction action = null;

            if (distance==1 && RandomUtils.Next(10) < 7)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                ActionList.Add(action);
            }
            else
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                //
                List<MapObject> list = CurrentMap.getMapObjects(Target.CurrentLocation.X, Target.CurrentLocation.Y, 1);
                for (int o = 0; o < list.Count; o++)
                {
                    MapObject ob = list[o];
                    if (ob.Race != ObjectType.Player && ob.Race != ObjectType.Monster) continue;
                    if (!ob.IsAttackTarget(this)) continue;
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage, DefenceType.None);
                    ActionList.Add(action);
                    if (RandomUtils.Next(Settings.PoisonResistWeight) >= ob.PoisonResist)
                    {
                        if (RandomUtils.Next(6) == 0)
                        {
                            ob.ApplyPoison(new Poison { Owner = this, Duration = 7, PType = PoisonType.Stun, Value = damage / 2, TickSpeed = 2000 }, this);
                        }
                    }
                }
            }

            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }

        

    }
}
