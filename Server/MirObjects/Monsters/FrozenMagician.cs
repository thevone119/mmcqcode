using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  冰魄法师
    ///  3种攻击手段
    ///  1.近身普攻
    ///  2.飞弹1
    ///  3.飞弹2 （附带流血效果）
    /// </summary>
    public class FrozenMagician : MonsterObject
    {
        private byte attType;
        protected internal FrozenMagician(MonsterInfo info)
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
            int delay = distance * 50 + 750; //50 MS per Step
            int rd = RandomUtils.Next(100);
            if (distance > 2)
            {
                if (rd < 70)
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
                attType = 0;
            }
            

            if (attType == 0)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                ActionList.Add(action);
            }
            if (attType == 1)
            {
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID= Target.ObjectID,  Location = CurrentLocation, Type = 0 });
                DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.MACAgility);
                ActionList.Add(action);
            }
            //带流血效果
            if (attType == 2)
            {
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Type = 1 });
                DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage * 2, DefenceType.MAC);
                ActionList.Add(action);
                if (RandomUtils.Next(Settings.PoisonResistWeight) >= Target.PoisonResist && RandomUtils.Next(2) == 1)
                {
                    if (RandomUtils.Next(2) == 0)
                    {
                        Target.ApplyPoison(new Poison { Owner = this, Duration = 2, PType = PoisonType.Bleeding, Value = damage / 2, TickSpeed = 2000 }, this);
                    }
                }
            }
            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }

        

    }
}
