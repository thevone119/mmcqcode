using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  DarkBeast LightBeast 暗黑剑齿虎 光明剑齿虎 2种攻击
    ///  2种攻击手段
    ///  近身普攻，和近身1.5倍爆发攻击
    /// </summary>
    public class DarkBeast : MonsterObject
    {
        public byte AttackRange = 1;//攻击范围1格
        protected internal DarkBeast(MonsterInfo info)
            : base(info)
        {

        }

        //爆发的时候2格，普通1格
        protected override bool InAttackRange()
        {
            if (Target.CurrentMap != CurrentMap) return false;
            if (Target.CurrentLocation == CurrentLocation) return false;

            int x = Math.Abs(Target.CurrentLocation.X - CurrentLocation.X);
            int y = Math.Abs(Target.CurrentLocation.Y - CurrentLocation.Y);

            if (x > AttackRange || y > AttackRange) return false;

            return true;
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

            if (AttackRange == 2)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                ActionList.Add(action);
                AttackRange = 1;
            }
            else
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage*2, DefenceType.ACAgility);
                ActionList.Add(action);
            }

            int rd = 5;
            if (HP < MaxHP / 2)
            {
                rd = 3;
            }
            else if (HP < MaxHP / 4)
            {
                rd = 2;
            }
            if (RandomUtils.Next(rd) == 0 && AttackRange == 1)
            {
                AttackRange = 2;
            }

            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }
    }
}
