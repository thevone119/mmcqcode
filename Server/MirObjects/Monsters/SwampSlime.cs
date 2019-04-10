using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  SwampSlime 泥战士
    ///  2种攻击手段
    ///  1.普通攻击
    ///  2.跳击，小于半血，几率触发，麻痹
    /// </summary>
    public class SwampSlime : MonsterObject
    {
        public long FearTime;


        protected internal SwampSlime(MonsterInfo info)
            : base(info)
        {

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

            if(HP < MaxHP / 2 && RandomUtils.Next(4) == 1)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                ActionList.Add(action);
                if (RandomUtils.Next(Settings.PoisonResistWeight) >= Target.PoisonResist)
                {
                    if (RandomUtils.Next(8) == 0)
                    {
                        Target.ApplyPoison(new Poison { Owner = this, Duration = 3, PType = PoisonType.Paralysis, Value = damage / 5, TickSpeed = 1000 }, this);
                    }
                }
            }
            else
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                ActionList.Add(action);
            }

            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }

        

    }
}
