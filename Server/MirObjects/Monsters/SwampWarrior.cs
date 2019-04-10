using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  神殿树人，如何神殿怪物
    ///  2种攻击手段
    ///  1.普通攻击(上毒)
    ///  2.放蘑菇（范围伤害）
    /// </summary>
    public class SwampWarrior : MonsterObject
    {
        public long FearTime;
        public byte AttackRange = 7;

        protected internal SwampWarrior(MonsterInfo info)
            : base(info)
        {

        }

        protected override bool InAttackRange()
        {
            return CurrentMap == Target.CurrentMap && Functions.InRange(CurrentLocation, Target.CurrentLocation, AttackRange);
        }

        protected override void ProcessAI()
        {
            if (!Dead && Envir.Time > FearTime)
            {
                FearTime = Envir.Time + 2000;
                if (HP > MaxHP / 2)
                {
                    if (RandomUtils.Next(2) == 0)
                    {
                        AttackRange = 7;
                    }
                    else
                    {
                        AttackRange = 1;
                    }
                }
                else {
                    if (RandomUtils.Next(10) < 7)
                    {
                        AttackRange = 7;
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

 
            if (distance==1&&RandomUtils.Next(10) < 7)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                ActionList.Add(action);
                if (RandomUtils.Next(Settings.PoisonResistWeight) >= Target.PoisonResist)
                {
                    if (RandomUtils.Next(5) == 0)
                    {
                        Target.ApplyPoison(new Poison { Owner = this, Duration = 12, PType = PoisonType.Green, Value = damage / 5, TickSpeed = 2000 }, this);
                    }
                }
            }
            else
            {
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, TargetID= Target.ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                List<MapObject> list = CurrentMap.getMapObjects(Target.CurrentLocation.X, Target.CurrentLocation.Y, 1);
                for (int o = 0; o < list.Count; o++)
                {
                    MapObject ob = list[o];
                    if (ob.Race == ObjectType.Monster || ob.Race == ObjectType.Player)
                    {
                        if (!ob.IsAttackTarget(this)) continue;
                        DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage, DefenceType.MACAgility);
                        ActionList.Add(action);
                    }
                }
            }
            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }

        

    }
}
