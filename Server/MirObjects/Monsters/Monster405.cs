using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  冰蜗牛
    ///  2种攻击手段
    /// </summary>
    public class Monster405 : MonsterObject
    {
        protected internal Monster405(MonsterInfo info)
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
            int delay = distance * 50 + 350; //50 MS per Step
            if (RandomUtils.Next(100) < 70)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction,  Location = CurrentLocation, Type = 0 });
                DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                ActionList.Add(action);
            }
            else
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                DelayedAction action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.MAC);
                ActionList.Add(action);

                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay+300, Target, damage, DefenceType.MAC);
                ActionList.Add(action);
            }
  
            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }

        
    }
}
