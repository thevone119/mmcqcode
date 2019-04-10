using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  血狒狒
    ///  3种攻击手段
    ///  
    /// </summary>
    public class BloodBaboon : MonsterObject
    {
        public byte mtype;//
        protected internal BloodBaboon(MonsterInfo info)
            : base(info)
        {
            if (RandomUtils.Next(10) == 0)
            {
                mtype = 1;
                return;
            }
            if (RandomUtils.Next(20) == 0)
            {
                mtype = 2;
                return;
            }
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
            DelayedAction action = null;
            if (mtype >= 1 && RandomUtils.Next(5) == 0)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage * 3 / 2, DefenceType.ACAgility);
                ActionList.Add(action);
                ShockTime = 0;
                ActionTime = Envir.Time + 500;
                AttackTime = Envir.Time + (AttackSpeed);
                return;
            }
            if (mtype == 2 && RandomUtils.Next(5)==0)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 2 });
                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage*2, DefenceType.ACAgility);
                ActionList.Add(action);
                ShockTime = 0;
                ActionTime = Envir.Time + 500;
                AttackTime = Envir.Time + (AttackSpeed);
                return;
            }

            Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
            action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
            ActionList.Add(action);
            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }
    }
}
