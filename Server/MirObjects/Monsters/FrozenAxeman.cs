using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  冰魄斧兵
    ///  3种攻击手段
    ///  1.近身普攻
    ///  2.重击
    ///  3.击退
    /// </summary>
    public class FrozenAxeman : MonsterObject
    {
        private byte attType;
        protected internal FrozenAxeman(MonsterInfo info)
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
            int delay = distance * 50 + 750; //50 MS per Step
            int rd = RandomUtils.Next(100);
            if (rd < 70)
            {
                attType = 0;
            }else if(rd < 85)
            {
                attType = 1;
            }
            else
            {
                attType = 2;
            }

            if (attType == 0)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                ActionList.Add(action);
            }
            if (attType == 1)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage*3/2, DefenceType.AC);
                ActionList.Add(action);
            }
            //盾击，击退
            if (attType == 2)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 2 });
                DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.AC);
                ActionList.Add(action);
                Target.Pushed(this, Direction, 2);
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

            MoveTo(Target.CurrentLocation);
        }

    }
}
