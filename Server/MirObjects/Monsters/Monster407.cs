using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  冰宫射手
    ///  2种射箭，其中一种带穿透
    ///  1.远程攻击
    /// </summary>
    public class Monster407 : MonsterObject
    {

        protected internal Monster407(MonsterInfo info)
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

            if(distance>4 && RandomUtils.Next(100) < 65)
            {
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Type = 1 });
                DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.None);
                ActionList.Add(action);
            }
            else
            {
                Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Type = 0 });
                DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
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
            int distance = Functions.MaxDistance(CurrentLocation, Target.CurrentLocation);
            if (distance > 7)
            {
                MoveTo(Target.CurrentLocation);
                return;
            }


            //30几率跑路
            if (RandomUtils.Next(100) < 70)
            {
                return;
            }
            MirDirection dir = Functions.DirectionFromPoint(Target.CurrentLocation, CurrentLocation);

            if (Walk(dir)) return;

            switch (RandomUtils.Next(2)) //No favour
            {
                case 0:
                    for (int i = 0; i < 7; i++)
                    {
                        dir = Functions.NextDir(dir);

                        if (Walk(dir))
                            return;
                    }
                    break;
                default:
                    for (int i = 0; i < 7; i++)
                    {
                        dir = Functions.PreviousDir(dir);

                        if (Walk(dir))
                            return;
                    }
                    break;
            }
        }

    }
}
