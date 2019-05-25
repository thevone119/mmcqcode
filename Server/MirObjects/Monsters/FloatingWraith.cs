using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Server.MirDatabase;
using Server.MirEnvir;
using S = ServerPackets;
using System.Drawing;

namespace Server.MirObjects.Monsters
{
    //FloatingWraith 幽灵射手
    //丢冰弹
    class FloatingWraith : MonsterObject
    {
        //private byte AttackType = 0;
        protected internal FloatingWraith(MonsterInfo info)
            : base(info)
        {
           
        }

        //7格以内，都是攻击距离
        protected override bool InAttackRange()
        {
            if (Target.CurrentMap != CurrentMap) return false;
            if (Target.CurrentLocation == CurrentLocation) return false;

            int x = Math.Abs(Target.CurrentLocation.X - CurrentLocation.X);
            int y = Math.Abs(Target.CurrentLocation.Y - CurrentLocation.Y);

            if (x > 7 || y > 7) return false;
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

            //Point target = Functions.PointMove(CurrentLocation, Direction, 2);
            int delay = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation) * 50 + 550; //50 MS per Step

            Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID=Target.ObjectID, Location = CurrentLocation,Type= 0 });

            int damage = GetAttackPower(MinDC, MaxDC);
            if (damage == 0) return;

            //
            DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.MAC);
            ActionList.Add(action);

            ActionTime = Envir.Time + 300;
            AttackTime = Envir.Time + AttackSpeed;

           
        }


    }
}
