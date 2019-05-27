using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  冰宫护卫
    ///  2种攻击手段
    ///  1.普通攻击
    ///  2.砸地板
    /// </summary>
    public class Monster408 : MonsterObject
    {
        protected internal Monster408(MonsterInfo info)
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
            int delay = distance * 50 + 500; //50 MS per Step
            DelayedAction action = null;
            if (RandomUtils.Next(100) < 65)
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction,  Location = CurrentLocation, Type = 0 });
                action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                ActionList.Add(action);
            }
            else
            {
                Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 1 });
                Point target = Functions.PointMove(CurrentLocation, Direction, 1);
                List<MapObject> list = FindAllTargets(1, target, false);
                foreach (MapObject ob in list)
                {
                    if (!ob.IsAttackTarget(this)) continue;
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, ob, damage * 3 / 2, DefenceType.MAC);
                    ActionList.Add(action);
                }
            }

            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }

        
    }
}
