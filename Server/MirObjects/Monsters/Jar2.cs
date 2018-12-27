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
    //四耳圣壶
    class Jar2 : MonsterObject
    {
        private byte AttackType = 0;
        protected internal Jar2(MonsterInfo info)
            : base(info)
        {
           
        }

        //2格以内，都是攻击距离
        protected override bool InAttackRange()
        {
            if (Target.CurrentMap != CurrentMap) return false;
            if (Target.CurrentLocation == CurrentLocation) return false;

            int x = Math.Abs(Target.CurrentLocation.X - CurrentLocation.X);
            int y = Math.Abs(Target.CurrentLocation.Y - CurrentLocation.Y);

            if (x > 2 || y > 2) return false;
            if (x <= 1 && y <= 1)
            {
                AttackType = 0;
                return true;
            }
            if (x == y || x % 2 == y % 2)
            {
                AttackType = 1;
                return true;
            }
            return false;
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


            Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation,Type= AttackType });

            ActionTime = Envir.Time + 300;
            AttackTime = Envir.Time + AttackSpeed;

            int damage = GetAttackPower(MinDC, MaxDC);
            if (damage == 0) return;
            //进身物理攻击，隔位直接破防
            if (AttackType == 0)
            {
                DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + 500, Target, damage, DefenceType.AC);
                ActionList.Add(action);
            }
            else
            {
                DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + 500, Target, damage, DefenceType.None);
                ActionList.Add(action);
            }
        }


    }
}
