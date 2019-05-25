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
    /// <summary>
    /// 复仇的勇士
    /// 1.刀砍
    /// 2.丢火球
    /// </summary>
    class AvengingWarrior : MonsterObject
    {
        private byte AttackType = 0;
        protected internal AvengingWarrior(MonsterInfo info)
            : base(info)
        {
           
        }

        //8格以内，都是攻击距离
        protected override bool InAttackRange()
        {
            if (Target.CurrentMap != CurrentMap) return false;
            if (Target.CurrentLocation == CurrentLocation) return false;

            int x = Math.Abs(Target.CurrentLocation.X - CurrentLocation.X);
            int y = Math.Abs(Target.CurrentLocation.Y - CurrentLocation.Y);

            if (x > 8 || y > 8) return false;
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
            int distance = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation);
            //Point target = Functions.PointMove(CurrentLocation, Direction, 2);
            int delay = distance * 50 + 550; //50 MS per Step

            int damage = GetAttackPower(MinDC, MaxDC);
            if (damage == 0) return;

            if (distance <= 1 && RandomUtils.Next(100) < 70)
            {
                AttackType = 0;
            }
            else
            {
                AttackType = 1;
            }
         
            //刀砍
            if (AttackType == 0)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                ActionList.Add(action);
            }
            //丢火球
            if (AttackType == 1)
            {
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Type = 0 });
                DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.MAC);
                ActionList.Add(action);
            }
            ActionTime = Envir.Time + 300;
            AttackTime = Envir.Time + AttackSpeed;
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

            MoveTo(Target.CurrentLocation);
        }


    }
}
