using Server.MirDatabase;
using System;
using System.Collections.Generic;
using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects.Monsters
{
    /// <summary>
    ///  冰宫鼠卫
    ///  3种攻击手段
    ///  1.普通攻击，破甲
    ///  2.普通攻击
    ///  3.标枪
    /// </summary>
    public class Monster410 : MonsterObject
    {
       
        protected internal Monster410(MonsterInfo info)
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
            int delay = distance * 50 + 500; //50 MS per Step
            int attType = 0;
            if (distance == 1)
            {
                attType = RandomUtils.Next(2);
            }
            else
            {
                attType = 2;
            }
            DelayedAction action = null;
            switch (attType)
            {
                case 0:
                    Broadcast(new S.ObjectAttack { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Type = 0 });
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.AC);
                    ActionList.Add(action);
                    break;
                case 1:
                    Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Type = 0 });
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                    ActionList.Add(action);
                    break;
                case 2:
                    Broadcast(new S.ObjectRangeAttack { ObjectID = ObjectID, Direction = Direction, TargetID = Target.ObjectID, Location = CurrentLocation, Type = 1 });
                    action = new DelayedAction(DelayedType.Damage, Envir.Time + delay, Target, damage, DefenceType.ACAgility);
                    ActionList.Add(action);
                    break;
            }
            
            ShockTime = 0;
            ActionTime = Envir.Time + 500;
            AttackTime = Envir.Time + (AttackSpeed);
        }

        
    }
}
