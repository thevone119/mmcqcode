using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  昆仑虎 2种攻击手段
    ///  2种攻击手段
    /// </summary>
    public class Monster424 : MonsterObject
    {


        protected internal Monster424(MonsterInfo info)
            : base(info)
        {
        }



        protected override bool InAttackRange()
        {
            return CurrentMap == Target.CurrentMap && Functions.InRange(CurrentLocation, Target.CurrentLocation, 2);
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

            if (RandomUtils.Next(100) < 70)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                ActionList.Add(action);
            }
            else
            {
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Type = 0 });
                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.AC);
                ActionList.Add(action);
                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay+200, Target, damage, DefenceType.MAC);
                ActionList.Add(action);
            }
            

            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
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
            MoveTo(Target.CurrentLocation);
           
        }

    
     
    }
}
