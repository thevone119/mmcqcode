using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  RhinoWarrior 犀牛勇士
    ///  3种攻击手段
    ///  1.普通攻击
    ///  2.水泡效果，减速
    ///  3.砸地板，身前1格范围
    public class RhinoWarrior : MonsterObject
    {
        public long FearTime;
        public byte AttackRange = 2;//攻击范围2格

        protected internal RhinoWarrior(MonsterInfo info)
            : base(info)
        {

        }

        protected override void ProcessAI()
        {
            if (!Dead && Envir.Time > FearTime)
            {
                FearTime = Envir.Time + 2000;
                if (RandomUtils.Next(2) == 0)
                {
                    AttackRange = 2;
                }
                else
                {
                    AttackRange = 1;
                }
            }
            base.ProcessAI();
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

   
        protected override void Attack()
        {
            if (!Target.IsAttackTarget(this))
            {
                Target = null;
                return;
            }
            Direction = Functions.DirectionFromPoint(CurrentLocation, Target.CurrentLocation);
            int damage = GetAttackPower(MinDC, MaxDC);
            int Distance = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation);
            int delay = Distance * 50 + 750; //50 MS per Step
            byte attType = 0;
            int rd = RandomUtils.Next(10);
            if (Distance <= 1)
            {
                if (rd<5)
                {
                    attType = 0;
                }else if (rd < 8)
                {
                    attType = 1;
                }
                else
                {
                    attType = 2;
                }
            }
            else
            {
                if (rd < 2)
                {
                    attType = 1;
                }
                else
                {
                    attType = 2;
                }
            }
            DelayedAction action = null;

            switch (attType)
            {
                case 0://普通攻击,2次伤害的
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
             
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage/2, DefenceType.ACAgility);
                    ActionList.Add(action);
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay+300, Target, damage, DefenceType.ACAgility);
                    ActionList.Add(action);
                    break;
                case 1://减速攻击
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.MACAgility);
                    ActionList.Add(action);
                    if (RandomUtils.Next(Settings.PoisonResistWeight) >= Target.PoisonResist)
                    {
                        if (RandomUtils.Next(4) == 0)
                        {
                            Target.ApplyPoison(new Poison { Owner = this, Duration = 7, PType = PoisonType.Slow, Value = damage / 2, TickSpeed = 2000 }, this);
                        }
                    }
                    break;
                case 2://范围攻击
                    Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, TargetID = Target.ObjectID, Type = 0 });
                    List<MapObject> list2 = CurrentMap.getMapObjects(Target.CurrentLocation.X, Target.CurrentLocation.Y, 1);
                    for (int o = 0; o < list2.Count; o++)
                    {
                        MapObject ob = list2[o];
                        if (ob.Race != ObjectType.Player && ob.Race != ObjectType.Monster) continue;
                        if (!ob.IsAttackTarget(this)) continue;
                        action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage, DefenceType.MAC);
                        ActionList.Add(action);
                    }
                    break;
            }

            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }

       

    }
}
